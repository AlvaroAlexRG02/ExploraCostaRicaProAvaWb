using System;
using System.Collections.Generic;
using ExploraCostaRica.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Data;

public partial class ExploraCostaRicaDbContext : DbContext
{
    public ExploraCostaRicaDbContext(DbContextOptions<ExploraCostaRicaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiSource> ApiSources { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ContentType> ContentTypes { get; set; }

    public virtual DbSet<Destination> Destinations { get; set; }

    public virtual DbSet<Download> Downloads { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<MediaItem> MediaItems { get; set; }

    public virtual DbSet<SearchHistory> SearchHistories { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwMediaCatalog> VwMediaCatalogs { get; set; }

    public virtual DbSet<VwUserFavorite> VwUserFavorites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiSource>(entity =>
        {
            entity.HasKey(e => e.SourceId).HasName("PK__ApiSourc__16E019192119E43A");

            entity.HasIndex(e => e.Name, "UQ_ApiSources_Name").IsUnique();

            entity.Property(e => e.ApiKeySettingName).HasMaxLength(200);
            entity.Property(e => e.BaseUrl).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(1000);

            entity.HasOne(d => d.ContentType).WithMany(p => p.ApiSources)
                .HasForeignKey(d => d.ContentTypeId)
                .HasConstraintName("FK_ApiSources_ContentTypes");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0BD634067F");

            entity.HasIndex(e => e.Name, "UQ_Categories_Name").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ContentType>(entity =>
        {
            entity.HasKey(e => e.ContentTypeId).HasName("PK__ContentT__2026064AD6961795");

            entity.HasIndex(e => e.Name, "UQ_ContentTypes_Name").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(80);
        });

        modelBuilder.Entity<Destination>(entity =>
        {
            entity.HasKey(e => e.DestinationId).HasName("PK__Destinat__DB5FE4CCA6A21F7D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(1500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Province).HasMaxLength(100);
        });

        modelBuilder.Entity<Download>(entity =>
        {
            entity.HasKey(e => e.DownloadId).HasName("PK__Download__73D5A6F025EC5095");

            entity.HasIndex(e => e.UserId, "IX_Downloads_UserId");

            entity.Property(e => e.DownloadedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FileFormat).HasMaxLength(20);
            entity.Property(e => e.FileName).HasMaxLength(300);

            entity.HasOne(d => d.Media).WithMany(p => p.Downloads)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Downloads_MediaItems");

            entity.HasOne(d => d.User).WithMany(p => p.Downloads)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Downloads_Users");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.FavoriteId).HasName("PK__Favorite__CE74FAD503DD5D3F");

            entity.HasIndex(e => e.UserId, "IX_Favorites_UserId");

            entity.HasIndex(e => new { e.UserId, e.MediaId }, "UQ_Favorites_User_Media").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Media).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favorites_MediaItems");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favorites_Users");
        });

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__MediaIte__B2C2B5CFDBE0348E");

            entity.HasIndex(e => e.CategoryId, "IX_MediaItems_CategoryId");

            entity.HasIndex(e => e.DestinationId, "IX_MediaItems_DestinationId");

            entity.HasIndex(e => e.SourceId, "IX_MediaItems_SourceId");

            entity.HasIndex(e => new { e.SourceId, e.ExternalId }, "UX_MediaItems_Source_ExternalId")
                .IsUnique()
                .HasFilter("([ExternalId] IS NOT NULL)");

            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.ExternalId).HasMaxLength(250);
            entity.Property(e => e.ImageUrl).HasMaxLength(1500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SavedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Url).HasMaxLength(1500);

            entity.HasOne(d => d.Category).WithMany(p => p.MediaItems)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_MediaItems_Categories");

            entity.HasOne(d => d.ContentType).WithMany(p => p.MediaItems)
                .HasForeignKey(d => d.ContentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MediaItems_ContentTypes");

            entity.HasOne(d => d.Destination).WithMany(p => p.MediaItems)
                .HasForeignKey(d => d.DestinationId)
                .HasConstraintName("FK_MediaItems_Destinations");

            entity.HasOne(d => d.Source).WithMany(p => p.MediaItems)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MediaItems_ApiSources");

            entity.HasMany(d => d.Tags).WithMany(p => p.Media)
                .UsingEntity<Dictionary<string, object>>(
                    "MediaTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MediaTags_Tags"),
                    l => l.HasOne<MediaItem>().WithMany()
                        .HasForeignKey("MediaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MediaTags_MediaItems"),
                    j =>
                    {
                        j.HasKey("MediaId", "TagId");
                        j.ToTable("MediaTags");
                    });
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.SearchId).HasName("PK__SearchHi__21C535F4D714CDFE");

            entity.ToTable("SearchHistory");

            entity.HasIndex(e => e.UserId, "IX_SearchHistory_UserId");

            entity.Property(e => e.SearchText).HasMaxLength(300);
            entity.Property(e => e.SearchedAt).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Category).WithMany(p => p.SearchHistories)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_SearchHistory_Categories");

            entity.HasOne(d => d.Destination).WithMany(p => p.SearchHistories)
                .HasForeignKey(d => d.DestinationId)
                .HasConstraintName("FK_SearchHistory_Destinations");

            entity.HasOne(d => d.User).WithMany(p => p.SearchHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_SearchHistory_Users");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__Tags__657CF9AC2E020F97");

            entity.HasIndex(e => e.Name, "UQ_Tags_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C33BF3048");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<VwMediaCatalog>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MediaCatalog");

            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.ContentType).HasMaxLength(80);
            entity.Property(e => e.Destination).HasMaxLength(150);
            entity.Property(e => e.ExternalId).HasMaxLength(250);
            entity.Property(e => e.ImageUrl).HasMaxLength(1500);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.Source).HasMaxLength(150);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Url).HasMaxLength(1500);
        });

        modelBuilder.Entity<VwUserFavorite>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_UserFavorites");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.ContentType).HasMaxLength(80);
            entity.Property(e => e.Destination).HasMaxLength(150);
            entity.Property(e => e.ImageUrl).HasMaxLength(1500);
            entity.Property(e => e.Source).HasMaxLength(150);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Url).HasMaxLength(1500);
            entity.Property(e => e.UserName).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
