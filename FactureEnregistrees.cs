using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce.Main.Facturation
{
    public class FactureEnregistree
    {
        [JsonPropertyName("savedInvoiceId")]
        public int SavedInvoiceID { get; set; }

        // Image round-trips as Base64 JSON ↔ byte[] in memory
        [JsonPropertyName("invoiceImageBase64")]
        public string InvoiceImageBase64
        {
            get => InvoiceImage != null && InvoiceImage.Length > 0
                       ? Convert.ToBase64String(InvoiceImage) : null;
            set => InvoiceImage = !string.IsNullOrEmpty(value)
                       ? Convert.FromBase64String(value) : null;
        }

        [JsonIgnore]
        public byte[] InvoiceImage { get; set; }

        [JsonPropertyName("imageFileName")]
        public string ImageFileName { get; set; }

        [JsonPropertyName("fournisseurId")]
        public int FournisseurID { get; set; }

        [JsonPropertyName("fournisseurNom")]
        public string FournisseurNom { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("invoiceDate")]
        public DateTime InvoiceDate { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("invoiceReference")]
        public string InvoiceReference { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("updatedDate")]
        public DateTime UpdatedDate { get; set; }

        // Aliases kept for backward compatibility
        public string Fournisseur => FournisseurNom;
        public decimal? Montant => TotalAmount;
        public DateTime DateReception => CreatedDate;
        public string NumeroFacture => InvoiceReference;
        public string InvoiceImagePath
        {
            get => ImageFileName;
            set => ImageFileName = value;
        }

        public FactureEnregistree()
        {
            InvoiceDate = DateTime.Now;
            CreatedDate = DateTime.Now;
            UpdatedDate = DateTime.Now;
        }

        private static readonly string BaseUrl = "http://localhost:5050/api/facture/enregistrees";

        // ── GET ALL ───────────────────────────────────────────────────────────

        /// <summary>Async — use this from all async UI methods.</summary>
        public static async Task<List<FactureEnregistree>> GetAllAsync()
        {
            return await MainWindow.ApiClient
                       .GetFromJsonAsync<List<FactureEnregistree>>(BaseUrl)
                   ?? new List<FactureEnregistree>();
        }

        /// <summary>Sync shim — safe to call from non-async code (runs on thread-pool).</summary>
        public static List<FactureEnregistree> GetAllSavedInvoicesAsync()
            => Task.Run(GetAllAsync).GetAwaiter().GetResult();

        public static List<FactureEnregistree> GetAllInvoices()
            => GetAllSavedInvoicesAsync();

        // ── GET BY ID ─────────────────────────────────────────────────────────

        /// <summary>Async — use this from all async UI methods.</summary>
        public static async Task<FactureEnregistree> GetByIdAsync(int id)
        {
            return await MainWindow.ApiClient
                .GetFromJsonAsync<FactureEnregistree>($"{BaseUrl}/{id}");
        }

        /// <summary>Sync shim — safe to call from non-async code.</summary>
        public static FactureEnregistree GetById(int id)
            => Task.Run(() => GetByIdAsync(id)).GetAwaiter().GetResult();

        // ── INSERT ────────────────────────────────────────────────────────────

        /// <summary>
        /// Async insert — always use this from async button handlers.
        /// Throws on failure so the caller's try/catch shows the error.
        /// </summary>
        public async Task<bool> InsertAsync()
        {
            var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, BuildPayload());

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<SavedInvoiceIdResult>();
            this.SavedInvoiceID = result?.SavedInvoiceId ?? 0;

            // Accounting hook — failure is logged but must never crash the save
            // AFTER
            try
            {
                DateTime safeDate = this.InvoiceDate == DateTime.MinValue || this.InvoiceDate.Year < 1753
                    ? DateTime.Now : this.InvoiceDate;
                int invoiceId = this.SavedInvoiceID;
                decimal amount = this.TotalAmount;
                string reference = this.InvoiceReference ?? $"FAC-{this.SavedInvoiceID}";
                int fournId = this.FournisseurID;

                await Task.Run(() =>
                {
                    var svc = new GestionComerce.ComptabiliteService();
                    svc.EnregistrerAchat(invoiceId, amount, reference, fournId, safeDate);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ComptaHook] {ex.Message}");
            }

            return true;
        }

        // Sync shims kept for backward compat
        public bool InsertSavedInvoiceAsync() => Insert();
        public bool Insert()
            => Task.Run(async () => await InsertAsync()).GetAwaiter().GetResult();

        // ── UPDATE ────────────────────────────────────────────────────────────

        public async Task<bool> UpdateAsync()
        {
            var response = await MainWindow.ApiClient
                .PutAsJsonAsync($"{BaseUrl}/{this.SavedInvoiceID}", BuildPayload());

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
            }

            return true;
        }

        public bool UpdateSavedInvoiceAsync() => Update();
        public bool Update()
            => Task.Run(async () => await UpdateAsync()).GetAwaiter().GetResult();

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync()
        {
            var response = await MainWindow.ApiClient
                .DeleteAsync($"{BaseUrl}/{this.SavedInvoiceID}");

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
            }

            return true;
        }

        public bool DeleteSavedInvoiceAsync() => Delete();
        public bool Delete()
            => Task.Run(async () => await DeleteAsync()).GetAwaiter().GetResult();

        // ── HELPERS ───────────────────────────────────────────────────────────

        private object BuildPayload() => new
        {
            imageFileName = this.ImageFileName ?? string.Empty,
            invoiceImageBase64 = this.InvoiceImage != null && this.InvoiceImage.Length > 0
                                     ? Convert.ToBase64String(this.InvoiceImage) : null,
            fournisseurId = this.FournisseurID,
            totalAmount = this.TotalAmount,
            invoiceDate = this.InvoiceDate,
            description = this.Description,
            invoiceReference = this.InvoiceReference,
            notes = this.Notes
        };

        public static List<SupplierItem> GetSuppliers() => new List<SupplierItem>();

        private class SavedInvoiceIdResult
        {
            [JsonPropertyName("savedInvoiceId")]
            public int SavedInvoiceId { get; set; }
        }
    }

    public class SupplierItem
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public override string ToString() => SupplierName;
    }
}