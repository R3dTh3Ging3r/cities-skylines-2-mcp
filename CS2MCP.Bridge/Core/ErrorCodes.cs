namespace CS2MCP.Core
{
    public static class ErrorCodes
    {
        public const string WorldMismatch = "WORLD_MISMATCH";
        public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
        public const string OperationCapacity = "OPERATION_CAPACITY";
        public const string UpgradeRequired = "UPGRADE_REQUIRED";
        public const string InvalidArgument = "INVALID_ARGUMENT";
        public const string BridgeBusy = "BRIDGE_BUSY";
        public const string NotReady = "NOT_READY";
        public const string Unsupported = "UNSUPPORTED";
        public const string Locked = "LOCKED";
        public const string NativeRejected = "NATIVE_REJECTED";
        public const string ProtocolError = "PROTOCOL_ERROR";
        public const string TransportUnavailable = "TRANSPORT_UNAVAILABLE";
        public const string OutcomeUnknown = "OUTCOME_UNKNOWN";

        /// <summary>Status-only compatibility fallback, never a classifier of native reasons or effects.</summary>
        public static string FromStatus(int status)
        {
            switch (status)
            {
                case 400: return InvalidArgument;
                case 403: return Locked;
                case 404:
                case 405:
                case 501: return Unsupported;
                case 409: return BridgeBusy;
                case 422: return NativeRejected;
                case 503: return NotReady;
                default: return ProtocolError;
            }
        }
    }
}
