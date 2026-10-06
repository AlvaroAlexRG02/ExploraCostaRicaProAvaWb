using System;
using System.Collections.Generic;

namespace ExploraCostaRica.Models.Entities;

public partial class SearchHistory
{
    public long SearchId { get; set; }

    public int? UserId { get; set; }

    public string SearchText { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? DestinationId { get; set; }

    public int? ResultCount { get; set; }

    public DateTime SearchedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Destination? Destination { get; set; }

    public virtual User? User { get; set; }
}
