using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Client
    {
        [JsonPropertyName("clientId")]
        public int ClientID { get; set; }

        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("telephone")]
        public string Telephone { get; set; }

        [JsonPropertyName("adresse")]
        public string Adresse { get; set; }

        [JsonPropertyName("isCompany")]
        public bool IsCompany { get; set; }

        [JsonPropertyName("etatJuridique")]
        public string EtatJuridique { get; set; }

        [JsonPropertyName("ice")]
        public string ICE { get; set; }

        [JsonPropertyName("siegeEntreprise")]
        public string SiegeEntreprise { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        public bool Etat { get; set; } = true;

        [JsonPropertyName("remise")]
        public decimal? Remise { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/clients";

        // GET all active clients
        public async Task<List<Client>> GetClientsAsync()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Client>>(BaseUrl)
                       ?? new List<Client>();
            }
            catch (Exception err)
            {
                MessageBox.Show($"Error loading clients: {err.Message}");
                return new List<Client>();
            }
        }

        // POST — insert new client
        public async Task<int> InsertClientAsync()
        {
            try
            {
                var payload = new
                {
                    nom = this.Nom,
                    telephone = this.Telephone,
                    adresse = this.Adresse,
                    isCompany = this.IsCompany,
                    etatJuridique = this.EtatJuridique,
                    ice = this.ICE,
                    siegeEntreprise = this.SiegeEntreprise,
                    code = this.Code,
                    remise = this.Remise
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ClientIdResult>();
                    return result?.ClientId ?? 0;
                }
                MessageBox.Show($"Client not inserted. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Client not inserted, error: {err.Message}");
                return 0;
            }
        }

        // PUT — update existing client
        public async Task<int> UpdateClientAsync()
        {
            try
            {
                var payload = new
                {
                    nom = this.Nom,
                    telephone = this.Telephone,
                    adresse = this.Adresse,
                    isCompany = this.IsCompany,
                    etatJuridique = this.EtatJuridique,
                    ice = this.ICE,
                    siegeEntreprise = this.SiegeEntreprise,
                    code = this.Code,
                    remise = this.Remise
                };

                var response = await MainWindow.ApiClient.PutAsJsonAsync($"{BaseUrl}/{this.ClientID}", payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Client not updated. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Client not updated: {err.Message}");
                return 0;
            }
        }

        // DELETE — soft delete
        public async Task<int> DeleteClientAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync($"{BaseUrl}/{this.ClientID}");
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show($"Client not deleted. Status: {response.StatusCode}");
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show($"Client not deleted: {err.Message}");
                return 0;
            }
        }

        // Helper DTO for insert response
        private class ClientIdResult
        {
            [JsonPropertyName("clientId")]
            public int ClientId { get; set; }
        }
    }
}