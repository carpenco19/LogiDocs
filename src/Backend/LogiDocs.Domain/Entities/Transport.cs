using LogiDocs.Domain.Enums;
using System.Reflection.Metadata;

namespace LogiDocs.Domain.Entities;

public sealed class Transport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ReferenceNo { get; set; } = string.Empty; // ex: TR-2026-0001
    public string Origin { get; set; } = string.Empty;      // ex: Chișinău
    public string Destination { get; set; } = string.Empty; // ex: Iași

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public TransportStatus Status { get; set; } = TransportStatus.Draft;

    
    public Guid CreatedByUserId { get; set; }

    
    public List<Document> Documents { get; set; } = new();
}