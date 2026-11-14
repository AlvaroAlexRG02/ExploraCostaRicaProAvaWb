namespace ExploraCostaRica.Models.Entities;


public partial class DashboardSource
{
    public int Id { get; set; }


    public string Url { get; set; } = string.Empty;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }


    public string ComponentType { get; set; } = "api";

    public bool RequiresSecret { get; set; }


    public string SourceKey { get; set; } = null!;


    public string? Category { get; set; }

    public virtual ICollection<DashboardSourceItem> Items { get; set; } = new List<DashboardSourceItem>();
}
