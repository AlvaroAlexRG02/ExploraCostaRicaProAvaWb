using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class Download
{
    public long DownloadId { get; set; }

    public int UserId { get; set; }

    public long MediaId { get; set; }

    public string FileFormat { get; set; } = null!;

    public string? FileName { get; set; }

    public DateTime DownloadedAt { get; set; }

    public virtual MediaItem Media { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
