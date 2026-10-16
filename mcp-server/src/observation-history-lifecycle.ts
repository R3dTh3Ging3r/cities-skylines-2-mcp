import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { Transport } from "@modelcontextprotocol/sdk/shared/transport.js";
import type { EventEmitter } from "node:events";

type Events = Pick<EventEmitter, "on" | "off">;
/** Install before connect: Protocol.connect owns/wraps transport callbacks afterward. */
export function bindObservationHistoryLifecycle(options: {
  server: McpServer; transport: Transport; history: {dispose(): void};
  stdin: Events; stdout: Events; process: Events; reportError?: (error: unknown) => void;
}) {
  const {server, transport, history, stdin, stdout, process: signals} = options;
  const report = (error: unknown) => {
    try {options.reportError?.(error);} catch { /* reporting must not interrupt SDK cleanup */ }
  };
  let disposed = false;
  let serverClosing: Promise<void> | undefined;
  let transportClosing: Promise<void> | undefined;
  let shutdownResult: Promise<void> | undefined;
  let pendingCloses = 0;
  let removed = false;
  const previousServerCloseMethod = server.close;
  const previousTransportCloseMethod = transport.close;
  const dispose = () => {
    if (disposed) return;
    disposed = true;
    try {history.dispose();} catch (error) {report(error);}
  };
  const remove = () => {
    if (removed) return;
    removed = true;
    stdin.off("end", onDisconnect);
    stdin.off("close", onDisconnect);
    stdin.off("error", onStreamError);
    stdout.off("error", onStreamError);
    signals.off("SIGINT", onDisconnect);
    signals.off("SIGTERM", onDisconnect);
    if (server.close === closeServer) server.close = previousServerCloseMethod;
    if (transport.close === closeTransport) transport.close = previousTransportCloseMethod;
  };
  const trackClose = (invoke: () => Promise<void>): Promise<void> => {
    pendingCloses++;
    // Assign the guarded promise before invoking close: callbacks/errors can run
    // synchronously, and onclose itself is earlier than actual promise settlement.
    return Promise.resolve().then(invoke).finally(() => {
      pendingCloses--;
      if (pendingCloses === 0) remove();
    });
  };
  function closeServer(): Promise<void> {
    dispose();
    serverClosing ??= trackClose(() => previousServerCloseMethod.call(server));
    return serverClosing;
  }
  function closeTransport(): Promise<void> {
    dispose();
    transportClosing ??= trackClose(() => previousTransportCloseMethod.call(transport));
    return transportClosing;
  }
  server.close = closeServer;
  transport.close = closeTransport;
  const shutdown = (): Promise<void> => {
    dispose();
    // Join an external close already in flight; never recurse from onclose or a
    // stream error. Both successful and rejected close attempts release hooks.
    shutdownResult ??= (serverClosing ?? transportClosing ?? closeServer()).catch(report);
    return shutdownResult;
  };
  const onDisconnect = () => {void shutdown();};
  const onStreamError = (error: unknown) => {report(error); void shutdown();};
  const previousTransportClose = transport.onclose;
  transport.onclose = () => {
    dispose();
    try {previousTransportClose?.call(transport);} catch (error) {report(error);}
    // Do not throw: the SDK invokes its own close work after this callback.
  };
  const previousServerClose = server.server.onclose;
  server.server.onclose = () => {
    dispose();
    try {previousServerClose?.call(server.server);} catch (error) {report(error);}
  };
  // Keep recoverable protocol errors recoverable, including earlier reporters
  // that throw. Fatal stream events have their own synchronous disposal hook.
  const previousTransportError = transport.onerror;
  if (previousTransportError) transport.onerror = error => {
    try {previousTransportError.call(transport, error);} catch (failure) {report(failure);}
  };
  const previousServerError = server.server.onerror;
  if (previousServerError) server.server.onerror = error => {
    try {previousServerError.call(server.server, error);} catch (failure) {report(failure);}
  };
  stdin.on("end", onDisconnect);
  stdin.on("close", onDisconnect);
  stdin.on("error", onStreamError);
  stdout.on("error", onStreamError);
  signals.on("SIGINT", onDisconnect);
  signals.on("SIGTERM", onDisconnect);
  return {shutdown};
}
