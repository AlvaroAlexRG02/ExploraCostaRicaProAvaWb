namespace ExploraCostaRica.Models.ViewModels

    public class DashboardFilter
    {
        public const int PageSize = 24;


        public string? Q { get; set; }


        public string? Source { get; set; }


        public string? MediaType { get; set; }


        public string? Category { get; set; }


        public DateTime? From { get; set; }

        public DateTime? To { get; set; }


        public string Sort { get; set; } = "recientes";

        public int Page { get; set; } = 1;

        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Q) ||
            !string.IsNullOrWhiteSpace(Source) ||
            !string.IsNullOrWhiteSpace(MediaType) ||
            !string.IsNullOrWhiteSpace(Category) ||
            From.HasValue ||
            To.HasValue;
    }

    public class DashboardSourceSummary
    {
        public string Key { get; set; } = "";

        public string Name { get; set; } = "";

        public string? Category { get; set; }

        public string? Url { get; set; }

        public int Count { get; set; }
    }


    public class DashboardItemCard
    {
        public int Id { get; set; }

        public string SourceKey { get; set; } = "";

        public string SourceName { get; set; } = "";

        public string ItemKey { get; set; } = "";

        public string Title { get; set; } = "";

        public string MediaType { get; set; } = "text";

        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Url { get; set; }

        public string? Thumbnail { get; set; }

        public string? Description { get; set; }
    }

    public class DashboardViewModel
    {
        public DashboardFilter Filter { get; set; } = new();

        public List<DashboardItemCard> Items { get; set; } = new();


        public int Total { get; set; }

        public int TotalPages { get; set; } = 1;

        public List<DashboardSourceSummary> Sources { get; set; } = new();


        public int TotalItems { get; set; }

        public int TotalSources { get; set; }

        public Dictionary<string, int> ByMediaType { get; set; } = new();
    }
}
