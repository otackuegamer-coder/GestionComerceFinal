using GestionComerce;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Superete
{
    public class Facture
    {
        [JsonPropertyName("factureId")]
        public int FactureID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("ice")]
        public string ICE { get; set; } = "";

        [JsonPropertyName("vat")]
        public string VAT { get; set; } = "";

        [JsonPropertyName("telephone")]
        public string Telephone { get; set; } = "";

        [JsonPropertyName("adresse")]
        public string Adresse { get; set; } = "";

        public bool Etat { get; set; }

        [JsonPropertyName("companyId")]
        public string CompanyId { get; set; } = "";

        [JsonPropertyName("etatJuridic")]
        public string EtatJuridic { get; set; } = "";

        [JsonPropertyName("siegeEntreprise")]
        public string SiegeEntreprise { get; set; } = "";

        [JsonPropertyName("logoPath")]
        public string LogoPath { get; set; } = "";

        [JsonPropertyName("iF")]
        public string IF { get; set; } = "";

        [JsonPropertyName("cnss")]
        public string CNSS { get; set; } = "";

        [JsonPropertyName("rc")]
        public string RC { get; set; } = "";

        [JsonPropertyName("tp")]
        public string TP { get; set; } = "";

        [JsonPropertyName("rib")]
        public string RIB { get; set; } = "";

        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("siteWeb")]
        public string SiteWeb { get; set; } = "";

        [JsonPropertyName("patente")]
        public string Patente { get; set; } = "";

        [JsonPropertyName("capital")]
        public string Capital { get; set; } = "";

        [JsonPropertyName("fax")]
        public string Fax { get; set; } = "";

        [JsonPropertyName("ville")]
        public string Ville { get; set; } = "";

        [JsonPropertyName("codePostal")]
        public string CodePostal { get; set; } = "";

        [JsonPropertyName("bankName")]
        public string BankName { get; set; } = "";

        [JsonPropertyName("agencyCode")]
        public string AgencyCode { get; set; } = "";

        private static readonly string BaseUrl = "http://localhost:5050/api/facture/info";

        // GET active facture info
        public async Task<Facture> GetFactureAsync()
        {
            try
            {
                var result = await MainWindow.ApiClient
                    .GetFromJsonAsync<Facture>("http://localhost:5050/api/facture/info");
                return result ?? new Facture();
            }
            catch
            {
                return new Facture();
            }
        }

        // POST/PUT — upsert (API handles insert or update internally)
        public async Task<int> InsertOrUpdateFactureAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await GestionComerce.MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Facture not saved. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Facture not saved: {0}", ex.Message));
                return 0;
            }
        }

        // DELETE — soft delete
        public async Task<int> DeleteFactureAsync()
        {
            try
            {
                var response = await GestionComerce.MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, this.FactureID));
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Facture not deleted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Facture not deleted: {0}", ex.Message));
                return 0;
            }
        }

        private object BuildPayload() => new
        {
            name = this.Name,
            ice = this.ICE,
            vat = this.VAT,
            telephone = this.Telephone,
            adresse = this.Adresse,
            companyId = this.CompanyId,
            etatJuridic = this.EtatJuridic,
            siegeEntreprise = this.SiegeEntreprise,
            logoPath = this.LogoPath,
            iF = this.IF,
            cnss = this.CNSS,
            rc = this.RC,
            tp = this.TP,
            rib = this.RIB,
            email = this.Email,
            siteWeb = this.SiteWeb,
            patente = this.Patente,
            capital = this.Capital,
            fax = this.Fax,
            ville = this.Ville,
            codePostal = this.CodePostal,
            bankName = this.BankName,
            agencyCode = this.AgencyCode
        };
    }
}