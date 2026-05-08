using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GestionComerce
{
    /// <summary>
    /// Resolves the base URL of the local API.
    /// Online installs bundle api.json with the hosted URL.
    /// Offline installs have no api.json and fall back to the local Windows service.
    /// </summary>
    public static class ApiConfig
    {
        private static string _baseUrl;

        public static string BaseUrl
        {
            get
            {
                if (_baseUrl != null) return _baseUrl;
                try
                {
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api.json");
                    if (File.Exists(path))
                    {
                        var json = File.ReadAllText(path, Encoding.UTF8);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("baseUrl", out var v))
                        {
                            var url = v.GetString();
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                _baseUrl = url.TrimEnd('/');
                                return _baseUrl;
                            }
                        }
                    }
                }
                catch { }

                // No api.json → offline install → local Windows service
                _baseUrl = "http://localhost:5050";
                return _baseUrl;
            }
        }
    }
}
