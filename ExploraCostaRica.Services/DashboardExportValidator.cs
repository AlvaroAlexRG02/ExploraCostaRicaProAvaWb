using System.Text.Json;
using System.Text.RegularExpressions;
using ExploraCostaRica.Models.Exchange;

namespace ExploraCostaRica.Services
{
    public static class DashboardExportValidator
    {
        public const int MaxIssues = 100;

        public const int MaxSourceKeyLength = 100;
        public const int MaxSourceNameLength = 200;
        public const int MaxSourceUrlLength = 500;
        public const int MaxItemKeyLength = 250;


        private static readonly Regex IsoUtc = new(
            @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]+)?Z\z",
            RegexOptions.CultureInvariant);

        private static readonly Regex SourceIdPattern = new(
            @"\A[a-z0-9-]+\z",
            RegexOptions.CultureInvariant);

        private static readonly HashSet<string> Categories =
            new(ExportCategories.All, StringComparer.Ordinal);

        private static readonly HashSet<string> Media =
            new(MediaTypes.All, StringComparer.Ordinal);

        public static List<ValidationIssue> Validate(JsonElement root)
        {
            var c = new Collector();

            if (root.ValueKind != JsonValueKind.Object)
            {
                c.Add("$", "El archivo debe contener un objeto JSON en la raíz.");
                return c.Done();
            }


            if (!root.TryGetProperty("version", out var version))
            {
                c.Add("$.version", "Falta el campo obligatorio 'version'.");
            }
            else if (version.ValueKind != JsonValueKind.String)
            {
                c.Add("$.version", "Debe ser un texto con el valor \"1.0\".");
            }
            else if (version.GetString() != DashboardExport.CurrentVersion)
            {
                c.Add("$.version",
                    $"La versión '{Cut(version.GetString())}' no es compatible; solo se acepta \"{DashboardExport.CurrentVersion}\".");
            }


            if (root.TryGetProperty("exportedAt", out var exportedAt) &&
                exportedAt.ValueKind != JsonValueKind.Null)
            {
                CheckDate(exportedAt, "$.exportedAt", c, checkSqlRange: false);
            }

            CheckStringOrNull(root, "exportedBy", "$.exportedBy", c);


            if (!root.TryGetProperty("sources", out var sources))
            {
                c.Add("$.sources", "Falta el campo obligatorio 'sources'.");
            }
            else if (sources.ValueKind != JsonValueKind.Array)
            {
                c.Add("$.sources", "Debe ser una lista de fuentes.");
            }
            else
            {
                int i = 0;

                foreach (var source in sources.EnumerateArray())
                {
                    ValidateSource(source, $"$.sources[{i}]", c);
                    i++;
                }
            }

            return c.Done();
        }



        private static void ValidateSource(JsonElement source, string path, Collector c)
        {
            if (source.ValueKind != JsonValueKind.Object)
            {
                c.Add(path, "Cada fuente debe ser un objeto.");
                return;
            }


            if (!source.TryGetProperty("id", out var id))
            {
                c.Add($"{path}.id", "Falta el campo obligatorio 'id'.");
            }
            else if (id.ValueKind != JsonValueKind.String)
            {
                c.Add($"{path}.id", "Debe ser un texto.");
            }
            else
            {
                var valor = id.GetString()!;

                if (!SourceIdPattern.IsMatch(valor))
                {
                    c.Add($"{path}.id",
                        $"'{Cut(valor)}' no es válido: solo minúsculas, números y guiones (ej. reddit-memes).");
                }
                else if (valor.Length > MaxSourceKeyLength)
                {
                    c.Add($"{path}.id", $"Supera los {MaxSourceKeyLength} caracteres permitidos.");
                }
            }


            if (!source.TryGetProperty("name", out var name))
            {
                c.Add($"{path}.name", "Falta el campo obligatorio 'name'.");
            }
            else if (name.ValueKind != JsonValueKind.String)
            {
                c.Add($"{path}.name", "Debe ser un texto.");
            }
            else if (name.GetString()!.Length == 0)
            {
                c.Add($"{path}.name", "No puede estar vacío.");
            }
            else if (name.GetString()!.Length > MaxSourceNameLength)
            {
                c.Add($"{path}.name", $"Supera los {MaxSourceNameLength} caracteres permitidos.");
            }


            var url = CheckStringOrNull(source, "url", $"{path}.url", c);

            if (url != null && url.Length > MaxSourceUrlLength)
            {
                c.Add($"{path}.url", $"Supera los {MaxSourceUrlLength} caracteres permitidos.");
            }


            CheckCategory(source, $"{path}.category", c);


            if (!source.TryGetProperty("items", out var items))
            {
                c.Add($"{path}.items", "Falta el campo obligatorio 'items'.");
            }
            else if (items.ValueKind != JsonValueKind.Array)
            {
                c.Add($"{path}.items", "Debe ser una lista de items.");
            }
            else
            {
                int i = 0;

                foreach (var item in items.EnumerateArray())
                {
                    ValidateItem(item, $"{path}.items[{i}]", c);
                    i++;
                }
            }
        }



        private static void ValidateItem(JsonElement item, string path, Collector c)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                c.Add(path, "Cada item debe ser un objeto.");
                return;
            }


            if (!item.TryGetProperty("id", out var id))
            {
                c.Add($"{path}.id", "Falta el campo obligatorio 'id'.");
            }
            else if (id.ValueKind != JsonValueKind.String)
            {
                c.Add($"{path}.id", "Debe ser un texto.");
            }
            else if (id.GetString()!.Length == 0)
            {
                c.Add($"{path}.id", "No puede estar vacío.");
            }
            else if (id.GetString()!.Length > MaxItemKeyLength)
            {
                c.Add($"{path}.id", $"Supera los {MaxItemKeyLength} caracteres permitidos.");
            }


            if (!item.TryGetProperty("title", out var title))
            {
                c.Add($"{path}.title", "Falta el campo obligatorio 'title'.");
            }
            else if (title.ValueKind != JsonValueKind.String)
            {
                c.Add($"{path}.title", "Debe ser un texto.");
            }


            if (!item.TryGetProperty("mediaType", out var mediaType))
            {
                c.Add($"{path}.mediaType", "Falta el campo obligatorio 'mediaType'.");
            }
            else if (mediaType.ValueKind != JsonValueKind.String ||
                     !Media.Contains(mediaType.GetString()!))
            {
                c.Add($"{path}.mediaType",
                    $"Valor no permitido{Echo(mediaType)}. Use: {string.Join(", ", MediaTypes.All)} (minúscula y sin tildes).");
            }


            if (!item.TryGetProperty("createdAt", out var createdAt))
            {
                c.Add($"{path}.createdAt", "Falta el campo obligatorio 'createdAt'.");
            }
            else
            {
                CheckDate(createdAt, $"{path}.createdAt", c, checkSqlRange: true);
            }


            CheckCategory(item, $"{path}.category", c);
            CheckStringOrNull(item, "url", $"{path}.url", c);
            CheckStringOrNull(item, "thumbnail", $"{path}.thumbnail", c);
            CheckStringOrNull(item, "description", $"{path}.description", c);


            if (item.TryGetProperty("data", out var data) &&
                data.ValueKind != JsonValueKind.Null &&
                data.ValueKind != JsonValueKind.Object)
            {
                c.Add($"{path}.data",
                    data.ValueKind == JsonValueKind.String
                        ? "Debe ser un objeto JSON, no un texto con JSON escapado adentro."
                        : "Debe ser un objeto JSON (o null).");
            }
        }



        private static string? CheckStringOrNull(
            JsonElement parent, string name, string path, Collector c)
        {
            if (!parent.TryGetProperty(name, out var value) ||
                value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                c.Add(path, "Debe ser un texto (o null).");
                return null;
            }

            return value.GetString();
        }

        private static void CheckCategory(JsonElement parent, string path, Collector c)
        {
            var name = path[(path.LastIndexOf('.') + 1)..];

            if (!parent.TryGetProperty(name, out var value) ||
                value.ValueKind == JsonValueKind.Null)
            {
                return;
            }

            if (value.ValueKind != JsonValueKind.String ||
                !Categories.Contains(value.GetString()!))
            {
                c.Add(path,
                    $"Valor no permitido{Echo(value)}. Use uno de: {string.Join(", ", ExportCategories.All)}.");
            }
        }

        private static void CheckDate(
            JsonElement value, string path, Collector c, bool checkSqlRange)
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                c.Add(path, "Debe ser un texto con fecha ISO 8601 UTC (ej. 2026-10-14T18:30:00Z).");
                return;
            }

            var texto = value.GetString()!;

            if (!IsoUtc.IsMatch(texto))
            {
                c.Add(path,
                    $"'{Cut(texto)}' no es una fecha ISO 8601 UTC; debe verse como 2026-10-14T18:30:00Z (termina en Z).");
                return;
            }


            if (!value.TryGetDateTime(out var fecha))
            {
                c.Add(path, $"'{texto}' no es una fecha que exista.");
                return;
            }

            if (checkSqlRange && fecha.Year < 1753)
            {
                c.Add(path, "La fecha es anterior a 1753, el mínimo que admite la base de datos.");
            }
        }

        private static string Echo(JsonElement value) =>
            value.ValueKind == JsonValueKind.String
                ? $" '{Cut(value.GetString())}'"
                : string.Empty;

        private static string Cut(string? texto)
        {
            texto ??= string.Empty;
            return texto.Length <= 40 ? texto : texto[..40] + "…";
        }

        private sealed class Collector
        {
            private readonly List<ValidationIssue> _issues = new();
            private int _total;

            public void Add(string path, string message)
            {
                _total++;

                if (_issues.Count < MaxIssues)
                {
                    _issues.Add(new ValidationIssue(path, message));
                }
            }

            public List<ValidationIssue> Done()
            {
                if (_total > _issues.Count)
                {
                    _issues.Add(new ValidationIssue(
                        "$", $"… y {_total - _issues.Count} problemas más."));
                }

                return _issues;
            }
        }
    }
}
