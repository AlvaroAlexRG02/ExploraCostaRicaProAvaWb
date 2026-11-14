namespace ExploraCostaRica.Models.DTOs
{

    public class DashboardImportResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = "";


        public List<string> Errors { get; set; } = new();


        public List<string> Notes { get; set; } = new();

        public int Sources { get; set; }

        public int Added { get; set; }

        public int Replaced { get; set; }


        public int CredentialsRemoved { get; set; }

        public static DashboardImportResult Ok(string message) =>
            new() { Success = true, Message = message };

        public static DashboardImportResult Fail(
            string message,
            IEnumerable<string>? errors = null) =>
            new()
            {
                Success = false,
                Message = message,
                Errors = errors?.ToList() ?? new List<string>()
            };
    }
}
