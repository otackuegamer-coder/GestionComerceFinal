using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class FactureSettings
    {
        [JsonPropertyName("factureSettingsId")]
        public int FactureSettingsID { get; set; }

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }

        [JsonPropertyName("companyAddress")]
        public string CompanyAddress { get; set; }

        [JsonPropertyName("companyPhone")]
        public string CompanyPhone { get; set; }

        [JsonPropertyName("companyEmail")]
        public string CompanyEmail { get; set; }

        [JsonPropertyName("logoPath")]
        public string LogoPath { get; set; }

        [JsonPropertyName("invoicePrefix")]
        public string InvoicePrefix { get; set; }

        [JsonPropertyName("taxPercentage")]
        public decimal TaxPercentage { get; set; }

        [JsonPropertyName("termsAndConditions")]
        public string TermsAndConditions { get; set; }

        [JsonPropertyName("footerText")]
        public string FooterText { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/facture/settings";

        private static FactureSettings GetDefaults()
        {
            return new FactureSettings
            {
                CompanyName = "NOM DE L'ENTREPRISE",
                CompanyAddress = "Adresse de l'entreprise",
                CompanyPhone = "0600000000",
                CompanyEmail = "email@entreprise.ma",
                LogoPath = "",
                InvoicePrefix = "FAC-",
                TaxPercentage = 20,
                TermsAndConditions = "",
                FooterText = "MERCI DE VOTRE VISITE"
            };
        }

        // GET current settings
        public static async Task<FactureSettings> GetFactureSettingsAsync()
        {
            try
            {
                var settings = await MainWindow.ApiClient.GetFromJsonAsync<FactureSettings>(BaseUrl);
                if (settings == null || settings.FactureSettingsID == 0)
                    return GetDefaults();
                return settings;
            }
            catch
            {
                return GetDefaults();
            }
        }

        // Alias for compatibility
        public static async Task<FactureSettings> LoadSettingsAsync()
        {
            return await GetFactureSettingsAsync();
        }

        // POST — upsert (API handles insert or update internally)
        public async Task<int> SaveFactureSettingsAsync()
        {
            try
            {
                var payload = new
                {
                    companyName = this.CompanyName ?? "",
                    companyAddress = this.CompanyAddress ?? "",
                    companyPhone = this.CompanyPhone ?? "",
                    companyEmail = this.CompanyEmail ?? "",
                    logoPath = this.LogoPath ?? "",
                    invoicePrefix = this.InvoicePrefix ?? "FAC-",
                    taxPercentage = this.TaxPercentage,
                    termsAndConditions = this.TermsAndConditions ?? "",
                    footerText = this.FooterText ?? ""
                };

                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Erreur lors de l'enregistrement des paramètres. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Erreur lors de l'enregistrement des paramètres de facture: {0}", err.Message));
                return 0;
            }
        }
    }
}