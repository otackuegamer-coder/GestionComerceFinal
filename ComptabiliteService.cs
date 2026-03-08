using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using GestionComerce.Models;

namespace GestionComerce
{
    public class ComptabiliteService
    {
        private static readonly string BaseUrl = "http://localhost:5050/api/accounting";

        // ── JOURNAL — CREATE ─────────────────────────────────────────────────

        private async Task<int> CreateJournalAsync(string numPiece, DateTime date, string libelle,
            string typeOperation, string refExterne)
        {
            var payload = new
            {
                numPiece = numPiece,
                dateEcriture = date,
                libelle = libelle,
                typeOperation = typeOperation,
                refExterne = refExterne,
                remarques = (string)null
            };

            var response = await MainWindow.ApiClient.PostAsJsonAsync($"{BaseUrl}/journal", payload);
            if (!response.IsSuccessStatusCode) return 0;

            var result = await response.Content.ReadFromJsonAsync<JournalIdResult>();
            return result?.JournalId ?? 0;
        }

        private async Task<bool> AddEcritureAsync(int journalId, string codeCompte, string libelle,
            decimal debit, decimal credit, DateTime date)
        {
            var payload = new
            {
                codeCompte = codeCompte,
                libelle = libelle,
                debit = debit,
                credit = credit,
                dateEcriture = date
            };

            var response = await MainWindow.ApiClient.PostAsJsonAsync(
                $"{BaseUrl}/journal/{journalId}/ecritures", payload);
            return response.IsSuccessStatusCode;
        }

        // ── EnregistrerVente ─────────────────────────────────────────────────

        public async Task<int> EnregistrerVenteAsync(int operationID, decimal totalAmount,
            int? paymentMethodID, int? clientID, DateTime? Date)
        {
            try
            {
                string compteTresorerie = DeterminerCompteTresorerie(paymentMethodID);
                string numPiece = string.Format("VE-{0:yyyyMMdd}-{1}", DateTime.Now, operationID);
                DateTime dateEcriture = Date ?? DateTime.Now;
                string libelle = string.Format("Vente N° {0}", operationID);

                decimal montantHT = totalAmount / 1.20m;
                decimal montantTVA = totalAmount - montantHT;

                int journalID = await CreateJournalAsync(numPiece, dateEcriture, libelle, "Vente", string.Format("OP-{0}", operationID));
                if (journalID == 0) return 0;

                await AddEcritureAsync(journalID, compteTresorerie, string.Format("Encaissement vente N° {0}", operationID), totalAmount, 0, dateEcriture);
                await AddEcritureAsync(journalID, "7111", string.Format("Vente marchandises N° {0}", operationID), 0, montantHT, dateEcriture);
                await AddEcritureAsync(journalID, "4455", string.Format("TVA facturée N° {0}", operationID), 0, montantTVA, dateEcriture);

                return journalID;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur enregistrement vente: {0}", ex.Message));
                return 0;
            }
        }

        // ── EnregistrerAchat ─────────────────────────────────────────────────

        public async Task<int> EnregistrerAchatAsync(int factureID, decimal totalAmount,
            string numeroFacture, int? fournisseurID, DateTime? dateFacture)
        {
            try
            {
                string numPiece = string.Format("AC-{0:yyyyMMdd}-{1}", DateTime.Now, factureID);
                DateTime dateEcriture = dateFacture ?? DateTime.Now;
                string libelle = string.Format("Achat facture N° {0}", numeroFacture);

                decimal montantHT = totalAmount / 1.20m;
                decimal montantTVA = totalAmount - montantHT;

                int journalID = await CreateJournalAsync(numPiece, dateEcriture, libelle, "Achat", string.Format("FA-{0}", factureID));
                if (journalID == 0) return 0;

                await AddEcritureAsync(journalID, "6111", string.Format("Achats marchandises facture {0}", numeroFacture), montantHT, 0, dateEcriture);
                await AddEcritureAsync(journalID, "3455", string.Format("TVA récupérable facture {0}", numeroFacture), montantTVA, 0, dateEcriture);
                await AddEcritureAsync(journalID, "4411", string.Format("Fournisseur facture {0}", numeroFacture), 0, totalAmount, dateEcriture);

                return journalID;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur enregistrement achat: {0}", ex.Message));
                return 0;
            }
        }

