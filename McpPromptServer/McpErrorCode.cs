namespace McpPromptServer
{
    public enum McpErrorCode
    {
        InvalidParams = -32602,
        InternalError = -32603
    }

    public class JsonRpcException : Exception
    {
        public int Code { get; }
        public object? Data { get; }

        public JsonRpcException(McpErrorCode code, string message, object? data = null)
            : base(message)
        {
            Code = (int)code;
            Data = data;
        }
    }
}
