using System.Text.Json;
using System.Text.Json.Nodes;
using ExploraCostaRica.Models.Exchange;

namespace ExploraCostaRica.Services
{

    public static class DashboardSecretScrubber
    {

        private static readonly HashSet<string> SecretProperties =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "apikey", "api_key", "api-key", "access_key", "accesskey",
                "access_token", "accesstoken", "refresh_token",
                "client_secret", "secret", "password", "passwd",
                "token", "authorization"
            };


        private static readonly HashSet<string> SecretQueryParams =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "key", "apikey", "api_key", "api-key", "access_key",
                "access_token", "token", "client_id", "client_secret",
                "secret", "appid", "app_id", "auth"
            };

        public static void ScrubSource(ExportSource source, ref int removed)
        {
            source.Url = ScrubUrl(source.Url, ref removed);

            foreach (var item in source.Items)
            {
                ScrubItem(item, ref removed);
            }
        }

        public static void ScrubItem(ExportItem item, ref int removed)
        {
            item.Url = ScrubUrl(item.Url, ref removed);
            item.Thumbnail = ScrubUrl(item.Thumbnail, ref removed);

            if (item.Data is { ValueKind: JsonValueKind.Object } data)
            {
                int antes = removed;
                var nodo = JsonNode.Parse(data.GetRawText());

                Walk(nodo, ref removed);


                if (removed != antes)
                {
                    item.Data = JsonSerializer.SerializeToElement(nodo);
                }
            }
        }


        public static string? ScrubUrl(string? url, ref int removed)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            int q = url.IndexOf('?');

            if (q < 0)
            {
                return url;
            }

            int hash = url.IndexOf('#', q);

            string baseUrl = url[..q];
            string query = hash < 0 ? url[(q + 1)..] : url[(q + 1)..hash];
            string fragment = hash < 0 ? string.Empty : url[hash..];

            var partes = query.Split('&');
            var conservar = new List<string>(partes.Length);

            foreach (var parte in partes)
            {
                int igual = parte.IndexOf('=');
                string nombre = igual < 0 ? parte : parte[..igual];

                try
                {
                    nombre = Uri.UnescapeDataString(nombre);
                }
                catch (UriFormatException)
                {

                }

                if (SecretQueryParams.Contains(nombre))
                {
                    removed++;
                    continue;
                }

                conservar.Add(parte);
            }

            if (conservar.Count == partes.Length)
            {
                return url;
            }

            return baseUrl
                + (conservar.Count > 0 ? "?" + string.Join("&", conservar) : string.Empty)
                + fragment;
        }

        private static void Walk(JsonNode? nodo, ref int removed)
        {
            if (nodo is JsonObject objeto)
            {
                foreach (var nombre in objeto.Select(p => p.Key).ToList())
                {
                    if (SecretProperties.Contains(nombre))
                    {
                        objeto.Remove(nombre);
                        removed++;
                        continue;
                    }

                    var hijo = objeto[nombre];

                    if (TryGetString(hijo, out var texto))
                    {
                        var limpio = ScrubUrl(texto, ref removed);

                        if (!string.Equals(limpio, texto, StringComparison.Ordinal))
                        {
                            objeto[nombre] = limpio;
                        }
                    }
                    else
                    {
                        Walk(hijo, ref removed);
                    }
                }
            }
            else if (nodo is JsonArray lista)
            {
                for (int i = 0; i < lista.Count; i++)
                {
                    var hijo = lista[i];

                    if (TryGetString(hijo, out var texto))
                    {
                        var limpio = ScrubUrl(texto, ref removed);

                        if (!string.Equals(limpio, texto, StringComparison.Ordinal))
                        {
                            lista[i] = limpio;
                        }
                    }
                    else
                    {
                        Walk(hijo, ref removed);
                    }
                }
            }
        }

        private static bool TryGetString(JsonNode? nodo, out string texto)
        {
            if (nodo is JsonValue valor && valor.TryGetValue<string>(out var s) && s != null)
            {
                texto = s;
                return true;
            }

            texto = string.Empty;
            return false;
        }
    }
}
