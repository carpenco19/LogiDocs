using LogiDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogiDocs.Infrastructure.Persistence;

public sealed class LogiDocsDbContext : DbContext
{
    public LogiDocsDbContext(DbContextOptions<LogiDocsDbContext> options) : base(options) { }

    public DbSet<Transport> Transports => Set<Transport>();
    public DbSet<Document> Documents => Set<Document>();

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

            e.HasMany(x => x.Documents)
             .WithOne(d => d.Transport!)
             .HasForeignKey(d => d.TransportId)
             .OnDelete(DeleteBehavior.Cascade);
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

            e.Property(x => x.UploadedAtUtc).IsRequired();

            e.HasIndex(x => new { x.TransportId, x.Type });
        });
    }
}