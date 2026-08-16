namespace PowerBIModelingService.RpcHost;

internal static class PbirAuthoringRpcHostContract
{
    internal const string Operation = "pbir/authoring";
    internal const int MaxRequestPayloadBytes = 16 * 1024 * 1024;
}
