using LogiDocs.Application.Abstractions;
using LogiDocs.Contracts.Documents;

namespace LogiDocs.Application.Documents.Queries;

public sealed class GetDocumentsByTransportUseCase
{
    private readonly ILogiDocsDbContext _db;

    public GetDocumentsByTransportUseCase(ILogiDocsDbContext db)
    {
        _db = db;
    }

    public Task<List<DocumentDto>> ExecuteAsync(Guid transportId, CancellationToken ct = default)
    {
        var items = _db.Documents
            .Where(d => d.TransportId == transportId)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                TransportId = d.TransportId,
                Type = (int)d.Type,
                Status = (int)d.Status,
                OriginalFileName = d.OriginalFileName,
                Sha256 = d.Sha256,
                BlockchainTxId = d.BlockchainTxId,
                ChainStatus = d.ChainStatus,
                RegisteredOnChainAtUtc = d.RegisteredOnChainAtUtc,
                ChainError = d.ChainError,
                UploadedAtUtc = d.UploadedAtUtc,
                UploadedByUserId = d.UploadedByUserId
            })
            .ToList();

        return Task.FromResult(items);
    }
}