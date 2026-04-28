using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class UpdateCheckResult
    {
        [JsonPropertyName("hasUpdate")]
        public bool HasUpdate { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; }
    }

    public static class UpdateService
    {
        private static readonly JsonSerializerOptions _json =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public static async Task<UpdateCheckResult> CheckAsync()
        {
            if (!ClientConfig.IsConfigured) return null;

            try
            {
                var url = string.Format(
                    "{0}/api/updates/check?clientId={1}",
                    ClientConfig.ManagementApiBaseUrl,
                    Uri.EscapeDataString(ClientConfig.ClientId));

                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) })
                {
                    var resp = await http.GetAsync(url).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) return null;

                    var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<UpdateCheckResult>(json, _json);
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> DownloadAsync(string downloadUrl, IProgress<int> progress)
        {
            var dir = Path.Combine(Path.GetTempPath(), "ZenixUpdate");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "installer.exe");

            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
            {
                using (var resp = await http
                    .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false))
                {
                    resp.EnsureSuccessStatusCode();
                    var total = resp.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        using (var file = File.Create(path))
                        {
                            var buffer = new byte[81920];
                            long downloaded = 0;
                            int read;
                            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                            {
                                await file.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                                downloaded += read;
                                if (total > 0)
                                    progress.Report((int)(downloaded * 100 / total));
                            }
                        }
                    }
                }
            }

            return path;
        }

        // Launches the downloaded installer silently, then exits the app.
        // Inno Setup with the same AppId (client GUID) automatically uninstalls the
        // old version before installing the new one — no manual uninstall needed.
        public static void InstallAndRestart(string installerPath)
        {
            if (!File.Exists(installerPath))
            {
                MessageBox.Show("Fichier installer introuvable.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // /SILENT   — shows progress, no wizard pages
                // /CLOSEAPPLICATIONS — Inno Setup closes any running instances automatically
                Process.Start(new ProcessStartInfo
                {
                    FileName        = installerPath,
                    Arguments       = "/SILENT /CLOSEAPPLICATIONS",
                    UseShellExecute = true  // required for UAC elevation prompt
                });

                // Exit so Inno Setup can replace our files
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du lancement de la mise à jour : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
