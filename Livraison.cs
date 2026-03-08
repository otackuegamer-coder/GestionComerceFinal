using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Livraison
    {
        [JsonPropertyName("livraisonId")] public int LivraisonID { get; set; }
        [JsonPropertyName("operationId")] public int OperationID { get; set; }
        [JsonPropertyName("clientId")] public int? ClientID { get; set; }
        [JsonPropertyName("clientNom")] public string ClientNom { get; set; } = string.Empty;
        [JsonPropertyName("clientTelephone")] public string ClientTelephone { get; set; } = string.Empty;
        [JsonPropertyName("adresseLivraison")] public string AdresseLivraison { get; set; } = string.Empty;
        [JsonPropertyName("ville")] public string Ville { get; set; } = string.Empty;
        [JsonPropertyName("codePostal")] public string CodePostal { get; set; } = string.Empty;
        [JsonPropertyName("zoneLivraison")] public string ZoneLivraison { get; set; } = string.Empty;
        [JsonPropertyName("fraisLivraison")] public decimal FraisLivraison { get; set; }
        [JsonPropertyName("dateCommande")] public DateTime DateCommande { get; set; }
        [JsonPropertyName("dateLivraisonPrevue")] public DateTime? DateLivraisonPrevue { get; set; }
        [JsonPropertyName("dateLivraisonEffective")] public DateTime? DateLivraisonEffective { get; set; }
        [JsonPropertyName("livreurId")] public int? LivreurID { get; set; }
        [JsonPropertyName("livreurNom")] public string LivreurNom { get; set; } = string.Empty;
        [JsonPropertyName("statut")] public string Statut { get; set; } = "en_attente";
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
        [JsonPropertyName("totalCommande")] public decimal TotalCommande { get; set; }
        [JsonPropertyName("modePaiement")] public string ModePaiement { get; set; } = string.Empty;
        [JsonPropertyName("paiementStatut")] public string PaiementStatut { get; set; } = "non_paye";

        public bool Etat { get; set; } = true;
        public DateTime DateCreation { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/livraisons";

        // GET all livraisons (optionally filter by statut and/or livreurId)
        public async Task<List<Livraison>> GetLivraisonsAsync(string statutFilter = null, int? livreurIdFilter = null)
        {
            try
            {
                var url = BaseUrl;
                var qs = new System.Text.StringBuilder();
                if (!string.IsNullOrWhiteSpace(statutFilter))
                    qs.Append(string.Format("statut={0}&", Uri.EscapeDataString(statutFilter)));
                if (livreurIdFilter.HasValue)
                    qs.Append(string.Format("livreurId={0}&", livreurIdFilter.Value));
                if (qs.Length > 0)
                    url = string.Format("{0}?{1}", BaseUrl, qs.ToString().TrimEnd('&'));

                return await MainWindow.ApiClient.GetFromJsonAsync<List<Livraison>>(url)
                       ?? new List<Livraison>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livraisons load error: {0}", err.Message));
                return new List<Livraison>();
            }
        }

        // GET single livraison by ID
        public async Task<Livraison> GetLivraisonByIDAsync(int livraisonId)
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<Livraison>(
                    string.Format("{0}/{1}", BaseUrl, livraisonId));
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livraison load error: {0}", err.Message));
                return null;
            }
        }

        // POST — insert
        public async Task<int> InsertLivraisonAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LivraisonIdResult>();
                    return result?.LivraisonId ?? 0;
                }
                MessageBox.Show(string.Format("Livraison not inserted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livraison not inserted, error: {0}", err.Message));
                return 0;
            }
        }

        // PUT — full update
        public async Task<int> UpdateLivraisonAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, this.LivraisonID), payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Livraison not updated. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livraison not updated: {0}", err.Message));
                return 0;
            }
        }

        // PATCH — update statut only (dedicated endpoint: PATCH /api/livraisons/{id}/statut)
        public async Task<int> UpdateStatutAsync(string nouveauStatut, string commentaire = "")
        {
            try
            {
                var payload = new { statut = nouveauStatut, commentaire = commentaire };
                var request = new HttpRequestMessage(
                    new HttpMethod("PATCH"),
                    string.Format("{0}/{1}/statut", BaseUrl, this.LivraisonID))
                {
                    Content = System.Net.Http.Json.JsonContent.Create(payload)
                };
                var response = await MainWindow.ApiClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    this.Statut = nouveauStatut;
                    return 1;
                }
                MessageBox.Show(string.Format("Statut not updated. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Statut not updated: {0}", err.Message));
                return 0;
            }
        }

        // DELETE — soft delete
        public async Task<int> DeleteLivraisonAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, this.LivraisonID));
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Livraison not deleted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Livraison not deleted: {0}", err.Message));
                return 0;
            }
        }

        // GET historique for this livraison
        public async Task<List<LivraisonHistorique>> GetHistoriqueAsync()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<LivraisonHistorique>>(
                    string.Format("{0}/{1}/historique", BaseUrl, this.LivraisonID))
                       ?? new List<LivraisonHistorique>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Historique load error: {0}", err.Message));
                return new List<LivraisonHistorique>();
            }
        }

        // GET statistics — API endpoint: GET /api/livraisons/stats?from=&to=
        public async Task<LivraisonStatistiques> GetStatistiquesAsync(DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            try
            {
                var url = string.Format("{0}/stats", BaseUrl);
                var qs = new System.Text.StringBuilder();
                if (dateDebut.HasValue) qs.Append(string.Format("from={0:yyyy-MM-dd}&", dateDebut.Value));
                if (dateFin.HasValue) qs.Append(string.Format("to={0:yyyy-MM-dd}&", dateFin.Value));
                if (qs.Length > 0) url = string.Format("{0}?{1}", url, qs.ToString().TrimEnd('&'));

                return await MainWindow.ApiClient.GetFromJsonAsync<LivraisonStatistiques>(url)
                       ?? new LivraisonStatistiques();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Statistiques load error: {0}", err.Message));
                return new LivraisonStatistiques();
            }
        }

        private object BuildPayload() => new
        {
            operationId = this.OperationID,
            clientID = this.ClientID,
            clientNom = this.ClientNom,
            clientTelephone = this.ClientTelephone,
            adresseLivraison = this.AdresseLivraison,
            ville = this.Ville,
            codePostal = this.CodePostal,
            zoneLivraison = this.ZoneLivraison,
            fraisLivraison = this.FraisLivraison,
            dateLivraisonPrevue = this.DateLivraisonPrevue,
            livreurID = this.LivreurID,
            livreurNom = this.LivreurNom,
            statut = this.Statut,
            notes = this.Notes,
            totalCommande = this.TotalCommande,
            modePaiement = this.ModePaiement,
            paiementStatut = this.PaiementStatut
        };

        private class LivraisonIdResult
        {
            [JsonPropertyName("livraisonId")]
            public int LivraisonId { get; set; }
        }
    }

    public class LivraisonHistorique
    {
        [JsonPropertyName("historiqueId")] public int HistoriqueID { get; set; }
        [JsonPropertyName("livraisonId")] public int LivraisonID { get; set; }
        [JsonPropertyName("ancienStatut")] public string AncienStatut { get; set; } = string.Empty;
        [JsonPropertyName("nouveauStatut")] public string NouveauStatut { get; set; } = string.Empty;
        [JsonPropertyName("commentaire")] public string Commentaire { get; set; } = string.Empty;
        [JsonPropertyName("dateChangement")] public DateTime DateChangement { get; set; }
    }

    public class LivraisonStatistiques
    {
        [JsonPropertyName("totalLivraisons")] public int TotalLivraisons { get; set; }
        [JsonPropertyName("livrees")] public int Livrees { get; set; }
        [JsonPropertyName("enCours")] public int EnCours { get; set; }
        [JsonPropertyName("enAttente")] public int EnAttente { get; set; }
        [JsonPropertyName("annulees")] public int Annulees { get; set; }
        [JsonPropertyName("totalVentes")] public decimal TotalVentes { get; set; }
        [JsonPropertyName("totalFrais")] public decimal TotalFrais { get; set; }
        [JsonPropertyName("fraisMoyen")] public decimal FraisMoyen { get; set; }
        // Alias: CLivraison.xaml.cs references TotalFraisLivraison; maps to TotalFrais
        public decimal TotalFraisLivraison => TotalFrais;
    }
}