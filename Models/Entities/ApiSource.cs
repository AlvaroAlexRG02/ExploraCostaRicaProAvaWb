using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class ApiSource
{
    public int SourceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? BaseUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    public int? ContentTypeId { get; set; }

    public bool RequiresApiKey { get; set; }

    public string? ApiKeySettingName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ContentType? ContentType { get; set; }

    public virtual ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();
}
