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
  let closing: Promise<void> | undefined;
  let removed = false;
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
  };
  const shutdown = (): Promise<void> => {
    dispose();
    // Defer close until closing is assigned, so a synchronous close callback cannot recurse.
    closing ??= Promise.resolve().then(() => server.close()).catch(report).finally(remove);
    return closing;
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
    finally {remove();}
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
