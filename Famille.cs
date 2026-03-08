using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Famille
    {
        [JsonPropertyName("familleId")]
        public int FamilleID { get; set; }

        [JsonPropertyName("familleName")]
        public string FamilleName { get; set; }

        [JsonPropertyName("nbrArticles")]
        public int NbrArticle { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/inventory/familles";

        // GET all active familles
        public async Task<List<Famille>> GetFamillesAsync()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Famille>>(BaseUrl)
                       ?? new List<Famille>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Famille load error: {0}", err.Message));
                return new List<Famille>();
            }
        }

        // POST — insert
        public async Task<int> InsertFamilleAsync()
        {
            try
            {
                var payload = new { familleName = this.FamilleName };
                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<FamilleIdResult>();
                    return result?.FamilleId ?? 0;
                }
                MessageBox.Show(string.Format("Famille not inserted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Famille not inserted, error: {0}", err.Message));
                return 0;
            }
        }

        // DELETE — soft delete
        public async Task<int> DeleteFamilleAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, this.FamilleID));
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Famille not deleted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Famille not deleted: {0}", err.Message));
                return 0;
            }
        }

        // PUT — update
        public async Task<int> UpdateFamilleAsync()
        {
            try
            {
                var payload = new { familleName = this.FamilleName };
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, this.FamilleID), payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Famille not updated. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Famille not updated: {0}", err.Message));
                return 0;
            }
        }

        private class FamilleIdResult
        {
            [JsonPropertyName("familleId")]
            public int FamilleId { get; set; }
        }
    }
}