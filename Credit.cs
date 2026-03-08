using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Credit
    {
        [JsonPropertyName("creditId")]
        public int CreditID { get; set; }

        [JsonPropertyName("clientId")]
        public int? ClientID { get; set; }

        [JsonPropertyName("fournisseurId")]
        public int? FournisseurID { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("paye")]
        public decimal Paye { get; set; }

        [JsonPropertyName("difference")]
        public decimal Difference { get; set; }

        public bool Etat { get; set; } = true;

        private static readonly string BaseUrl = "http://localhost:5050/api/credits";

        // GET all active credits (optionally filter by clientId or fournisseurId)
        public async Task<List<Credit>> GetCreditsAsync(int? clientId = null, int? fournisseurId = null)
        {
            try
            {
                var url = BaseUrl;
                if (clientId.HasValue)
                    url += $"?clientId={clientId.Value}";
                else if (fournisseurId.HasValue)
                    url += $"?fournisseurId={fournisseurId.Value}";

                return await MainWindow.ApiClient.GetFromJsonAsync<List<Credit>>(url)
                       ?? new List<Credit>();
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error loading credits: {err.Message}");
                return new List<Credit>();
            }
        }

        // POST — insert new credit
        public async Task<int> InsertCreditAsync()
        {
            try
            {
                var payload = new
                {
                    clientId = this.ClientID,
                    fournisseurId = this.FournisseurID,
                    total = this.Total,
                    paye = this.Paye
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreditIdResult>();
                    return result?.CreditId ?? 0;
                }
                MessageBox.Show($"Credit not inserted. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Credit not inserted, error: {err.Message}");
                return 0;
            }
        }

        // PUT — update existing credit
        public async Task<int> UpdateCreditAsync()
        {
            try
            {
                var payload = new
                {
                    clientId = this.ClientID,
                    fournisseurId = this.FournisseurID,
                    total = this.Total,
                    paye = this.Paye
                };

                var response = await MainWindow.ApiClient.PutAsJsonAsync($"{BaseUrl}/{this.CreditID}", payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Credit not updated. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Credit not updated: {err.Message}");
                return 0;
            }
        }

        // DELETE — soft delete + nulls related operation rows
        public async Task<int> DeleteCreditAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync($"{BaseUrl}/{this.CreditID}");
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Credit not deleted. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Credit not deleted: {err.Message}");
                return 0;
            }
        }

        private class CreditIdResult
        {
            [JsonPropertyName("creditId")]
            public int CreditId { get; set; }
        }
    }
}