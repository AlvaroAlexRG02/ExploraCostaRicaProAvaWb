using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class VwUserFavorite
{
    public long FavoriteId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public long MediaId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Url { get; set; }

    public string? ImageUrl { get; set; }

    public string ContentType { get; set; } = null!;

    public string? Category { get; set; }

    public string? Destination { get; set; }

    public string Source { get; set; } = null!;

    public DateTime FavoriteDate { get; set; }
}
