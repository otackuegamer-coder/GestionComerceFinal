using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce.Main.Facturation
{
    /// <summary>
    /// Model + service for the SavedInvoicesArticles table.
    /// Mirrors the Invoice / FactureEnregistree pattern:
    /// all CRUD methods live on the class itself.
    ///
    /// API base route: /api/facture/enregistrees/{savedInvoiceId}/articles
    ///   POST   /api/facture/enregistrees/{savedInvoiceId}/articles        → create
    ///   GET    /api/facture/enregistrees/{savedInvoiceId}/articles        → list by invoice
    ///   PUT    /api/facture/enregistrees/articles/{articleId}             → update
    ///   DELETE /api/facture/enregistrees/articles/{articleId}             → delete
    /// </summary>
    public class FactureEnregistreeArticle
    {
        // ── DB columns (matches SavedInvoicesArticles) ────────────────────────

        [JsonPropertyName("savedInvoiceArticleId")]
        public int SavedInvoiceArticleId { get; set; }

        [JsonPropertyName("savedInvoiceId")]
        public int SavedInvoiceId { get; set; }

        /// <summary>
        /// Optional link to the Articles catalogue.
        /// NULL when the article was typed manually on the invoice.
        /// </summary>
        [JsonPropertyName("articleId")]
        public int? ArticleId { get; set; }

        [JsonPropertyName("articleName")]
        public string ArticleName { get; set; } = string.Empty;

        [JsonPropertyName("prixUnitaire")]
        public decimal PrixUnitaire { get; set; }

        [JsonPropertyName("quantite")]
        public decimal Quantite { get; set; }

        /// <summary>TVA percentage (e.g. 20 for 20 %).</summary>
        [JsonPropertyName("tva")]
        public decimal Tva { get; set; }

        [JsonPropertyName("isReversed")]
        public bool IsReversed { get; set; }

        // ── Calculated helpers (not stored; derived locally) ──────────────────

        /// <summary>PrixUnitaire × Quantite — before tax.</summary>
        [JsonIgnore]
        public decimal TotalHT => PrixUnitaire * Quantite;

        /// <summary>TVA amount for this line.</summary>
        [JsonIgnore]
        public decimal MontantTVA => TotalHT * (Tva / 100m);

        /// <summary>TotalHT + MontantTVA.</summary>
        [JsonIgnore]
        public decimal TotalTTC => TotalHT + MontantTVA;

        // ── API base URL ──────────────────────────────────────────────────────

        private const string BaseInvoiceUrl = "http://localhost:5050/api/facture/enregistrees";
        private const string BaseArticleUrl = "http://localhost:5050/api/facture/enregistrees/articles";

        // ════════════════════════════════════════════════════════════════════
        // CREATE
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSTs this article to /enregistrees/{savedInvoiceId}/articles.
        /// Sets <see cref="SavedInvoiceArticleId"/> from the API response on success.
        /// Throws on HTTP failure.
        /// </summary>
        public async Task<bool> InsertAsync()
        {
            string url = $"{BaseInvoiceUrl}/{SavedInvoiceId}/articles";
            var response = await MainWindow.ApiClient.PostAsJsonAsync(url, BuildPayload());

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<ArticleIdResult>();
            SavedInvoiceArticleId = result?.SavedInvoiceArticleId ?? 0;
            return true;
        }

        /// <summary>Sync shim — safe to call from non-async code.</summary>
        public bool Insert()
            => Task.Run(async () => await InsertAsync()).GetAwaiter().GetResult();

        // ════════════════════════════════════════════════════════════════════
        // READ
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns all articles for a given saved invoice.
        /// GET /enregistrees/{savedInvoiceId}/articles
        /// </summary>
        public static async Task<List<FactureEnregistreeArticle>> GetBySavedInvoiceIdAsync(int savedInvoiceId)
        {
            try
            {
                string url = $"{BaseInvoiceUrl}/{savedInvoiceId}/articles";
                return await MainWindow.ApiClient
                           .GetFromJsonAsync<List<FactureEnregistreeArticle>>(url)
                       ?? new List<FactureEnregistreeArticle>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement articles: {ex.Message}");
                return new List<FactureEnregistreeArticle>();
            }
        }

        /// <summary>Sync shim.</summary>
        public static List<FactureEnregistreeArticle> GetBySavedInvoiceId(int savedInvoiceId)
            => Task.Run(() => GetBySavedInvoiceIdAsync(savedInvoiceId)).GetAwaiter().GetResult();

        // ════════════════════════════════════════════════════════════════════
        // UPDATE
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// PUTs this article to /enregistrees/articles/{savedInvoiceArticleId}.
        /// Throws on HTTP failure.
        /// </summary>
        public async Task<bool> UpdateAsync()
        {
            string url = $"{BaseArticleUrl}/{SavedInvoiceArticleId}";
            var response = await MainWindow.ApiClient.PutAsJsonAsync(url, BuildPayload());

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
            }

            return true;
        }

        /// <summary>Sync shim.</summary>
        public bool Update()
            => Task.Run(async () => await UpdateAsync()).GetAwaiter().GetResult();

        // ════════════════════════════════════════════════════════════════════
        // DELETE
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// DELETEs this article at /enregistrees/articles/{savedInvoiceArticleId}.
        /// Throws on HTTP failure.
        /// </summary>
        public async Task<bool> DeleteAsync()
        {
            string url = $"{BaseArticleUrl}/{SavedInvoiceArticleId}";
            var response = await MainWindow.ApiClient.DeleteAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
            }

            return true;
        }

        /// <summary>Sync shim.</summary>
        public bool Delete()
            => Task.Run(async () => await DeleteAsync()).GetAwaiter().GetResult();

        /// <summary>
        /// Static convenience overload — deletes by ID without needing an instance.
        /// </summary>
        public static async Task<bool> DeleteByIdAsync(int savedInvoiceArticleId)
        {
            try
            {
                string url = $"{BaseArticleUrl}/{savedInvoiceArticleId}";
                var response = await MainWindow.ApiClient.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur suppression article: {ex.Message}");
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // BULK HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Replaces all articles for a saved invoice:
        ///   1. Deletes every existing article.
        ///   2. Inserts the new list.
        /// Use inside an edit-save workflow.
        /// </summary>
        public static async Task ReplaceAllAsync(int savedInvoiceId,
            List<FactureEnregistreeArticle> newArticles)
        {
            // Delete existing
            var existing = await GetBySavedInvoiceIdAsync(savedInvoiceId);
            foreach (var old in existing)
                await DeleteByIdAsync(old.SavedInvoiceArticleId);

            // Insert new
            if (newArticles == null) return;
            foreach (var a in newArticles)
            {
                a.SavedInvoiceId = savedInvoiceId;
                await a.InsertAsync();
            }
        }

        /// <summary>Sync shim for ReplaceAllAsync.</summary>
        public static void ReplaceAll(int savedInvoiceId,
            List<FactureEnregistreeArticle> newArticles)
            => Task.Run(() => ReplaceAllAsync(savedInvoiceId, newArticles)).GetAwaiter().GetResult();

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the anonymous payload that matches the server-side
        /// <c>SavedInvoiceArticleDto(ArticleId, ArticleName, PrixUnitaire, Quantite, Tva)</c>.
        /// </summary>
        private object BuildPayload() => new
        {
            articleId = ArticleId,
            articleName = ArticleName ?? string.Empty,
            prixUnitaire = PrixUnitaire,
            quantite = Quantite,
            tva = Tva
            // IsReversed is not in the DTO — the API always inserts 0 / does not update it.
            // Add here if the server DTO is extended in the future.
        };

        // ── Private result wrappers ───────────────────────────────────────────

        private class ArticleIdResult
        {
            [JsonPropertyName("savedInvoiceArticleId")]
            public int SavedInvoiceArticleId { get; set; }
        }
    }
}