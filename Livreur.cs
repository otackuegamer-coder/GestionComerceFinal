using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Livreur
    {
        [JsonPropertyName("livreurId")] public int LivreurID { get; set; }
        [JsonPropertyName("nom")] public string Nom { get; set; } = string.Empty;
        [JsonPropertyName("prenom")] public string Prenom { get; set; } = string.Empty;
        [JsonPropertyName("telephone")] public string Telephone { get; set; } = string.Empty;
        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
        [JsonPropertyName("vehiculeType")] public string VehiculeType { get; set; } = string.Empty;
        [JsonPropertyName("vehiculeImmatriculation")] public string VehiculeImmatriculation { get; set; } = string.Empty;
        [JsonPropertyName("statut")] public string Statut { get; set; } = "disponible";
        [JsonPropertyName("zoneCouverture")] public string ZoneCouverture { get; set; } = string.Empty;
        [JsonPropertyName("dateEmbauche")] public DateTime? DateEmbauche { get; set; }
        [JsonPropertyName("actif")] public bool Actif { get; set; } = true;

        // Photo is not returned by API — kept locally
        public string Photo { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }

        // Computed (unchanged)
        public string NomComplet { get { return string.Format("{0} {1}", Prenom, Nom); } }

        private static readonly string BaseUrl = "http://localhost:5050/api/livreurs";

        // GET all livreurs (actifSeulement has no matching API filter — API returns all, filter client-side)
        public async Task<List<Livreur>> GetLivreursAsync(bool actifSeulement = true)
        {
            try
            {
                var list = await MainWindow.ApiClient.GetFromJsonAsync<List<Livreur>>(BaseUrl)
                           ?? new List<Livreur>();
                if (actifSeulement)
                    return list.FindAll(l => l.Actif);
                return list;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livreurs load error: {0}", err.Message));
                return new List<Livreur>();
            }
        }

        // GET single livreur by ID
        public async Task<Livreur> GetLivreurByIDAsync(int livreurId)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<Livreur>(
                    string.Format("{0}/{1}", BaseUrl, livreurId));
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livreur load error: {0}", err.Message));
                return null;
            }
        }

        // GET only disponibles
        public async Task<List<Livreur>> GetLivreursDisponiblesAsync()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Livreur>>(
                    string.Format("{0}/disponibles", BaseUrl))
                       ?? new List<Livreur>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livreurs disponibles load error: {0}", err.Message));
                return new List<Livreur>();
            }
        }

        // POST — insert
        public async Task<int> InsertLivreurAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LivreurIdResult>();
                    return result?.LivreurId ?? 0;
                }
                MessageBox.Show(string.Format("Livreur not inserted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livreur not inserted, error: {0}", err.Message));
                return 0;
            }
        }

        // PUT — full update
        public async Task<int> UpdateLivreurAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, this.LivreurID), payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Livreur not updated. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livreur not updated: {0}", err.Message));
                return 0;
            }
        }

        // PUT — update statut only (uses same PUT endpoint, sends statut in payload)
        // Note: API has no dedicated PATCH statut for livreur — use full update
        public async Task<int> UpdateStatutAsync(string nouveauStatut)
        {
            this.Statut = nouveauStatut;
            return await UpdateLivreurAsync();
        }

        // DELETE — soft delete (sets Actif=0)
        public async Task<int> DeleteLivreurAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, this.LivreurID));
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Livreur not deleted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livreur not deleted: {0}", err.Message));
                return 0;
            }
        }

        // GET livraisons for this livreur
        public async Task<List<Livraison>> GetLivraisonsAsync(string statutFilter = null)
        {
            try
            {
                var url = string.Format("http://localhost:5050/api/livraisons?livreurId={0}", this.LivreurID);
                if (!string.IsNullOrWhiteSpace(statutFilter))
                    url += string.Format("&statut={0}", Uri.EscapeDataString(statutFilter));
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Livraison>>(url)
                       ?? new List<Livraison>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livraisons load error: {0}", err.Message));
                return new List<Livraison>();
            }
        }

        // GET statistics — not a dedicated endpoint in API; computed client-side from livraisons
        public async Task<LivreurStatistiques> GetStatistiquesAsync(DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            var livraisons = await GetLivraisonsAsync();
            var stats = new LivreurStatistiques();
            foreach (var l in livraisons)
            {
                if (dateDebut.HasValue && l.DateCommande < dateDebut.Value) continue;
                if (dateFin.HasValue && l.DateCommande > dateFin.Value) continue;
                stats.TotalLivraisons++;
                if (l.Statut == "livree") stats.Livrees++;
                if (l.Statut == "en_cours") stats.EnCours++;
                if (l.Statut == "annulee") stats.Annulees++;
                stats.TotalVentes += l.TotalCommande;
                stats.TotalFrais += l.FraisLivraison;
            }
            return stats;
        }

        private object BuildPayload() => new
        {
            nom = this.Nom,
            prenom = this.Prenom,
            telephone = this.Telephone,
            email = this.Email,
            vehiculeType = this.VehiculeType,
            vehiculeImmatriculation = this.VehiculeImmatriculation,
            statut = this.Statut,
            zoneCouverture = this.ZoneCouverture,
            dateEmbauche = this.DateEmbauche,
            actif = this.Actif
        };

        private class LivreurIdResult
        {
            [JsonPropertyName("livreurId")]
            public int LivreurId { get; set; }
        }
    }

    public class LivreurStatistiques
    {
        public int TotalLivraisons { get; set; }
        public int Livrees { get; set; }
        public int EnCours { get; set; }
        public int Annulees { get; set; }
        public decimal TotalVentes { get; set; }
        public decimal TotalFrais { get; set; }
    }
}