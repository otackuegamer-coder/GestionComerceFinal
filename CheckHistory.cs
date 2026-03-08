using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce.Models
{
    public class CheckHistory
    {
        [JsonPropertyName("checkId")]
        public int CheckID { get; set; }

        [JsonPropertyName("checkReference")]
        public string CheckReference { get; set; } = "";

        // Image stored and transmitted as Base64 string via the API
        [JsonPropertyName("checkImageBase64")]
        public string CheckImageBase64
        {
            get => CheckImage != null && CheckImage.Length > 0
                ? Convert.ToBase64String(CheckImage)
                : null;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    CheckImage = null;
                    return;
                }

                // Strip the data URI prefix if present (e.g. "data:image/jpeg;base64,")
                string base64 = value;
                int commaIndex = value.IndexOf(',');
                if (commaIndex >= 0)
                    base64 = value.Substring(commaIndex + 1);

                try
                {
                    CheckImage = Convert.FromBase64String(base64);
                }
                catch (FormatException)
                {
                    CheckImage = null;
                }
            }
        }

        [JsonIgnore]
        public byte[] CheckImage { get; set; }

        [JsonPropertyName("checkImagePath")]
        public string CheckImagePath { get; set; } = "";

        [JsonPropertyName("invoiceId")]
        public int? InvoiceID { get; set; }

        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; } = "";

        [JsonPropertyName("checkAmount")]
        public decimal? CheckAmount { get; set; }

        [JsonPropertyName("checkDate")]
        public DateTime CheckDate { get; set; }

        [JsonPropertyName("bankName")]
        public string BankName { get; set; } = "";

        [JsonPropertyName("checkStatus")]
        public string CheckStatus { get; set; } = "En Attente";

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("updatedDate")]
        public DateTime UpdatedDate { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/checks";

        public CheckHistory()
        {
            CheckDate = DateTime.Now;
            CreatedDate = DateTime.Now;
            UpdatedDate = DateTime.Now;
            CheckStatus = "En Attente";
        }

        // GET all checks (optionally filter by status or invoiceId)
        public async Task<List<CheckHistory>> GetAllChecksAsync(string status = null, int? invoiceId = null)
        {
            try
            {
                var url = BaseUrl;
                if (!string.IsNullOrWhiteSpace(status) && invoiceId.HasValue)
                    url += $"?status={status}&invoiceId={invoiceId.Value}";
                else if (!string.IsNullOrWhiteSpace(status))
                    url += $"?status={status}";
                else if (invoiceId.HasValue)
                    url += $"?invoiceId={invoiceId.Value}";

                return await MainWindow.ApiClient.GetFromJsonAsync<List<CheckHistory>>(url)
                       ?? new List<CheckHistory>();
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error loading checks: {err.Message}");
                return new List<CheckHistory>();
            }
        }

        // GET single check by ID
        public async Task<CheckHistory> GetCheckByIDAsync(int checkID)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<CheckHistory>($"{BaseUrl}/{checkID}");
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error loading check: {err.Message}");
                return null;
            }
        }

        // GET checks by invoice ID
        public async Task<List<CheckHistory>> GetChecksByInvoiceIDAsync(int invoiceID)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<CheckHistory>>($"{BaseUrl}/by-invoice/{invoiceID}")
                       ?? new List<CheckHistory>();
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error loading checks by invoice: {err.Message}");
                return new List<CheckHistory>();
            }
        }

        // GET checks by status
        public async Task<List<CheckHistory>> GetChecksByStatusAsync(string status)
        {
            return await GetAllChecksAsync(status: status);
        }

        // SEARCH checks by reference
        public async Task<List<CheckHistory>> SearchChecksByReferenceAsync(string reference)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<CheckHistory>>($"{BaseUrl}/search?reference={Uri.EscapeDataString(reference)}")
                       ?? new List<CheckHistory>();
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error searching checks: {err.Message}");
                return new List<CheckHistory>();
            }
        }

        // POST — insert new check
        public async Task<int> InsertCheckAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CheckIdResult>();
                    CheckID = result?.CheckId ?? 0;
                    return 1;
                }
                MessageBox.Show($"Chèque non inséré. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chèque non inséré: {ex.Message}");
                return 0;
            }
        }

        // PUT — update check
        public async Task<int> UpdateCheckAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PutAsJsonAsync($"{BaseUrl}/{this.CheckID}", payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Chèque non mis à jour. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chèque non mis à jour: {ex.Message}");
                return 0;
            }
        }

        // PUT — update status only
        public async Task<int> UpdateCheckStatusAsync(string status)
        {
            try
            {
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    $"{BaseUrl}/{this.CheckID}/status",
                    new { status = status });

                if (response.IsSuccessStatusCode)
                {
                    CheckStatus = status;
                    return 1;
                }
                MessageBox.Show($"Statut du chèque non mis à jour. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Statut du chèque non mis à jour: {ex.Message}");
                return 0;
            }
        }

        // DELETE — hard delete
        public async Task<int> DeleteCheckAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync($"{BaseUrl}/{this.CheckID}");
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Chèque non supprimé. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chèque non supprimé: {ex.Message}");
                return 0;
            }
        }

        private object BuildPayload() => new
        {
            checkReference = this.CheckReference,
            checkImagePath = this.CheckImagePath,
            checkImageBase64 = this.CheckImage != null && this.CheckImage.Length > 0
                ? Convert.ToBase64String(this.CheckImage) : null,
            invoiceID = this.InvoiceID,
            checkAmount = this.CheckAmount,
            checkDate = this.CheckDate,
            bankName = this.BankName,
            checkStatus = this.CheckStatus,
            notes = this.Notes
        };

        private class CheckIdResult
        {
            [JsonPropertyName("checkId")]
            public int CheckId { get; set; }
        }
    }
}