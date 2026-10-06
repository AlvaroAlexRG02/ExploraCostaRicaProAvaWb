namespace ExploraCostaRica.Models
{
    public class MediaItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string MediaType { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;
    }
}