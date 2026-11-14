using ExploraCostaRica.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Data;


public partial class ExploraCostaRicaDbContext
{
    public virtual DbSet<DashboardSource> DashboardSources { get; set; } = null!;

    public virtual DbSet<DashboardSourceItem> DashboardSourceItems { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DashboardSource>(entity =>
        {
            entity.ToTable("Sources");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.SourceKey, "UX_Sources_SourceKey").IsUnique();

            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ComponentType).HasMaxLength(100);
            entity.Property(e => e.SourceKey).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
        });

        modelBuilder.Entity<DashboardSourceItem>(entity =>
        {
            entity.ToTable("SourceItems");

            entity.HasKey(e => e.Id);


            entity.HasIndex(e => new { e.SourceId, e.ItemKey }, "UX_SourceItems_Source_ItemKey")
                .IsUnique();

            entity.HasIndex(e => e.CreatedAt, "IX_SourceItems_CreatedAt");
            entity.HasIndex(e => e.MediaType, "IX_SourceItems_MediaType");

            entity.Property(e => e.Json).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.SavedAt).HasColumnType("datetime");


            entity.Property(e => e.ItemKey)
                .HasMaxLength(250)
                .UseCollation("Latin1_General_100_BIN2");

            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.MediaType).HasMaxLength(10);
            entity.Property(e => e.Category).HasMaxLength(50);

            entity.HasOne(d => d.Source)
                .WithMany(p => p.Items)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SourceItems_Sources");
        });
    }
}
