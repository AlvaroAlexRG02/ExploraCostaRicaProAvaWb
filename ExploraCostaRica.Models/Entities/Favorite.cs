using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class Favorite
{
    public long FavoriteId { get; set; }

    public int UserId { get; set; }

    public long MediaId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual MediaItem Media { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
