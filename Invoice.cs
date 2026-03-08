using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Invoice
    {
        [JsonPropertyName("invoiceId")] public int InvoiceID { get; set; }
        [JsonPropertyName("invoiceNumber")] public string InvoiceNumber { get; set; }
        [JsonPropertyName("invoiceDate")] public DateTime InvoiceDate { get; set; }
        [JsonPropertyName("invoiceType")] public string InvoiceType { get; set; }
        [JsonPropertyName("invoiceIndex")] public string InvoiceIndex { get; set; }
        [JsonPropertyName("creditClientName")] public string CreditClientName { get; set; }
        [JsonPropertyName("creditMontant")] public decimal CreditMontant { get; set; }
        [JsonPropertyName("objet")] public string Objet { get; set; }
        [JsonPropertyName("numberLetters")] public string NumberLetters { get; set; }
        [JsonPropertyName("nameFactureGiven")] public string NameFactureGiven { get; set; }
        [JsonPropertyName("nameFactureReceiver")] public string NameFactureReceiver { get; set; }
        [JsonPropertyName("referenceClient")] public string ReferenceClient { get; set; }

        // PaymentMethod is NOT returned by the API — kept locally
        public string PaymentMethod { get; set; }

        [JsonPropertyName("userName")] public string UserName { get; set; }
        [JsonPropertyName("userICE")] public string UserICE { get; set; }
        [JsonPropertyName("userVAT")] public string UserVAT { get; set; }
        [JsonPropertyName("userPhone")] public string UserPhone { get; set; }
        [JsonPropertyName("userAddress")] public string UserAddress { get; set; }
        [JsonPropertyName("userEtatJuridique")] public string UserEtatJuridique { get; set; }
        [JsonPropertyName("userIdSociete")] public string UserIdSociete { get; set; }
        [JsonPropertyName("userSiegeEntreprise")] public string UserSiegeEntreprise { get; set; }
        [JsonPropertyName("clientName")] public string ClientName { get; set; }
        [JsonPropertyName("clientICE")] public string ClientICE { get; set; }
        [JsonPropertyName("clientVAT")] public string ClientVAT { get; set; }
        [JsonPropertyName("clientPhone")] public string ClientPhone { get; set; }
        [JsonPropertyName("clientAddress")] public string ClientAddress { get; set; }
        [JsonPropertyName("clientEtatJuridique")] public string ClientEtatJuridique { get; set; }
        [JsonPropertyName("clientIdSociete")] public string ClientIdSociete { get; set; }
        [JsonPropertyName("clientSiegeEntreprise")] public string ClientSiegeEntreprise { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("tvaRate")] public decimal TVARate { get; set; }
        [JsonPropertyName("totalHT")] public decimal TotalHT { get; set; }
        [JsonPropertyName("totalTVA")] public decimal TotalTVA { get; set; }
        [JsonPropertyName("totalTTC")] public decimal TotalTTC { get; set; }
        [JsonPropertyName("remise")] public decimal Remise { get; set; }
        [JsonPropertyName("totalAfterRemise")] public decimal TotalAfterRemise { get; set; }
        [JsonPropertyName("etatFacture")] public int EtatFacture { get; set; }
        [JsonPropertyName("isReversed")] public bool IsReversed { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("logoPath")] public string LogoPath { get; set; }
        [JsonPropertyName("createdDate")] public DateTime CreatedDate { get; set; }
        [JsonPropertyName("createdBy")] public int? CreatedBy { get; set; }
        [JsonPropertyName("modifiedDate")] public DateTime? ModifiedDate { get; set; }
        [JsonPropertyName("modifiedBy")] public int? ModifiedBy { get; set; }

        // IsDeleted is not returned by API (soft-deletes are filtered server-side)
        public bool IsDeleted { get; set; }

        public List<InvoiceArticle> Articles { get; set; } = new List<InvoiceArticle>();

        // ── Inner class ───────────────────────────────────────────────────────
        public class InvoiceArticle
        {
            [JsonPropertyName("invoiceArticleId")] public int InvoiceArticleID { get; set; }
            [JsonPropertyName("invoiceId")] public int InvoiceID { get; set; }
            [JsonPropertyName("operationId")] public int? OperationID { get; set; }
            [JsonPropertyName("articleId")] public int ArticleID { get; set; }
            [JsonPropertyName("articleName")] public string ArticleName { get; set; }
            [JsonPropertyName("prixUnitaire")] public decimal PrixUnitaire { get; set; }
            [JsonPropertyName("quantite")] public decimal Quantite { get; set; }
            [JsonPropertyName("tva")] public decimal TVA { get; set; }
            [JsonPropertyName("totalHT")] public decimal TotalHT { get; set; }
            [JsonPropertyName("montantTVA")] public decimal MontantTVA { get; set; }
            [JsonPropertyName("totalTTC")] public decimal TotalTTC { get; set; }
            [JsonPropertyName("isReversed")] public bool IsReversed { get; set; }
            [JsonPropertyName("createdDate")] public DateTime? CreatedDate { get; set; }
            [JsonPropertyName("userIF")] public string UserIF { get; set; }
            [JsonPropertyName("userCNSS")] public string UserCNSS { get; set; }
            [JsonPropertyName("userRC")] public string UserRC { get; set; }
            [JsonPropertyName("userTP")] public string UserTP { get; set; }
            [JsonPropertyName("userRIB")] public string UserRIB { get; set; }
            [JsonPropertyName("userEmail")] public string UserEmail { get; set; }
            [JsonPropertyName("userSiteWeb")] public string UserSiteWeb { get; set; }
            [JsonPropertyName("userPatente")] public string UserPatente { get; set; }
            [JsonPropertyName("userCapital")] public string UserCapital { get; set; }
            [JsonPropertyName("userFax")] public string UserFax { get; set; }
            [JsonPropertyName("userVille")] public string UserVille { get; set; }
            [JsonPropertyName("userCodePostal")] public string UserCodePostal { get; set; }
            [JsonPropertyName("userBankName")] public string UserBankName { get; set; }
            [JsonPropertyName("userAgencyCode")] public string UserAgencyCode { get; set; }
            // Soft-delete flag (filtered server-side; kept locally for compatibility)
            public bool IsDeleted { get; set; }
        }

        // Recalculates TotalHT, TotalTVA, TotalTTC from Articles list
        public void CalculateTotals()
        {
            if (Articles == null || Articles.Count == 0) return;
            decimal ht = 0, tva = 0;
            foreach (var a in Articles)
            {
                decimal lineHT = a.PrixUnitaire * a.Quantite;
                decimal lineTVA = lineHT * (a.TVA / 100m);
                ht += lineHT;
                tva += lineTVA;
            }
            TotalHT = ht;
            TotalTVA = tva;
            TotalTTC = ht + tva;
            TotalAfterRemise = TotalTTC - Remise;
        }

        private static readonly string BaseUrl = "http://localhost:5050/api/facture/invoices";

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<int> CreateInvoiceAsync(Invoice invoice)
        {
            try
            {
                var payload = BuildPayload(invoice);
                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<InvoiceIdResult>();
                    int newId = result?.InvoiceId ?? 0;
                    invoice.InvoiceID = newId;
                    return newId;
                }
                MessageBox.Show(string.Format("Invoice not created. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Invoice not created: {0}", err.Message));
                return 0;
            }
        }

        // ── READ ──────────────────────────────────────────────────────────────

        // GET single invoice with articles
        public async Task<Invoice> GetInvoiceByIdAsync(int invoiceId)
        {
            try
            {
                // API returns { invoice, articles } for single GET
                var wrapper = await MainWindow.ApiClient.GetFromJsonAsync<InvoiceWithArticles>(
                    string.Format("{0}/{1}", BaseUrl, invoiceId));
                if (wrapper?.InvoiceData == null) return null;
                wrapper.InvoiceData.Articles = wrapper.Articles ?? new List<InvoiceArticle>();
                return wrapper.InvoiceData;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Invoice load error: {0}", err.Message));
                return null;
            }
        }

        // GET by invoice number — API has no dedicated endpoint; search client-side from all
        public async Task<Invoice> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            var all = await GetAllInvoicesAsync();
            return all.Find(i => i.InvoiceNumber == invoiceNumber);
        }

        // GET all invoices (includeDeleted has no API param — API always excludes deleted)
        public async Task<List<Invoice>> GetAllInvoicesAsync(bool includeDeleted = false)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Invoice>>(BaseUrl)
                       ?? new List<Invoice>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Invoices load error: {0}", err.Message));
                return new List<Invoice>();
            }
        }

        // SEARCH — API has no search endpoint; filter client-side from all invoices
        public async Task<List<Invoice>> SearchInvoicesAsync(string searchTerm,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var all = await GetAllInvoicesAsync();
            var results = new List<Invoice>();
            string term = (searchTerm ?? "").ToLower();
            foreach (var inv in all)
            {
                if (startDate.HasValue && inv.InvoiceDate < startDate.Value) continue;
                if (endDate.HasValue && inv.InvoiceDate > endDate.Value) continue;
                if (!string.IsNullOrWhiteSpace(term))
                {
                    bool match = (inv.InvoiceNumber ?? "").ToLower().Contains(term)
                              || (inv.ClientName ?? "").ToLower().Contains(term)
                              || (inv.Description ?? "").ToLower().Contains(term)
                              || (inv.ReferenceClient ?? "").ToLower().Contains(term);
                    if (!match) continue;
                }
                results.Add(inv);
            }
            return results;
        }

        // Check if invoice number already exists
        public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber)
        {
            var inv = await GetInvoiceByNumberAsync(invoiceNumber);
            return inv != null;
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<bool> UpdateInvoiceAsync(Invoice invoice)
        {
            try
            {
                var payload = BuildPayload(invoice);
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, invoice.InvoiceID), payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Invoice not updated: {0}", err.Message));
                return false;
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        // Soft delete
        public async Task<bool> DeleteInvoiceAsync(int invoiceId)
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, invoiceId));
                return response.IsSuccessStatusCode;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Invoice not deleted: {0}", err.Message));
                return false;
            }
        }

        // Hard delete — API has only one DELETE endpoint (same as soft delete)
        public async Task<bool> HardDeleteInvoiceAsync(int invoiceId)
        {
            return await DeleteInvoiceAsync(invoiceId);
        }

        // ── ARTICLES ──────────────────────────────────────────────────────────

        public async Task<List<InvoiceArticle>> GetInvoiceArticlesAsync(int invoiceId)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<InvoiceArticle>>(
                    string.Format("{0}/{1}/articles", BaseUrl, invoiceId))
                       ?? new List<InvoiceArticle>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Invoice articles load error: {0}", err.Message));
                return new List<InvoiceArticle>();
            }
        }

        public async Task<bool> AddInvoiceArticleAsync(InvoiceArticle article)
        {
            try
            {
                var payload = new
                {
                    invoiceID = article.InvoiceID,
                    operationID = article.OperationID,
                    articleID = article.ArticleID,
                    articleName = article.ArticleName,
                    prixUnitaire = article.PrixUnitaire,
                    quantite = article.Quantite,
                    tva = article.TVA,
                    isReversed = article.IsReversed
                };
                var response = await MainWindow.ApiClient.PostAsJsonAsync(
                    string.Format("{0}/articles", BaseUrl), payload);
                if (!response.IsSuccessStatusCode)
                {
                    // Read the error body so failures are visible during debugging
                    string body = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine(
                        $"AddInvoiceArticle failed: {response.StatusCode} – {body}");
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Article not added: {0}", err.Message));
                return false;
            }
        }

        public async Task<bool> UpdateInvoiceArticleAsync(InvoiceArticle article)
        {
            try
            {
                var payload = new
                {
                    invoiceID = article.InvoiceID,
                    operationID = article.OperationID,
                    articleID = article.ArticleID,
                    articleName = article.ArticleName,
                    prixUnitaire = article.PrixUnitaire,
                    quantite = article.Quantite,
                    tva = article.TVA,
                    isReversed = article.IsReversed
                };
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/articles/{1}", BaseUrl, article.InvoiceArticleID), payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Article not updated: {0}", err.Message));
                return false;
            }
        }

        public async Task<bool> DeleteInvoiceArticleAsync(int invoiceArticleId)
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/articles/{1}", BaseUrl, invoiceArticleId));
                return response.IsSuccessStatusCode;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Article not deleted: {0}", err.Message));
                return false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static object BuildPayload(Invoice inv) => new
        {
            invoiceNumber = inv.InvoiceNumber,
            invoiceDate = inv.InvoiceDate,
            invoiceType = inv.InvoiceType,
            invoiceIndex = inv.InvoiceIndex,
            creditClientName = inv.CreditClientName,
            creditMontant = inv.CreditMontant,
            objet = inv.Objet,
            numberLetters = inv.NumberLetters,
            nameFactureGiven = inv.NameFactureGiven,
            nameFactureReceiver = inv.NameFactureReceiver,
            referenceClient = inv.ReferenceClient,
            userName = inv.UserName,
            userICE = inv.UserICE,
            userVAT = inv.UserVAT,
            userPhone = inv.UserPhone,
            userAddress = inv.UserAddress,
            userEtatJuridique = inv.UserEtatJuridique,
            userIdSociete = inv.UserIdSociete,
            userSiegeEntreprise = inv.UserSiegeEntreprise,
            clientName = inv.ClientName,
            clientICE = inv.ClientICE,
            clientVAT = inv.ClientVAT,
            clientPhone = inv.ClientPhone,
            clientAddress = inv.ClientAddress,
            clientEtatJuridique = inv.ClientEtatJuridique,
            clientIdSociete = inv.ClientIdSociete,
            clientSiegeEntreprise = inv.ClientSiegeEntreprise,
            currency = inv.Currency,
            tVARate = inv.TVARate,
            totalHT = inv.TotalHT,
            totalTVA = inv.TotalTVA,
            totalTTC = inv.TotalTTC,
            remise = inv.Remise,
            totalAfterRemise = inv.TotalAfterRemise,
            etatFacture = inv.EtatFacture,
            isReversed = inv.IsReversed,
            description = inv.Description,
            logoPath = inv.LogoPath,
            createdBy = inv.CreatedBy
        };

        // Wrapper for single GET which returns { invoice: {...}, articles: [...] }
        private class InvoiceWithArticles
        {
            [JsonPropertyName("invoice")]
            public Invoice InvoiceData { get; set; }

            [JsonPropertyName("articles")]
            public List<InvoiceArticle> Articles { get; set; }
        }

        private class InvoiceIdResult
        {
            [JsonPropertyName("invoiceId")]
            public int InvoiceId { get; set; }
        }
    }
}