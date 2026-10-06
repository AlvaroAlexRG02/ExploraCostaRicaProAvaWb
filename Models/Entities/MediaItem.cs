using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class MediaItem
{
    public long MediaId { get; set; }

    public string? ExternalId { get; set; }

    public int SourceId { get; set; }

    public int ContentTypeId { get; set; }

    public int? CategoryId { get; set; }

    public int? DestinationId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Url { get; set; }

    public string? ImageUrl { get; set; }

    public string? Author { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime SavedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ContentType ContentType { get; set; } = null!;

    public virtual Destination? Destination { get; set; }

    public virtual ICollection<Download> Downloads { get; set; } = new List<Download>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ApiSource Source { get; set; } = null!;

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
