using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    // =============================================
    // ACCOUNTING MODELS
    // =============================================

    public class PlanComptable
    {
        public string CodeCompte { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public int Classe { get; set; }
        public string TypeCompte { get; set; } = string.Empty;
        public string SensNormal { get; set; } = string.Empty;
        public bool EstActif { get; set; }
        public DateTime DateCreation { get; set; }

        // Shares Operation.ApiBaseUrl / Operation.BearerToken
        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(Operation.ApiBaseUrl);
            if (!string.IsNullOrEmpty(Operation.BearerToken))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Operation.BearerToken);
            return client;
        }

        // GET ALL
        public static async Task<List<PlanComptable>> GetAllAsync(int? classe = null)
        {
            try
            {
                var url = "/api/accounting/plan-comptable";
                if (classe.HasValue) url += "?classe=" + classe.Value;

                using (var client = CreateClient())
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var raw = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<PlanComptable>>(raw, _json);
                    return list ?? new List<PlanComptable>();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("PlanComptable fetch failed: " + err.Message);
                return new List<PlanComptable>();
            }
        }

        // GET BY CODE
        public static async Task<PlanComptable> GetByCodeAsync(string code)
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = await client.GetAsync(
                        "/api/accounting/plan-comptable/" + Uri.EscapeDataString(code));
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                    response.EnsureSuccessStatusCode();
                    var raw = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<PlanComptable>(raw, _json);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("PlanComptable fetch failed: " + err.Message);
                return null;
            }
        }

        // INSERT
        public async Task<bool> InsertAsync()
        {
            try
            {
                using (var client = CreateClient())
                {
                    var dto = new PlanComptableDto
                    {
                        CodeCompte = CodeCompte,
                        Libelle = Libelle,
                        Classe = Classe,
                        TypeCompte = TypeCompte,
                        SensNormal = SensNormal,
                        EstActif = EstActif
                    };
                    var content = new StringContent(
                        JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("/api/accounting/plan-comptable", content);
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("PlanComptable not inserted: " + err.Message);
                return false;
            }
        }

        // UPDATE
        public async Task<bool> UpdateAsync()
        {
            try
            {
                using (var client = CreateClient())
                {
                    var dto = new PlanComptableDto
                    {
                        CodeCompte = CodeCompte,
                        Libelle = Libelle,
                        Classe = Classe,
                        TypeCompte = TypeCompte,
                        SensNormal = SensNormal,
                        EstActif = EstActif
                    };
                    var content = new StringContent(
                        JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                    var response = await client.PutAsync(
                        "/api/accounting/plan-comptable/" + Uri.EscapeDataString(CodeCompte), content);
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("PlanComptable not updated: " + err.Message);
                return false;
            }
        }

        // DELETE
        public async Task<bool> DeleteAsync()
        {
            try
            {
                using (var client = CreateClient())
                {
                    var response = await client.DeleteAsync(
                        "/api/accounting/plan-comptable/" + Uri.EscapeDataString(CodeCompte));
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("PlanComptable not deleted: " + err.Message);
                return false;
            }
        }
    }

    internal class PlanComptableDto
    {
        public string CodeCompte { get; set; }
        public string Libelle { get; set; }
        public int Classe { get; set; }
        public string TypeCompte { get; set; }
        public string SensNormal { get; set; }
        public bool EstActif { get; set; }
    }

    public class JournalComptable
    {
        public int JournalID { get; set; }
        public string NumPiece { get; set; } = string.Empty;
        public DateTime DateEcriture { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string TypeOperation { get; set; } = string.Empty;
        public string RefExterne { get; set; } = string.Empty;
        public bool EstValide { get; set; }
        public DateTime? DateValidation { get; set; }
        public string ValidePar { get; set; } = string.Empty;
        public string Remarques { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public List<EcrituresComptables> Ecritures { get; set; } = new List<EcrituresComptables>();
    }

    public class EcrituresComptables
    {
        public int EcritureID { get; set; }
        public int JournalID { get; set; }
        public string CodeCompte { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public DateTime DateEcriture { get; set; }
        public string LibelleCompte { get; set; } = string.Empty;
        // Added: populated by ComptabiliteService from the parent JournalComptable
        public string NumPiece { get; set; } = string.Empty;
        public string TypeOperation { get; set; } = string.Empty;
    }

    public class ExerciceComptable
    {
        public int ExerciceID { get; set; }
        public int Annee { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public bool EstCloture { get; set; }
        public DateTime? DateCloture { get; set; }
    }

    // =============================================
    // DTOs FOR FINANCIAL REPORTS
    // =============================================

    public class BilanDTO
    {
        public DateTime DateBilan { get; set; }
        public List<BilanLigneDTO> Actifs { get; set; } = new List<BilanLigneDTO>();
        public List<BilanLigneDTO> Passifs { get; set; } = new List<BilanLigneDTO>();
        public decimal TotalActif { get; set; }
        public decimal TotalPassif { get; set; }
    }

    public class BilanLigneDTO
    {
        public string CodeCompte { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public int Classe { get; set; }
    }

    public class CPCDTO
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public List<CPCLigneDTO> Produits { get; set; } = new List<CPCLigneDTO>();
        public List<CPCLigneDTO> Charges { get; set; } = new List<CPCLigneDTO>();
        public decimal TotalProduits { get; set; }
        public decimal TotalCharges { get; set; }
        public decimal ResultatNet { get; set; }
    }

    public class CPCLigneDTO
    {
        public string CodeCompte { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public int Classe { get; set; }
    }

    public class DashboardFinancierDTO
    {
        public decimal TotalVentes { get; set; }
        public decimal TotalAchats { get; set; }
        public decimal TotalSalaires { get; set; }
        public decimal TotalDepenses { get; set; }
        public decimal BeneficeNet { get; set; }
        public decimal TresorerieCaisse { get; set; }
        public decimal TresorerieBanque { get; set; }
        public decimal TresorerieTotale { get; set; }
    }
}