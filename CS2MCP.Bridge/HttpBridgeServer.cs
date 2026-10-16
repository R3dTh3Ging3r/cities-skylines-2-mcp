using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CS2MCP.Core;

namespace CS2MCP
{
    /// <summary>
    /// Minimal HTTP/1.1 server on top of TcpListener (avoids HttpListener's
    /// URL ACL requirements on Windows). Localhost only, Connection: close,
    /// single request per connection. The only intended client is the external
    /// cs2-mcp process, but curl works too for debugging.
    /// </summary>
    public sealed class HttpBridgeServer
    {
        private const int MainThreadTimeoutMs = 10000;

        private readonly int m_Port;
        private readonly Func<BridgeRequest, bool> m_EnqueueToMainThread;
        private readonly Func<BridgeRequest, BridgeResponse> m_Operations;
        private readonly WorldIdentity m_World;
        private TcpListener m_Listener;
        private Thread m_AcceptThread;
        private volatile bool m_Running;

        public HttpBridgeServer(int port, Func<BridgeRequest, bool> enqueueToMainThread,
            Func<BridgeRequest, BridgeResponse> operations, WorldIdentity world)
        {
            m_Port = port;
            m_EnqueueToMainThread = enqueueToMainThread;
            m_Operations = operations;
            m_World = world;
        }

        public void Start()
        {
            m_Listener = new TcpListener(IPAddress.Loopback, m_Port);
            m_Listener.Start();
            m_Running = true;
            m_AcceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "CS2MCP.Bridge.Accept",
            };
            m_AcceptThread.Start();
        }

        public void Stop()
        {
            m_Running = false;
            try
            {
                m_Listener?.Stop();
            }
            catch
            {
                // listener already gone
            }
            m_Listener = null;
        }

        private void AcceptLoop()
        {
            while (m_Running)
            {
                TcpClient client;
                try
                {
                    client = m_Listener.AcceptTcpClient();
                }
                catch
                {
                    break; // Stop() closes the listener and unblocks Accept
                }
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    client.ReceiveTimeout = 15000;
                    client.SendTimeout = 15000;

                    try
                    {
                        HttpIngress.ReadAndDispatch(stream, parsed => {
                            var request = new BridgeRequest { Method = parsed.Method, Path = parsed.Path, Body = parsed.Body,
                                Command = parsed.Command, OperationId = parsed.OperationId, IngressWorld = m_World.Current };
                            ParseUrl(parsed.RawTarget, request);
                            WriteResponse(stream, Dispatch(request));
                        });
                    }
                    catch (OperationException error)
                    {
                        BridgeErrorData e = error.Error;
                        WriteResponse(stream, BridgeResponse.Error(e.httpStatus ?? 400, e.message, e.code, e.retryable, e.outcome, e.operationId));
                    }
                }
            }
            catch (Exception e)
            {
                Mod.Log.Warn($"bridge client error: {e.Message}");
            }
        }

        private BridgeResponse Dispatch(BridgeRequest request)
        {
            // Liveness check, answered off the simulation thread so it works
            // even while the game is loading a save.
            if (request.Path == "/ping")
            {
                return HandlePing();
            }
            if (request.Path == "/capabilities")
            {
                return BridgeResponse.Json(EndpointCatalog.CreateOperationManifest(Mod.Identity,
                    Mod.RuntimeSnapshots.Read(DateTime.UtcNow), Mod.NativeValidation, m_World.Snapshot));
            }

            if (EndpointCatalog.IsOperationRoute(request.Path)) return m_Operations(request);
            if (!m_EnqueueToMainThread(request)) return BridgeResponse.Error(409, "pending queue is full", ErrorCodes.BridgeBusy);
            BridgeResponse response = request.WaitForResponse(MainThreadTimeoutMs);
            return response ?? BridgeResponse.Error(503,
                "simulation thread did not respond within 10s (game still loading, or bridge system disabled)",
                ErrorCodes.OutcomeUnknown, retryable: false, outcome: "unknown");
        }

        private static BridgeResponse HandlePing()
        {
            RuntimeEvidence runtime = Mod.RuntimeSnapshots.Read(DateTime.UtcNow);
            return BridgeResponse.Json(new
            {
                ok = true,
                mod = Mod.Name,
                version = Mod.Version,
                gameMode = runtime.gameMode,
                isLoading = runtime.isLoading,
                runtime,
                identity = Mod.Identity,
            });
        }

        private static void ParseUrl(string rawUrl, BridgeRequest request)
        {
            int questionMark = rawUrl.IndexOf('?');
            if (questionMark < 0)
            {
                request.Path = rawUrl;
                return;
            }
            request.Path = rawUrl.Substring(0, questionMark);
            string queryText = rawUrl.Substring(questionMark + 1);
            if (request.Path == "/v2/construction/inventory")
            {
                request.Query = ConstructionInventory.DecodeQuery(queryText);
                return;
            }
            foreach (string pair in queryText.Split('&'))
            {
                if (pair.Length == 0)
                {
                    continue;
                }
                int equals = pair.IndexOf('=');
                string key = equals < 0 ? pair : pair.Substring(0, equals);
                string value = equals < 0 ? "" : pair.Substring(equals + 1);
                request.Query[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
            }
        }

        private static void WriteResponse(NetworkStream stream, BridgeResponse response)
        {
            var head = new StringBuilder();
            head.Append("HTTP/1.1 ").Append(response.Status).Append(response.Status == 200 ? " OK" : " Error").Append("\r\n");
            head.Append("Content-Type: ").Append(response.ContentType).Append("\r\n");
            head.Append("Content-Length: ").Append(response.Body.Length).Append("\r\n");
            head.Append("Connection: close\r\n");
            head.Append("\r\n");
            byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
            stream.Write(headBytes, 0, headBytes.Length);
            if (response.Body.Length > 0)
            {
                stream.Write(response.Body, 0, response.Body.Length);
            }
            stream.Flush();
        }
    }
}
