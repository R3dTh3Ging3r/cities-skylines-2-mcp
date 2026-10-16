using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CS2MCP.Core
{
    /// <summary>Bounded strict JSON for command identity. No native/serializer dependency.
    /// Object keys sort ordinally; number lexemes are retained exactly (conservative conflicts).
    /// Duplicate keys, nonfinite numbers and unpaired UTF-16 surrogates are rejected.</summary>
    public static class CommandJson
    {
        private sealed class Number { internal readonly string Text; internal Number(string text) { Text = text; } }
        public static object Parse(string json) => new Parser(json).Read();
        public static string Canonicalize(string json) => Stringify(Parse(json));
        public static object Snapshot(object value)
        {
            if (value == null || value is string || value is bool || value is int || value is long || value is decimal || value is double || value is float) return value;
            if (value is IDictionary<string, object> map) return map.ToDictionary(pair => pair.Key, pair => Snapshot(pair.Value), StringComparer.Ordinal);
            if (value is IEnumerable items) { var copy = new List<object>(); foreach (object item in items) copy.Add(Snapshot(item)); return copy; }
            throw Invalid("result must contain JSON-compatible BCL values");
        }
        public static string Stringify(object value)
        {
            if (value == null) return "null";
            if (value is Number number) return number.Text;
            if (value is string text) return Quote(text);
            if (value is bool flag) return flag ? "true" : "false";
            if (value is IDictionary<string, object> map)
                return "{" + string.Join(",", map.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => Quote(pair.Key) + ":" + Stringify(pair.Value))) + "}";
            if (value is IEnumerable items)
            {
                var parts = new List<string>(); foreach (object item in items) parts.Add(Stringify(item));
                return "[" + string.Join(",", parts) + "]";
            }
            if (value is int || value is long || value is decimal || value is double || value is float)
                return Canonicalize(Convert.ToString(value, CultureInfo.InvariantCulture));
            throw Invalid("unsupported JSON value");
        }
        private static string Quote(string value)
        {
            var result = new StringBuilder("\"");
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsHighSurrogate(c))
                {
                    if (++i >= value.Length || !char.IsLowSurrogate(value[i])) throw Invalid("unpaired surrogate");
                    result.Append(c).Append(value[i]); continue;
                }
                if (char.IsLowSurrogate(c)) throw Invalid("unpaired surrogate");
                if (c == '"' || c == '\\') result.Append('\\').Append(c);
                else if (c < 32) result.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else result.Append(c);
            }
            return result.Append('"').ToString();
        }
        internal static OperationException Invalid(string message) => new OperationException(
            new BridgeErrorData(ErrorCodes.InvalidArgument, message, httpStatus: 400));
        private sealed class Parser
        {
            private readonly string source; private int position;
            internal Parser(string source)
            {
                if (source == null || Encoding.UTF8.GetByteCount(source) > 1048576) throw Invalid("JSON body exceeds limit or is absent");
                this.source = source;
            }
            internal object Read() { object value = Value(0); Space(); if (position != source.Length) throw Invalid("trailing JSON data"); return value; }
            private void Space() { while (position < source.Length && " \r\n\t".IndexOf(source[position]) >= 0) position++; }
            private char Peek => position < source.Length ? source[position] : '\0';
            private bool Take(char c) { Space(); if (Peek != c) return false; position++; return true; }
            private void Need(char c) { if (!Take(c)) throw Invalid("invalid JSON syntax"); }
            private object Value(int depth)
            {
                if (depth > 64) throw Invalid("JSON nesting exceeds 64");
                Space(); char c = Peek;
                if (c == '"') return Text();
                if (Take('{'))
                {
                    var map = new Dictionary<string, object>(StringComparer.Ordinal);
                    if (Take('}')) return map;
                    do { Space(); string key = Text(); Need(':'); if (map.ContainsKey(key)) throw Invalid("duplicate JSON key"); map.Add(key, Value(depth + 1)); } while (Take(','));
                    Need('}'); return map;
                }
                if (Take('['))
                {
                    var list = new List<object>(); if (Take(']')) return list;
                    do { list.Add(Value(depth + 1)); } while (Take(',')); Need(']'); return list;
                }
                foreach (string literal in new[] { "true", "false", "null" })
                    if (source.Length - position >= literal.Length && string.CompareOrdinal(source, position, literal, 0, literal.Length) == 0)
                    { position += literal.Length; return literal == "null" ? null : (object)(literal == "true"); }
                int start = position;
                if (Peek == '-') position++;
                if (Peek == '0') position++;
                else { if (Peek < '1' || Peek > '9') throw Invalid("invalid JSON value"); Digits(); }
                if (Peek == '.') { position++; RequireDigits(); }
                if (Peek == 'e' || Peek == 'E') { position++; if (Peek == '+' || Peek == '-') position++; RequireDigits(); }
                return new Number(source.Substring(start, position - start));
            }
            private void Digits() { while (Peek >= '0' && Peek <= '9') position++; }
            private void RequireDigits() { int start = position; Digits(); if (start == position) throw Invalid("invalid JSON number"); }
            private string Text()
            {
                if (Peek != '"') throw Invalid("expected JSON string"); position++;
                var result = new StringBuilder();
                while (position < source.Length)
                {
                    char c = source[position++];
                    if (c == '"') { string value = result.ToString(); Quote(value); return value; }
                    if (c < 32) throw Invalid("control character in JSON string");
                    if (c != '\\') { result.Append(c); continue; }
                    if (position >= source.Length) break;
                    c = source[position++];
                    switch (c)
                    {
                        case '"': case '\\': case '/': result.Append(c); break;
                        case 'b': result.Append('\b'); break; case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break; case 'r': result.Append('\r'); break; case 't': result.Append('\t'); break;
                        case 'u':
                            if (source.Length - position < 4 || !ushort.TryParse(source.Substring(position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code)) throw Invalid("invalid unicode escape");
                            result.Append((char)code); position += 4; break;
                        default: throw Invalid("invalid JSON escape");
                    }
                }
                throw Invalid("unterminated JSON string");
            }
        }
    }
}
