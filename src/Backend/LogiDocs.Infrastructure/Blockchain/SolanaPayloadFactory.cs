namespace LogiDocs.Infrastructure.Blockchain;

internal static class SolanaPayloadFactory
{
    public static SolanaRegistrationPayload Create(
        string sha256,
        Guid documentId,
        Guid transportId)
    {
        return new SolanaRegistrationPayload
        {
            DocumentId = SolanaEncoding.GuidTo16Bytes(documentId),
            TransportId = SolanaEncoding.GuidTo16Bytes(transportId),
            DocumentHash = SolanaEncoding.Sha256HexTo32Bytes(sha256)
        };
    }
}