        // ── EnregistrerSalaire ───────────────────────────────────────────────

        public async Task<int> EnregistrerSalaireAsync(int salaireID, int? employeID,
            decimal salaireBrut, decimal cotisationCNSS, decimal ir, decimal salaireNet,
            string nomEmploye, DateTime? datePaiement)
        {
            try
            {
                string numPiece = string.Format("SA-{0:yyyyMMdd}-{1}", DateTime.Now, salaireID);
                DateTime dateEcriture = datePaiement ?? DateTime.Now;
                string libelle = string.Format("Salaire {0} - {1:MMMM yyyy}", nomEmploye ?? "Employé", dateEcriture);

                int journalID = await CreateJournalAsync(numPiece, dateEcriture, libelle, "Salaire", string.Format("SAL-{0}", salaireID));
                if (journalID == 0) return 0;

                await AddEcritureAsync(journalID, "6171", string.Format("Salaire brut {0}", nomEmploye), salaireBrut, 0, dateEcriture);
                await AddEcritureAsync(journalID, "4441", string.Format("CNSS patronale {0}", nomEmploye), cotisationCNSS, 0, dateEcriture);
                await AddEcritureAsync(journalID, "4443", string.Format("IR retenu {0}", nomEmploye), 0, ir, dateEcriture);
                await AddEcritureAsync(journalID, "4441", string.Format("CNSS salariale {0}", nomEmploye), 0, cotisationCNSS, dateEcriture);
                await AddEcritureAsync(journalID, "5141", string.Format("Paiement salaire {0}", nomEmploye), 0, salaireNet, dateEcriture);

                return journalID;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur enregistrement salaire: {0}", ex.Message));
                return 0;
            }
        }

        // ── EnregistrerDepense ───────────────────────────────────────────────

        public async Task<int> EnregistrerDepenseAsync(int expenseID, decimal amount,
            string category, string beneficiaire, DateTime? dateExpense)
        {
            try
            {
                string compteCharge = DeterminerCompteCharge(category);
                string numPiece = string.Format("DE-{0:yyyyMMdd}-{1}", DateTime.Now, expenseID);
                DateTime dateEcriture = dateExpense ?? DateTime.Now;
                string libelle = string.Format("Dépense {0} - {1}", category, beneficiaire ?? "");

                int journalID = await CreateJournalAsync(numPiece, dateEcriture, libelle, "Dépense", string.Format("EXP-{0}", expenseID));
                if (journalID == 0) return 0;

                await AddEcritureAsync(journalID, compteCharge, string.Format("Dépense {0} N° {1}", category, expenseID), amount, 0, dateEcriture);
                await AddEcritureAsync(journalID, "5141", string.Format("Paiement dépense N° {0}", expenseID), 0, amount, dateEcriture);

                return journalID;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur enregistrement dépense: {0}", ex.Message));
                return 0;
            }
        }

        // ── ObtenirJournalGeneral ────────────────────────────────────────────

        public async Task<List<EcrituresComptables>> ObtenirJournalGeneralAsync(
            DateTime? dateDebut = null, DateTime? dateFin = null, string typeOperation = null)
        {
            try
            {
                var url = string.Format("{0}/journal", BaseUrl);
                var qs = new List<string>();
                if (dateDebut.HasValue) qs.Add(string.Format("from={0:yyyy-MM-dd}", dateDebut.Value));
                if (dateFin.HasValue) qs.Add(string.Format("to={0:yyyy-MM-dd}", dateFin.Value));
                if (!string.IsNullOrWhiteSpace(typeOperation)) qs.Add(string.Format("type={0}", Uri.EscapeDataString(typeOperation)));
                if (qs.Count > 0) url += "?" + string.Join("&", qs);

                var journals = await MainWindow.ApiClient.GetFromJsonAsync<List<JournalComptable>>(url)
                               ?? new List<JournalComptable>();

                var allEcritures = new List<EcrituresComptables>();
                foreach (var j in journals)
                {
                    var ecritures = await MainWindow.ApiClient
                        .GetFromJsonAsync<List<EcrituresComptables>>(string.Format("{0}/journal/{1}/ecritures", BaseUrl, j.JournalID))
                        ?? new List<EcrituresComptables>();

                    foreach (var e in ecritures)
                    {
                        e.TypeOperation = j.TypeOperation;
                        e.NumPiece = j.NumPiece;
                    }
                    allEcritures.AddRange(ecritures);
                }
                return allEcritures;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur lecture journal: {0}", ex.Message));
                return new List<EcrituresComptables>();
            }
        }

