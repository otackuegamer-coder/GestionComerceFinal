using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Fournisseur
    {
        [JsonPropertyName("fournisseurId")]
        public int FournisseurID { get; set; }

        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("telephone")]
        public string Telephone { get; set; }

        public bool Etat { get; set; } = true;

        [JsonPropertyName("etatJuridic")]
        public string EtatJuridic { get; set; }

        [JsonPropertyName("ice")]
        public string ICE { get; set; }

        [JsonPropertyName("siegeEntreprise")]
        public string SiegeEntreprise { get; set; }

        [JsonPropertyName("adresse")]
        public string Adresse { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/fournisseurs";

        // GET all active fournisseurs
        public async Task<List<Fournisseur>> GetFournisseursAsync()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Fournisseur>>(BaseUrl)
                       ?? new List<Fournisseur>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Fournisseur load error: {0}", err.Message));
                return new List<Fournisseur>();
            }
        }

        // POST — insert
        public async Task<int> InsertFournisseurAsync()
        {
            try
            {
                var payload = new
                {
                    nom = this.Nom,
                    telephone = this.Telephone,
                    etatJuridic = this.EtatJuridic,
                    ice = this.ICE,
                    siegeEntreprise = this.SiegeEntreprise,
                    adresse = this.Adresse,
                    code = this.Code
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FournisseurIdResult>();
                    return result?.FournisseurId ?? 0;
                }
                MessageBox.Show(string.Format("Fournisseur not inserted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Fournisseur not inserted, error: {0}", err.Message));
                return 0;
            }
        }

        // PUT — update
        public async Task<int> UpdateFournisseurAsync()
        {
            try
            {
                var payload = new
                {
                    nom = this.Nom,
                    telephone = this.Telephone,
                    etatJuridic = this.EtatJuridic,
                    ice = this.ICE,
                    siegeEntreprise = this.SiegeEntreprise,
                    adresse = this.Adresse,
                    code = this.Code
                };

                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, this.FournisseurID), payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Fournisseur not updated. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Fournisseur not updated: {0}", err.Message));
                return 0;
            }
        }

        // DELETE — soft delete
        public async Task<int> DeleteFournisseurAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, this.FournisseurID));
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Fournisseur not deleted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Fournisseur not deleted: {0}", err.Message));
                return 0;
            }
        }

        private class FournisseurIdResult
        {
            [JsonPropertyName("fournisseurId")]
            public int FournisseurId { get; set; }
        }
    }
}