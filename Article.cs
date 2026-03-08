using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Article
    {
        [JsonPropertyName("articleId")]
        public int ArticleID { get; set; }

        [JsonPropertyName("quantite")]
        public int Quantite { get; set; }

        [JsonPropertyName("prixAchat")]
        public decimal PrixAchat { get; set; }

        [JsonPropertyName("prixVente")]
        public decimal PrixVente { get; set; }

        [JsonPropertyName("prixGros")]
        public decimal PrixGros { get; set; }

        // PrixMP is not returned by the API — keep locally defaulted
        public decimal PrixMP { get; set; }

        [JsonPropertyName("famillyId")]
        public int FamillyID { get; set; }

        [JsonPropertyName("code")]
        public long Code { get; set; }

        [JsonPropertyName("articleName")]
        public string ArticleName { get; set; }

        public bool Etat { get; set; } = true;

        public DateTime? Date { get; set; }

        [JsonPropertyName("dateExpiration")]
        public DateTime? DateExpiration { get; set; }

        [JsonPropertyName("marque")]
        public string marque { get; set; }

        [JsonPropertyName("tva")]
        public decimal tva { get; set; }

        public string numeroLot { get; set; }
        public string bonlivraison { get; set; }
        public DateTime? DateLivraison { get; set; }

        // Image stored and transmitted as Base64 string via the API
        [JsonPropertyName("articleImage")]
        public string ArticleImageBase64
        {
            get => ArticleImage != null && ArticleImage.Length > 0
                       ? Convert.ToBase64String(ArticleImage)
                       : null;

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    ArticleImage = null;
                    return;
                }

                try
                {
                    // Strip data URI prefix if present: "data:image/png;base64,ABC123..."
                    var base64 = value.Contains(",") ? value.Split(',')[1] : value;

                    // Remove any whitespace/newlines that some APIs inject
                    base64 = base64.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

                    ArticleImage = Convert.FromBase64String(base64);
                }
                catch (FormatException ex)
                {
                    // Log and gracefully fall back — don't crash the whole article load
                    System.Diagnostics.Debug.WriteLine($"[Article] Invalid base64 image for article '{ArticleName}': {ex.Message}");
                    ArticleImage = null;
                }
            }
        }

        [JsonIgnore]
        public byte[] ArticleImage { get; set; }

        // Local path of the last selected image file — stored alongside bytes so
        // the UI can fall back to opening the file directly if bytes are not available.
        [JsonPropertyName("articleImagePath")]
        public string ArticleImagePath { get; set; }

        [JsonPropertyName("isUnlimitedStock")]
        public bool IsUnlimitedStock { get; set; }

        // Packaging — kept for local display; not all returned by API
        public int PiecesPerPackage { get; set; }
        public string PackageType { get; set; }
        public decimal PackageWeight { get; set; }
        public string PackageDimensions { get; set; }

        [JsonPropertyName("minimumStock")]
        public int MinimumStock { get; set; }

        [JsonPropertyName("maximumStock")]
        public int MaximumStock { get; set; }

        [JsonPropertyName("storageLocation")]
        public string StorageLocation { get; set; }

        [JsonPropertyName("sku")]
        public string SKU { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        public bool IsPerishable { get; set; }

        [JsonPropertyName("unitOfMeasure")]
        public string UnitOfMeasure { get; set; }

        public int MinQuantityForGros { get; set; }
        public string CountryOfOrigin { get; set; }
        public string Manufacturer { get; set; }
        public DateTime? LastRestockDate { get; set; }
        public string Notes { get; set; }

        [JsonPropertyName("fournisseurId")]
        public int FournisseurID { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/inventory/articles";

        // GET active articles (optionally filter by familleId)
        public async Task<List<Article>> GetArticlesAsync(int? familleId = null)
        {
            try
            {
                var url = familleId.HasValue ? $"{BaseUrl}?familleId={familleId.Value}" : BaseUrl;
                var articles = await MainWindow.ApiClient.GetFromJsonAsync<List<Article>>(url)
                               ?? new List<Article>();

                // ── TEMPORARY DEBUG ──────────────────────────────────────────
                foreach (var art in articles)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[IMG DEBUG] Article '{art.ArticleName}' (ID={art.ArticleID}) " +
                        $"→ ArticleImage is {(art.ArticleImage == null ? "NULL" : $"{art.ArticleImage.Length} bytes")}");
                }
                // ── END DEBUG ────────────────────────────────────────────────

                return articles;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error loading articles: {err.Message}");
                return new List<Article>();
            }
        }

        // GET all articles including inactive (uses same endpoint — API only returns Etat=1,
        // so this mirrors the active list; adjust if a separate endpoint is added later)
        public async Task<List<Article>> GetAllArticlesAsync()
        {
            return await GetArticlesAsync();
        }

        // POST — insert new article
        public async Task<int> InsertArticleAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ArticleIdResult>();
                    return result?.ArticleId ?? 0;
                }
                MessageBox.Show($"Article not inserted. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Article not inserted, error: {err.Message}");
                return 0;
            }
        }

        // PUT — update existing article
        public async Task<int> UpdateArticleAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PutAsJsonAsync($"{BaseUrl}/{this.ArticleID}", payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Article not updated. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Article not updated: {err.Message}");
                return 0;
            }
        }

        // DELETE — soft delete (Etat=0)
        public async Task<int> DeleteArticleAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync($"{BaseUrl}/{this.ArticleID}");
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Article not deleted. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Article not deleted: {err.Message}");
                return 0;
            }
        }

        // Restore a soft-deleted article — PUT with Etat=1
        // Note: the API doesn't have a dedicated restore endpoint, so we use a regular update
        public async Task<int> BringBackArticleAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PutAsJsonAsync($"{BaseUrl}/{this.ArticleID}", payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Article not restored. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Article not restored: {err.Message}");
                return 0;
            }
        }

        // Builds the anonymous payload matching the API's ArticleDto
        private object BuildPayload() => new
        {
            articleName = this.ArticleName ?? string.Empty,
            prixAchat = this.PrixAchat,
            prixVente = this.PrixVente,
            prixGros = this.PrixGros,
            quantite = this.Quantite,
            famillyId = this.FamillyID,
            code = this.Code.ToString(),
            tva = this.tva,
            marque = this.marque ?? string.Empty,
            minimumStock = this.MinimumStock,
            maximumStock = this.MaximumStock,
            storageLocation = this.StorageLocation ?? string.Empty,
            sku = this.SKU ?? string.Empty,
            description = this.Description ?? string.Empty,
            isUnlimitedStock = this.IsUnlimitedStock,
            unitOfMeasure = this.UnitOfMeasure ?? "piece",
            fournisseurId = this.FournisseurID,
            dateExpiration = this.DateExpiration,
            articleImageBase64 = this.ArticleImage != null && this.ArticleImage.Length > 0
                ? Convert.ToBase64String(this.ArticleImage) : null,
            articleImagePath = this.ArticleImagePath ?? string.Empty
        };

        private class ArticleIdResult
        {
            [JsonPropertyName("articleId")]
            public int ArticleId { get; set; }
        }

        // ── Stock helper methods (unchanged) ──────────────────────────────────

        public string GetStockDisplayString() => IsUnlimitedStock ? "∞" : Quantite.ToString();

        public bool HasSufficientStock(int requestedQuantity) =>
            IsUnlimitedStock || Quantite >= requestedQuantity;

        public void DecrementStock(int quantity) { if (!IsUnlimitedStock) Quantite -= quantity; }
        public void IncrementStock(int quantity) { if (!IsUnlimitedStock) Quantite += quantity; }
        public int GetEffectiveQuantity() => IsUnlimitedStock ? 0 : Quantite;
        public bool IsLowStock() => !IsUnlimitedStock && MinimumStock > 0 && Quantite <= MinimumStock;
        public bool IsOverstock() => !IsUnlimitedStock && MaximumStock > 0 && Quantite > MaximumStock;
        public int GetTotalPackages() => PiecesPerPackage <= 0 ? 0 : (int)Math.Ceiling((double)Quantite / PiecesPerPackage);

        public decimal GetApplicablePrice(int quantity) =>
            MinQuantityForGros > 0 && quantity >= MinQuantityForGros && PrixGros > 0
                ? PrixGros : PrixVente;

        public bool IsExpired() => DateExpiration.HasValue && DateExpiration.Value < DateTime.Now;

        public bool IsExpiringWithin(int days) =>
            DateExpiration.HasValue &&
            DateExpiration.Value <= DateTime.Now.AddDays(days) &&
            !IsExpired();
    }
}