using LogiDocs.Application.Abstractions;
using LogiDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Infrastructure.Persistence;

public sealed class LogiDocsDbContext : DbContext, ILogiDocsDbContext
{
    public LogiDocsDbContext(DbContextOptions<LogiDocsDbContext> options) : base(options)
    {
    }

    public IQueryable<Transport> Transports => Set<Transport>();
    public IQueryable<Document> Documents => Set<Document>();
    public IQueryable<AuditEntry> AuditEntries => Set<AuditEntry>();
    public IQueryable<CustomsPayment> CustomsPayments => Set<CustomsPayment>();

    public void Add<T>(T entity) where T : class
    {
        Set<T>().Add(entity);
    }

    public void Delete<T>(T entity) where T : class
    {
        Set<T>().Remove(entity);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transport>(e =>
        {
            e.ToTable("Transports");
            e.HasKey(x => x.Id);

            e.Property(x => x.ReferenceNo).HasMaxLength(64).IsRequired();
            e.Property(x => x.Origin).HasMaxLength(128).IsRequired();
            e.Property(x => x.Destination).HasMaxLength(128).IsRequired();

            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.CreatedByUserId).IsRequired();

            e.HasMany(x => x.Documents)
                .WithOne(d => d.Transport!)
                .HasForeignKey(d => d.TransportId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Segments)
                .WithOne(s => s.Transport!)
                .HasForeignKey(s => s.TransportId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CustomsPayment)
                .WithOne(x => x.Transport!)
                .HasForeignKey<CustomsPayment>(x => x.TransportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TransportSegment>(e =>
        {
            e.ToTable("TransportSegments");
            e.HasKey(x => x.Id);

            e.Property(x => x.OrderNo).IsRequired();
            e.Property(x => x.Mode).IsRequired();

            e.Property(x => x.Origin).HasMaxLength(128).IsRequired();
            e.Property(x => x.Destination).HasMaxLength(128).IsRequired();
            e.Property(x => x.OperatorName).HasMaxLength(128);

            e.HasIndex(x => new { x.TransportId, x.OrderNo }).IsUnique();
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("Documents");
            e.HasKey(x => x.Id);

            e.Property(x => x.Type).IsRequired();
            e.Property(x => x.Status).IsRequired();

            e.Property(x => x.OriginalFileName).HasMaxLength(256).IsRequired();
            e.Property(x => x.StoredFileName).HasMaxLength(256).IsRequired();
            e.Property(x => x.StoredRelativePath).HasMaxLength(512).IsRequired();

            e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            e.Property(x => x.BlockchainTxId).HasMaxLength(256);

            e.Property(x => x.RegisteredOnChainAtUtc);

            e.Property(x => x.ChainStatus)
                .HasConversion<string>()
                .HasMaxLength(32);

            e.Property(x => x.ChainError)
                .HasMaxLength(1000);

            e.Property(x => x.UploadedAtUtc).IsRequired();
            e.Property(x => x.UploadedByUserId).IsRequired();

            e.HasIndex(x => new { x.TransportId, x.Type });
        });

        modelBuilder.Entity<CustomsPayment>(e =>
        {
            e.ToTable("CustomsPayments");
            e.HasKey(x => x.Id);

            e.Property(x => x.CustomsValue)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            e.Property(x => x.DutyRate)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            e.Property(x => x.DutyAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            e.Property(x => x.VatRate)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            e.Property(x => x.VatAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            e.Property(x => x.OtherFees)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            e.Property(x => x.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            e.Property(x => x.Status).IsRequired();

            e.Property(x => x.PaymentReference)
                .HasMaxLength(128);

            e.Property(x => x.Notes)
                .HasMaxLength(1000);

            e.Property(x => x.CalculatedAtUtc);
            e.Property(x => x.PaidAtUtc);

            e.Property(x => x.CreatedByUserId)
                .IsRequired();

            e.HasIndex(x => x.TransportId)
                .IsUnique();
        });
        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.ToTable("AuditEntries");
            e.HasKey(x => x.Id);

            e.Property(x => x.EntityType)
                .HasMaxLength(64)
                .IsRequired();

            e.Property(x => x.EntityId)
                .IsRequired();

            e.Property(x => x.Action)
                .HasMaxLength(128)
                .IsRequired();

            e.Property(x => x.Details)
                .HasMaxLength(2000);

            e.Property(x => x.PerformedByUserId);

            e.Property(x => x.PerformedByName)
                .HasMaxLength(256);

            e.Property(x => x.PerformedByRole)
                .HasMaxLength(128);

            e.Property(x => x.CreatedAtUtc)
                .IsRequired();

            e.HasIndex(x => x.CreatedAtUtc);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });
    }
}