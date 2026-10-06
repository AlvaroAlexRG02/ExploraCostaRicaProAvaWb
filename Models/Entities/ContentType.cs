using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class ContentType
{
    public int ContentTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ApiSource> ApiSources { get; set; } = new List<ApiSource>();

    public virtual ICollection<MediaItem> MediaItems { get; set; } = new List<MediaItem>();
}
