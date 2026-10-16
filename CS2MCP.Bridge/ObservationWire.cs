using System;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using CS2MCP.Core;
namespace CS2MCP
{
    /// <summary>Serializes only the fixed typed read results. Counting occurs on actual UTF8 writes before retaining an oversized buffer.</summary>
    internal sealed class ObservationWire : IObservationEncoder
    {
        sealed class UnionResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization mode)
            {
                var property = base.CreateProperty(member, mode);
                if ((member.DeclaringType == typeof(ObservationItemResult) && (member.Name == "observation" || member.Name == "error")) || member.DeclaringType == typeof(BridgeErrorData))
                    property.NullValueHandling = NullValueHandling.Ignore;
                return property;
            }
        }
        sealed class CappedStream : MemoryStream
        {
            readonly int limit; internal CappedStream(int limit)
            {
                this.limit = limit;
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (Length + count > limit)
                    throw new InvalidOperationException("observation UTF8 output cap exceeded");
                base.Write(buffer, offset, count);
            }
        }
        sealed class FiniteWriter : JsonTextWriter
        {
            internal FiniteWriter(TextWriter writer) : base(writer) { }
            public override void WriteValue(double value)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new InvalidOperationException("nonfinite observation");
                base.WriteValue(value);
            }
            public override void WriteValue(float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new InvalidOperationException("nonfinite observation");
                base.WriteValue(value);
            }
        }
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { ContractResolver = new UnionResolver(), NullValueHandling = NullValueHandling.Include };
        static byte[] Encode(object value, int limit)
        {
            using (var stream = new CappedStream(limit))
            {
                using (var text = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
                using (var writer = new FiniteWriter(text))
                {
                    JsonSerializer.Create(Settings).Serialize(writer, value);
                    writer.Flush();
                }
                return stream.ToArray();
            }
        }
        public byte[] Item(ObservationItemResult item) => Encode(item, ObservationScheduler.MaxItemBytes);
        public byte[] Batch(ObservationBatchResult batch) => Encode(batch, ObservationScheduler.MaxGroupBytes);
    }
}
