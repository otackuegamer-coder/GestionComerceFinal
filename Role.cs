using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce
{
    public class Role
    {
        [JsonPropertyName("roleId")] public int RoleID { get; set; }
        [JsonPropertyName("roleName")] public string RoleName { get; set; }
        [JsonPropertyName("createClient")] public bool CreateClient { get; set; }
        [JsonPropertyName("modifyClient")] public bool ModifyClient { get; set; }
        [JsonPropertyName("deleteClient")] public bool DeleteClient { get; set; }
        [JsonPropertyName("viewOperationClient")] public bool ViewOperationClient { get; set; }
        [JsonPropertyName("payeClient")] public bool PayeClient { get; set; }
        [JsonPropertyName("viewClient")] public bool ViewClient { get; set; }
        [JsonPropertyName("createFournisseur")] public bool CreateFournisseur { get; set; }
        [JsonPropertyName("modifyFournisseur")] public bool ModifyFournisseur { get; set; }
        [JsonPropertyName("deleteFournisseur")] public bool DeleteFournisseur { get; set; }
        [JsonPropertyName("viewOperationFournisseur")] public bool ViewOperationFournisseur { get; set; }
        [JsonPropertyName("payeFournisseur")] public bool PayeFournisseur { get; set; }
        [JsonPropertyName("viewFournisseur")] public bool ViewFournisseur { get; set; }
        [JsonPropertyName("reverseOperation")] public bool ReverseOperation { get; set; }
        [JsonPropertyName("reverseMouvment")] public bool ReverseMouvment { get; set; }
        [JsonPropertyName("viewOperation")] public bool ViewOperation { get; set; }
        [JsonPropertyName("viewMouvment")] public bool ViewMouvment { get; set; }
        [JsonPropertyName("viewProjectManagment")] public bool ViewProjectManagment { get; set; }
        [JsonPropertyName("viewSettings")] public bool ViewSettings { get; set; }
        [JsonPropertyName("viewUsers")] public bool ViewUsers { get; set; }
        [JsonPropertyName("editUsers")] public bool EditUsers { get; set; }
        [JsonPropertyName("deleteUsers")] public bool DeleteUsers { get; set; }
        [JsonPropertyName("addUsers")] public bool AddUsers { get; set; }
        [JsonPropertyName("viewRoles")] public bool ViewRoles { get; set; }
        [JsonPropertyName("addRoles")] public bool AddRoles { get; set; }
        [JsonPropertyName("deleteRoles")] public bool DeleteRoles { get; set; }
        [JsonPropertyName("viewFamilly")] public bool ViewFamilly { get; set; }
        [JsonPropertyName("editFamilly")] public bool EditFamilly { get; set; }
        [JsonPropertyName("deleteFamilly")] public bool DeleteFamilly { get; set; }
        [JsonPropertyName("addFamilly")] public bool AddFamilly { get; set; }
        [JsonPropertyName("addArticle")] public bool AddArticle { get; set; }
        [JsonPropertyName("deleteArticle")] public bool DeleteArticle { get; set; }
        [JsonPropertyName("editArticle")] public bool EditArticle { get; set; }
        [JsonPropertyName("viewArticle")] public bool ViewArticle { get; set; }
        [JsonPropertyName("repport")] public bool Repport { get; set; }
        [JsonPropertyName("ticket")] public bool Ticket { get; set; }
        [JsonPropertyName("solderFournisseur")] public bool SolderFournisseur { get; set; }
        [JsonPropertyName("solderClient")] public bool SolderClient { get; set; }
        [JsonPropertyName("viewFactureSettings")] public bool ViewFactureSettings { get; set; }
        [JsonPropertyName("modifyFactureSettings")] public bool ModifyFactureSettings { get; set; }
        [JsonPropertyName("viewFacture")] public bool ViewFacture { get; set; }
        [JsonPropertyName("viewPaymentMethod")] public bool ViewPaymentMethod { get; set; }
        [JsonPropertyName("addPaymentMethod")] public bool AddPaymentMethod { get; set; }
        [JsonPropertyName("modifyPaymentMethod")] public bool ModifyPaymentMethod { get; set; }
        [JsonPropertyName("deletePaymentMethod")] public bool DeletePaymentMethod { get; set; }
        [JsonPropertyName("viewApropos")] public bool ViewApropos { get; set; }
        [JsonPropertyName("logout")] public bool Logout { get; set; }
        [JsonPropertyName("viewExit")] public bool ViewExit { get; set; }
        [JsonPropertyName("viewShutDown")] public bool ViewShutDown { get; set; }
        [JsonPropertyName("viewClientsPage")] public bool ViewClientsPage { get; set; }
        [JsonPropertyName("viewFournisseurPage")] public bool ViewFournisseurPage { get; set; }
        [JsonPropertyName("viewInventrory")] public bool ViewInventrory { get; set; }
        [JsonPropertyName("viewVente")] public bool ViewVente { get; set; }
        [JsonPropertyName("cashClient")] public bool CashClient { get; set; }
        [JsonPropertyName("cashFournisseur")] public bool CashFournisseur { get; set; }
        [JsonPropertyName("viewCreditClient")] public bool ViewCreditClient { get; set; }
        [JsonPropertyName("viewCreditFournisseur")] public bool ViewCreditFournisseur { get; set; }
        [JsonPropertyName("viewLivraison")] public bool ViewLivraison { get; set; }
        [JsonPropertyName("accessFacturation")] public bool AccessFacturation { get; set; }
        [JsonPropertyName("createFacture")] public bool CreateFacture { get; set; }
        [JsonPropertyName("historiqueFacture")] public bool HistoriqueFacture { get; set; }
        [JsonPropertyName("historyCheck")] public bool HistoryCheck { get; set; }
        [JsonPropertyName("factureEnregistrees")] public bool FactureEnregistrees { get; set; }
        [JsonPropertyName("accessLivraison")] public bool AccessLivraison { get; set; }
        [JsonPropertyName("creationLivraison")] public bool CreationLivraison { get; set; }
        [JsonPropertyName("gestionLivreur")] public bool GestionLivreur { get; set; }

        private static readonly string BaseUrl = "http://localhost:5050/api/roles";

        // GET all roles
        public async Task<List<Role>> GetRolesAsync()
        {
            try
            {
                return await MainWindow.ApiClient.GetFromJsonAsync<List<Role>>(BaseUrl)
                       ?? new List<Role>();
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Roles load error: {0}", err.Message));
                return new List<Role>();
            }
        }

        // POST — insert
        public async Task<int> InsertRoleAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PostAsJsonAsync(BaseUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RoleIdResult>();
                    return result?.RoleId ?? 0;
                }
                MessageBox.Show(string.Format("Role not inserted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Role not inserted, error: {0}", err.Message));
                return 0;
            }
        }

        // PUT — update
        public async Task<int> UpdateRoleAsync()
        {
            try
            {
                var payload = BuildPayload();
                var response = await MainWindow.ApiClient.PutAsJsonAsync(
                    string.Format("{0}/{1}", BaseUrl, this.RoleID), payload);
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Role not updated. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Role not updated: {0}", err.Message));
                return 0;
            }
        }

        // DELETE
        public async Task<int> DeleteRoleAsync()
        {
            try
            {
                var response = await MainWindow.ApiClient.DeleteAsync(
                    string.Format("{0}/{1}", BaseUrl, this.RoleID));
                if (response.IsSuccessStatusCode) return 1;
                MessageBox.Show(string.Format("Role not deleted. Status: {0}", response.StatusCode));
                return 0;
            }
            catch (Exception err)
            {
                MessageBox.Show(string.Format("Role not deleted: {0}", err.Message));
                return 0;
            }
        }

        private object BuildPayload() => new
        {
            roleName = this.RoleName,
            createClient = this.CreateClient,
            modifyClient = this.ModifyClient,
            deleteClient = this.DeleteClient,
            viewOperationClient = this.ViewOperationClient,
            payeClient = this.PayeClient,
            viewClient = this.ViewClient,
            createFournisseur = this.CreateFournisseur,
            modifyFournisseur = this.ModifyFournisseur,
            deleteFournisseur = this.DeleteFournisseur,
            viewOperationFournisseur = this.ViewOperationFournisseur,
            payeFournisseur = this.PayeFournisseur,
            viewFournisseur = this.ViewFournisseur,
            reverseOperation = this.ReverseOperation,
            reverseMouvment = this.ReverseMouvment,
            viewOperation = this.ViewOperation,
            viewMouvment = this.ViewMouvment,
            viewProjectManagment = this.ViewProjectManagment,
            viewSettings = this.ViewSettings,
            viewUsers = this.ViewUsers,
            editUsers = this.EditUsers,
            deleteUsers = this.DeleteUsers,
            addUsers = this.AddUsers,
            viewRoles = this.ViewRoles,
            addRoles = this.AddRoles,
            deleteRoles = this.DeleteRoles,
            viewFamilly = this.ViewFamilly,
            editFamilly = this.EditFamilly,
            deleteFamilly = this.DeleteFamilly,
            addFamilly = this.AddFamilly,
            addArticle = this.AddArticle,
            deleteArticle = this.DeleteArticle,
            editArticle = this.EditArticle,
            viewArticle = this.ViewArticle,
            repport = this.Repport,
            ticket = this.Ticket,
            solderFournisseur = this.SolderFournisseur,
            solderClient = this.SolderClient,
            viewFactureSettings = this.ViewFactureSettings,
            modifyFactureSettings = this.ModifyFactureSettings,
            viewFacture = this.ViewFacture,
            viewPaymentMethod = this.ViewPaymentMethod,
            addPaymentMethod = this.AddPaymentMethod,
            modifyPaymentMethod = this.ModifyPaymentMethod,
            deletePaymentMethod = this.DeletePaymentMethod,
            viewApropos = this.ViewApropos,
            logout = this.Logout,
            viewExit = this.ViewExit,
            viewShutDown = this.ViewShutDown,
            viewClientsPage = this.ViewClientsPage,
            viewFournisseurPage = this.ViewFournisseurPage,
            viewInventrory = this.ViewInventrory,
            viewVente = this.ViewVente,
            cashClient = this.CashClient,
            cashFournisseur = this.CashFournisseur,
            viewCreditClient = this.ViewCreditClient,
            viewCreditFournisseur = this.ViewCreditFournisseur,
            viewLivraison = this.ViewLivraison,
            accessFacturation = this.AccessFacturation,
            createFacture = this.CreateFacture,
            historiqueFacture = this.HistoriqueFacture,
            historyCheck = this.HistoryCheck,
            factureEnregistrees = this.FactureEnregistrees,
            accessLivraison = this.AccessLivraison,
            creationLivraison = this.CreationLivraison,
            gestionLivreur = this.GestionLivreur
        };

        private class RoleIdResult
        {
            [JsonPropertyName("roleId")]
            public int RoleId { get; set; }
        }
    }
}