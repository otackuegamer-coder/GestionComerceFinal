using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace GestionComerce
{
    public static class ClientConfig
    {
        private static bool   _loaded          = false;
        private static bool   _clientJsonFound = false; // true once client.json is located on disk
        private static string _clientId        = null;
        private static string[] _allowedPages  = null;

        // True when the app is running from an installed location (Program Files).
        // Used to decide whether a missing client.json should lock down or open up pages.
        private static bool IsInstalledPath =>
            AppDomain.CurrentDomain.BaseDirectory
                .IndexOf("Program Files", StringComparison.OrdinalIgnoreCase) >= 0;

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.json");
                if (!File.Exists(path)) return;
                _clientJsonFound = true;
                var json = File.ReadAllText(path, Encoding.UTF8);
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("clientId", out var cid))
                        _clientId = cid.GetString();
                    if (root.TryGetProperty("allowedPages", out var pages))
                        _allowedPages = JsonSerializer.Deserialize<string[]>(pages.GetRawText());

                    // If a signature is present, verify it. A tampered allowedPages list
                    // produces a different HMAC and the pages are wiped to empty.
                    if (root.TryGetProperty("sig", out var sigProp))
                    {
                        var storedSig = sigProp.GetString() ?? "";
                        if (!string.IsNullOrEmpty(storedSig) &&
                            _clientId != null && _allowedPages != null)
                        {
                            if (!VerifyClientJsonSig(_clientId, _allowedPages, storedSig))
                                _allowedPages = Array.Empty<string>(); // tampered — grant nothing
                        }
                    }

                    // Machine binding: only enforce if installer wrote a non-empty guid
                    if (root.TryGetProperty("machineGuid", out var mgProp))
                    {
                        var storedGuid = mgProp.GetString() ?? "";
                        if (!string.IsNullOrEmpty(storedGuid))
                        {
                            var currentGuid = Registry.GetValue(
                                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                                "MachineGuid", "") as string ?? "";
                            if (!string.Equals(storedGuid, currentGuid, StringComparison.OrdinalIgnoreCase))
                            {
                                _clientId     = null;
                                _allowedPages = null;
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // Must match ComputeClientJsonSig in ZenixAuthApi's OneTimeBuyClientsController exactly.
        private static bool VerifyClientJsonSig(string clientId, string[] allowedPages, string storedSig)
        {
            try
            {
                var sorted = allowedPages.OrderBy(p => p, StringComparer.Ordinal);
                var data   = clientId.ToLowerInvariant() + "|" + string.Join(",", sorted);
                var key    = Encoding.UTF8.GetBytes("ZNX_CJ_2025_" + clientId.ToLowerInvariant());
                using (var hmac = new System.Security.Cryptography.HMACSHA256(key))
                {
                    var hash     = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                    var expected = Convert.ToBase64String(hash);
                    return string.Equals(storedSig, expected, StringComparison.Ordinal);
                }
            }
            catch { return false; }
        }

        // ── Management API base URL ───────────────────────────────────────────
        public static string ManagementApiBaseUrl =>
            ConfigurationManager.AppSettings["ManagementApiBaseUrl"]
            ?? "http://localhost:5001";

        // ── Baked-in client identity (written by installer) ───────────────────
        public static string ClientId
        {
            get
            {
                Load();
                if (!string.IsNullOrWhiteSpace(_clientId)) return _clientId;
                return ConfigurationManager.AppSettings["ClientId"] ?? string.Empty;
            }
        }

        // ── Baked-in page permissions (written by installer) ──────────────────
        public static string[] AllowedPages
        {
            get
            {
                Load();

                // _allowedPages is set (possibly empty) when client.json loaded — use it as-is.
                // An empty array means tampered or wrong machine: grant nothing.
                if (_allowedPages != null) return _allowedPages;

                // client.json was found but failed completely (exception in Load) → grant nothing.
                if (_clientJsonFound) return Array.Empty<string>();

                // No client.json at all. In an installed location this is tampering (file was deleted).
                // In a dev environment (no Program Files path) fall back to all pages.
                if (IsInstalledPath) return Array.Empty<string>();

                // Developer machine without client.json → show everything.
                var raw = ConfigurationManager.AppSettings["AllowedPages"];
                if (!string.IsNullOrWhiteSpace(raw))
                    try { return JsonSerializer.Deserialize<string[]>(raw) ?? AllKnownPages; }
                    catch { }
                return AllKnownPages;
            }
        }

        // ── All known page names (used as fallback and for admin UI) ──────────
        public static readonly string[] AllKnownPages =
        {
            "Vente",
            "Facturation",
            "Inventaire",
            "Clients",
            "Fournisseurs",
            "Livraison",
            "Comptabilité",
            "Projets",
            "Paramètres",
        };

        // Convenience: true when this installation has a baked-in ClientId
        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ClientId);

        // True for one-time-buy installs — login only needs appUserName + PIN,
        // subscription username/password fields should be hidden.
        public static bool IsOtcMode
        {
            get
            {
                Load();
                try
                {
                    var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.json");
                    if (!System.IO.File.Exists(path)) return false;
                    var json = System.IO.File.ReadAllText(path, Encoding.UTF8);
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("isOtc", out var v))
                            return v.GetBoolean();
                    }
                }
                catch { }
                return false;
            }
        }
    }
}