        // ── ValiderJournal ───────────────────────────────────────────────────

        public async Task<bool> ValiderJournalAsync(int journalID, string validePar)
        {
            try
            {
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/journal/{1}/valider", BaseUrl, journalID),
                    new { validePar = validePar });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur validation journal: {0}", ex.Message));
                return false;
            }
        }

        // ── ObtenirGrandLivreParCompte ───────────────────────────────────────

        public async Task<List<EcrituresComptables>> ObtenirGrandLivreParCompteAsync(
            string codeCompte, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            try
            {
                var url = string.Format("{0}/grand-livre/{1}", BaseUrl, Uri.EscapeDataString(codeCompte));
                var qs = new List<string>();
                if (dateDebut.HasValue) qs.Add(string.Format("from={0:yyyy-MM-dd}", dateDebut.Value));
                if (dateFin.HasValue) qs.Add(string.Format("to={0:yyyy-MM-dd}", dateFin.Value));
                if (qs.Count > 0) url += "?" + string.Join("&", qs);

                return await MainWindow.ApiClient.GetFromJsonAsync<List<EcrituresComptables>>(url)
                       ?? new List<EcrituresComptables>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur grand-livre: {0}", ex.Message));
                return new List<EcrituresComptables>();
            }
        }

        // ── CPC Detail ───────────────────────────────────────────────────────

        public async Task<CPCDetailDTO> GetCPCDetailAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var url = string.Format("{0}/cpc-detail", BaseUrl);
                var qs = new List<string>();
                if (from.HasValue) qs.Add(string.Format("from={0:yyyy-MM-dd}", from.Value));
                if (to.HasValue) qs.Add(string.Format("to={0:yyyy-MM-dd}", to.Value));
                if (qs.Count > 0) url += "?" + string.Join("&", qs);

