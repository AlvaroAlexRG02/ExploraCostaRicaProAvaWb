namespace ExploraCostaRica.Models.Entities;


public partial class DashboardSourceItem
{
    public int Id { get; set; }

    public int SourceId { get; set; }


    public string Json { get; set; } = null!;


    public DateTime CreatedAt { get; set; }


    public string ItemKey { get; set; } = null!;

    public string Title { get; set; } = null!;


    public string MediaType { get; set; } = null!;


    public string? Category { get; set; }


    public DateTime SavedAt { get; set; }

    public virtual DashboardSource Source { get; set; } = null!;
}
