using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CS2MCP.Core
{
    public sealed class IngressRequest
    {
        public string Method { get; internal set; } public string Path { get; internal set; }
        public string RawTarget { get; internal set; } public string Body { get; internal set; }
        public CommandRequest Command { get; internal set; } public string OperationId { get; internal set; }
    }
    /// <summary>Production framing and v2 shape validation, run entirely before enqueue.
    /// Single request/connection; transfer encodings are unsupported, never silently ignored.</summary>
    public static class HttpIngress
    {
        public const int MaxBodyBytes = 1048576, MaxHeaderBytes = 16384;
        private static OperationException Reject(string message, int status = 400) => new OperationException(
            new BridgeErrorData(status == 400 || status == 413 ? ErrorCodes.InvalidArgument : ErrorCodes.Unsupported, message, httpStatus: status));
        public static void ReadAndDispatch(Stream stream, Action<IngressRequest> dispatch)
        {
            // The callback is unreachable until *all* framing, verb, route and command JSON checks succeed.
            IngressRequest request = Read(stream); dispatch(request);
        }
        private static IngressRequest Read(Stream stream)
        {
            var header = new List<byte>(); int matched = 0;
            byte[] end = { 13, 10, 13, 10 };
            while (matched != 4)
            {
                int b = stream.ReadByte(); if (b < 0 || b > 127 || header.Count >= MaxHeaderBytes) throw Reject("invalid or oversized HTTP header");
                header.Add((byte)b); matched = b == end[matched] ? matched + 1 : b == 13 ? 1 : 0;
            }
            string[] lines = Encoding.ASCII.GetString(header.ToArray()).Split(new[] { "\r\n" }, StringSplitOptions.None);
            string[] first = lines[0].Split(' ');
            if (first.Length != 3 || (first[2] != "HTTP/1.1" && first[2] != "HTTP/1.0") ||
                !first[1].StartsWith("/", StringComparison.Ordinal) || first[1].StartsWith("//", StringComparison.Ordinal) ||
                first[1].IndexOfAny(new[] { '#', '\\', '\r', '\n', '\t' }) >= 0) throw Reject("invalid HTTP request line");
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < lines.Length - 2; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon <= 0 || lines[i].Take(colon).Any(c => !(char.IsLetterOrDigit(c) || "!#$%&'*+-.^_`|~".IndexOf(c) >= 0))) throw Reject("invalid HTTP header");
                string key = lines[i].Substring(0, colon); string value = lines[i].Substring(colon + 1).Trim();
                if (headers.ContainsKey(key)) throw Reject("duplicate HTTP header");
                headers.Add(key, value);
            }
            if (headers.ContainsKey("Transfer-Encoding")) throw Reject("transfer encoding unsupported");
            int length = 0;
            if (headers.TryGetValue("Content-Length", out string rawLength) &&
                (!int.TryParse(rawLength, NumberStyles.None, CultureInfo.InvariantCulture, out length) || length < 0)) throw Reject("invalid Content-Length");
            if (length > MaxBodyBytes) throw Reject("body exceeds 1 MiB", 413);
            string target = first[1]; int query = target.IndexOf('?'); string path = query < 0 ? target : target.Substring(0, query);
            string id = null; CS2MCP.EndpointDescriptor endpoint = CS2MCP.EndpointCatalog.Resolve(path, out id);
            if (endpoint == null) throw Reject("unknown route", 404);
            if (!endpoint.legacyHttpVerbs.Contains(first[0])) throw Reject("unsupported method", 405);
            bool command = path == "/v2/commands";
            if (path.StartsWith("/v2/", StringComparison.Ordinal) && query >= 0) throw Reject("v2 query parameters unsupported");
            if (!command && length != 0) throw Reject("this route accepts no body");
            if (first[0] == "POST" && !headers.ContainsKey("Content-Length")) throw Reject("Content-Length required");
            if (command && (!headers.TryGetValue("Content-Type", out string contentType) ||
                !contentType.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))) throw Reject("application/json required");
            byte[] body = new byte[length]; int count = 0;
            while (count < length) { int read = stream.Read(body, count, length - count); if (read <= 0) throw Reject("truncated body"); count += read; }
            string json;
            try { json = new UTF8Encoding(false, true).GetString(body); } catch (DecoderFallbackException) { throw Reject("invalid UTF-8 body"); }
            return new IngressRequest { Method = first[0], Path = path, RawTarget = target, Body = json, OperationId = id,
                Command = command ? ParseCommand(json) : null };
        }
        public static CommandRequest ParseCommand(string json)
        {
            var root = CommandJson.Parse(json) as Dictionary<string, object>;
            if (root == null) throw Reject("command must be an object");
            string[] allowed = { "operationId", "world", "leaseId", "name", "args", "expectedFingerprint" };
            if (root.Keys.Any(k => !allowed.Contains(k))) throw Reject("unknown command field");
            string Text(string key) => root.TryGetValue(key, out object value) && value is string text ? text : throw Reject("missing/string field: " + key);
            if (!root.TryGetValue("world", out object worldValue) || !(worldValue is Dictionary<string, object> world) || world.Count != 2 ||
                !world.TryGetValue("bootId", out object boot) || !(boot is string bootId) ||
                !world.TryGetValue("epoch", out object epochValue) || !long.TryParse(CommandJson.Stringify(epochValue), NumberStyles.None, CultureInfo.InvariantCulture, out long epoch)) throw Reject("invalid world");
            if (!root.TryGetValue("args", out object args) || !(args is Dictionary<string, object>)) throw Reject("args must be an object");
            if (root.ContainsKey("expectedFingerprint") && !(root["expectedFingerprint"] is string)) throw Reject("fingerprint must be a string");
            var request = new CommandRequest(Text("operationId"), new WorldRef(bootId, epoch), Text("leaseId"), Text("name"), CommandJson.Stringify(args),
                root.ContainsKey("expectedFingerprint") ? Text("expectedFingerprint") : null);
            // Only fixed catalogued legacy writer names are candidates. No URL/reflection dispatch.
            if (!CS2MCP.EndpointCatalog.All.Any(e => e.capability == request.name && e.access != "read" && !e.path.StartsWith("/v2/", StringComparison.Ordinal)))
                throw new OperationException(new BridgeErrorData(ErrorCodes.Unsupported, "unknown command", httpStatus: 404, operationId: request.operationId));
            return request;
        }
    }
}
