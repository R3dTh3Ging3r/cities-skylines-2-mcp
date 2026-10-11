using System;

namespace CS2MCP.Core
{
    /// <summary>Additive error wire data. Unknown requires reconciliation, not automatic retry.</summary>
    public sealed class BridgeErrorData
    {
        public string code { get; set; }
        public string message { get; set; }
        public bool retryable { get; set; }
        private string m_Outcome;
        public string outcome
        {
            get => m_Outcome;
            set
            {
                if (value != "notApplied" && value != "unknown" && value != "partial")
                    throw new ArgumentException("outcome must be notApplied, unknown or partial", nameof(value));
                m_Outcome = value;
            }
        }
        public int? httpStatus { get; set; }
        public string[] nativeErrors { get; set; }
        public string operationId { get; set; }

        public BridgeErrorData(string code, string message, bool retryable = false,
            string outcome = "notApplied", int? httpStatus = null,
            string operationId = null, string[] nativeErrors = null)
        {
            this.code = code;
            this.message = message;
            this.retryable = retryable;
            this.outcome = outcome;
            this.httpStatus = httpStatus;
            this.operationId = operationId;
            this.nativeErrors = nativeErrors;
        }
    }

    public sealed class Capability
    {
        public string name { get; set; }
        public string support { get; set; }
        public string reason { get; set; }
    }
}
