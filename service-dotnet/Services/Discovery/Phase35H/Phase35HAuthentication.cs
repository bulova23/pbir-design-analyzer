using System.Security.Cryptography;
using System.Text;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35HAuthentication
{
    internal static string Sign(Phase35HRequest request, RSA key) => Convert.ToBase64String(key.SignData(new Phase35ACanonicalJson().Serialize(request), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    internal static bool Verify(Phase35HRequest request, string signature, RSA key) => key.VerifyData(new Phase35ACanonicalJson().Serialize(request), Convert.FromBase64String(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    internal static string Hash(Phase35HRequest request) => new Phase35ACanonicalJson().Hash(request);
    internal static string SignResponse(Phase35HResponse response, RSA key) => Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(response.ResponseHash), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    internal static bool VerifyResponse(Phase35HResponse response, RSA key) => key.VerifyData(Encoding.UTF8.GetBytes(response.ResponseHash), Convert.FromBase64String(response.Signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
}

internal interface IPhase35HTransport
{
    Phase35HResponse Send(Phase35HEnvelope envelope);
}

internal sealed class Phase35HTransport(Phase35HWorker worker) : IPhase35HTransport
{
    public Phase35HResponse Send(Phase35HEnvelope envelope) => worker.Handle(envelope);
}
