using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class VwMediaCatalog
{
    public long MediaId { get; set; }

    public string? ExternalId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Url { get; set; }

    public string? ImageUrl { get; set; }

    public string? Author { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime SavedAt { get; set; }

    public int ContentTypeId { get; set; }

    public string ContentType { get; set; } = null!;

    public int? CategoryId { get; set; }

    public string? Category { get; set; }

    public int? DestinationId { get; set; }

    public string? Destination { get; set; }

    public string? Province { get; set; }

    public int SourceId { get; set; }

    public string Source { get; set; } = null!;
}
