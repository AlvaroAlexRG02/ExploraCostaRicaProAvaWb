using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class Tag
{
    public int TagId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<MediaItem> Media { get; set; } = new List<MediaItem>();
}