                return await MainWindow.ApiClient.GetFromJsonAsync<CPCDetailDTO>(url)
                       ?? new CPCDetailDTO();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur CPC: {0}", ex.Message));
                return new CPCDetailDTO();
            }
        }

        // ── Sync wrappers ────────────────────────────────────────────────────

        public int EnregistrerVente(int operationID, decimal totalAmount, int? paymentMethodID, int? clientID, DateTime? Date)
            => EnregistrerVenteAsync(operationID, totalAmount, paymentMethodID, clientID, Date).GetAwaiter().GetResult();

        public int EnregistrerAchat(int factureID, decimal totalAmount, string numeroFacture, int? fournisseurID, DateTime? dateFacture)
            => EnregistrerAchatAsync(factureID, totalAmount, numeroFacture, fournisseurID, dateFacture).GetAwaiter().GetResult();

        public int EnregistrerSalaire(int salaireID, int? employeID, decimal salaireBrut, decimal cotisationCNSS,
            decimal ir, decimal salaireNet, string nomEmploye, DateTime? datePaiement)
            => EnregistrerSalaireAsync(salaireID, employeID, salaireBrut, cotisationCNSS, ir, salaireNet, nomEmploye, datePaiement).GetAwaiter().GetResult();


        // ── EnregistrerSalaire overload with 10 args (matches GestionSalaires.xaml.cs caller) ──
        public int EnregistrerSalaire(int salaireID, int? employeID, decimal salaireBrut,
            decimal cotisationCNSS, decimal cotisationPatronaleCNSS, decimal cotisationAMO,
            decimal ir, decimal salaireNet, DateTime? datePaiement, DateTime? periodeDebut)
            => EnregistrerSalaireAsync(salaireID, employeID, salaireBrut,
               cotisationCNSS + cotisationPatronaleCNSS + cotisationAMO,
               ir, salaireNet, null, datePaiement)
               .GetAwaiter().GetResult();

        public int EnregistrerDepense(int expenseID, decimal amount, string category, string beneficiaire, DateTime? dateExpense)
            => EnregistrerDepenseAsync(expenseID, amount, category, beneficiaire, dateExpense).GetAwaiter().GetResult();

        public List<EcrituresComptables> ObtenirJournalGeneral(DateTime? dateDebut = null, DateTime? dateFin = null, string typeOperation = null)
            => ObtenirJournalGeneralAsync(dateDebut, dateFin, typeOperation).GetAwaiter().GetResult();

        public bool ValiderJournal(int journalID, string validePar)
            => ValiderJournalAsync(journalID, validePar).GetAwaiter().GetResult();

        public List<EcrituresComptables> ObtenirGrandLivreParCompte(string codeCompte, DateTime? dateDebut = null, DateTime? dateFin = null)
            => ObtenirGrandLivreParCompteAsync(codeCompte, dateDebut, dateFin).GetAwaiter().GetResult();

        // ── Private helpers ──────────────────────────────────────────────────

        private string DeterminerCompteTresorerie(int? paymentMethodID)
        {
            if (!paymentMethodID.HasValue) return "5141";
            switch (paymentMethodID.Value)
            {
                case 1: return "5141";  // Espèces
                case 2: return "5141";  // Virement
                case 3: return "5141";  // Chèque
                case 4: return "5141";  // Carte bancaire
                default: return "5141";
            }
        }

        private string DeterminerCompteCharge(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return "6119";
            switch (category.ToLower())
            {
                case "loyer": return "6132";
                case "electricite": return "6125";
                case "eau": return "6125";
                case "telephone": return "6135";
                case "internet": return "6135";
                case "transport": return "6142";
                case "fournitures": return "6123";
                case "maintenance": return "6152";
                case "publicite": return "6141";
                case "assurance": return "6134";
                case "formation": return "6183";
                default: return "6119";
            }
        }


        // ── GenererBilan (sync wrapper for BilanView) ─────────────────────────────
        public BilanDTO GenererBilan(DateTime asOf)
        {
            try
            {
                string url = string.Format("{0}/bilan?asOf={1:yyyy-MM-dd}", BaseUrl, asOf);
                var task = MainWindow.ApiClient.GetFromJsonAsync<BilanDTO>(url);
                return task.GetAwaiter().GetResult() ?? new BilanDTO();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur bilan: {0}", ex.Message));
                return new BilanDTO();
            }
        }

        // ── GenererDashboardFinancier (sync wrapper for DashboardFinancierView) ───
        public DashboardFinancierDTO GenererDashboardFinancier(DateTime dateDebut, DateTime dateFin)
        {
            try
            {
                string url = string.Format("{0}/dashboard?from={1:yyyy-MM-dd}&to={2:yyyy-MM-dd}",
                    BaseUrl, dateDebut, dateFin);
                var task = MainWindow.ApiClient.GetFromJsonAsync<DashboardFinancierDTO>(url);
                return task.GetAwaiter().GetResult() ?? new DashboardFinancierDTO();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur dashboard: {0}", ex.Message));
                return new DashboardFinancierDTO();
            }
        }

        // ── GetCPCData (sync wrapper for CPCView) ─────────────────────────────────
        public CPCDetailDTO GetCPCData(DateTime dateDebut, DateTime dateFin)
        {
            try
            {
                var task = GetCPCDetailAsync(dateDebut, dateFin);
                return task.GetAwaiter().GetResult() ?? new CPCDetailDTO();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur CPC: {0}", ex.Message));
                return new CPCDetailDTO();
            }
        }

        // ── Internal result DTO ──────────────────────────────────────────────

        private class JournalIdResult
        {
            [JsonPropertyName("journalId")]
            public int JournalId { get; set; }
        }
    }
}