using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestionComerce.Main.Settings
{
    /// <summary>
    /// Logique d'interaction pour WAddRole.xaml
    /// </summary>
    public partial class WAddRole : Window
    {
        public WAddRole(WRoles roles, List<Role> lr, Role r, int s)
        {
            InitializeComponent();
            this.lr = lr;
            this.roles = roles;
            this.s = s;
            this.rr = r;
            if (s == 1)
            {
                foreach (Role rr in lr)
                {
                    if (rr.RoleID == r.RoleID)
                    {
                        RoleName.Text = r.RoleName;
                        if (r.ViewClientsPage) checkit(ViewClientsPage);
                        if (r.ViewFournisseurPage) checkit(ViewFournisseurPage);
                        if (r.ViewInventrory) checkit(ViewInventory);
                        if (r.ViewVente) checkit(ViewVente);
                        if (r.ViewCreditClient) checkit(ViewCreditClient);
                        if (r.ViewCreditFournisseur) checkit(ViewCreditFournisseur);

                        // Client Permissions
                        if (r.CreateClient) checkit(CreateClient);
                        if (r.ModifyClient) checkit(ModifyClient);
                        if (r.DeleteClient) checkit(DeleteClient);
                        if (r.ViewClient) checkit(ViewClient);
                        if (r.ViewOperationClient) checkit(ViewOperationClient);
                        if (r.PayeClient) checkit(PayeClient);

                        // Fournisseur Permissions
                        if (r.CreateFournisseur) checkit(CreateFournisseur);
                        if (r.ModifyFournisseur) checkit(ModifyFournisseur);
                        if (r.DeleteFournisseur) checkit(DeleteFournisseur);
                        if (r.ViewFournisseur) checkit(ViewFournisseur);
                        if (r.ViewOperationFournisseur) checkit(ViewOperationFournisseur);
                        if (r.PayeFournisseur) checkit(PayeFournisseur);

                        // Operations / Movements
                        if (r.ReverseOperation) checkit(ReverseOperation);
                        if (r.ReverseMouvment) checkit(ReverseMouvment);
                        if (r.ViewOperation) checkit(ViewOperation);
                        if (r.ViewMouvment) checkit(ViewMouvment);

                        // Management & Settings
                        if (r.ViewProjectManagment) checkit(ViewProjectManagment);
                        if (r.ViewSettings) checkit(ViewSettings);

                        // Users & Roles
                        if (r.ViewUsers) checkit(ViewUsers);
                        if (r.AddUsers) checkit(AddUsers);
                        if (r.EditUsers) checkit(EditUsers);
                        if (r.DeleteUsers) checkit(DeleteUsers);
                        if (r.ViewRoles) checkit(ViewRoles);
                        if (r.AddRoles) checkit(AddRoles);
                        if (r.DeleteRoles) checkit(DeleteRoles);

                        // Familles
                        if (r.ViewFamilly) checkit(ViewFamilly);
                        if (r.AddFamilly) checkit(AddFamilly);
                        if (r.EditFamilly) checkit(EditFamilly);
                        if (r.DeleteFamilly) checkit(DeleteFamilly);

                        // Articles
                        if (r.AddArticle) checkit(AddArticle);
                        if (r.DeleteArticle) checkit(DeleteArticle);
                        if (r.EditArticle) checkit(EditArticle);
                        if (r.ViewArticle) checkit(ViewArticle);

                        // Reports & Tickets
                        if (r.Repport) checkit(Repport);
                        //if (r.Ticket) checkit(Ticket);
                        //if (r.ViewFacture) checkit(ViewFacture);

                        // Settlements
                        if (r.SolderFournisseur) checkit(SolderFournisseur);
                        if (r.SolderClient) checkit(SolderClient);
                        if (r.CashClient) checkit(CashClient);
                        if (r.CashFournisseur) checkit(CashFournisseur);

                        // Invoice Settings
                        if (r.ViewFactureSettings) checkit(ViewFactureSettings);
                        if (r.ModifyFactureSettings) checkit(ModifyFactureSettings);

                        // Payment Methods
                        if (r.ViewPaymentMethod) checkit(ViewPaymentMethod);
                        if (r.AddPaymentMethod) checkit(AddPaymentMethod);
                        if (r.ModifyPaymentMethod) checkit(ModifyPaymentMethod);
                        if (r.DeletePaymentMethod) checkit(DeletePaymentMethod);

                        // System Actions
                        if (r.ViewApropos) checkit(ViewApropos);
                        if (r.Logout) checkit(Logout);
                        if (r.ViewExit) checkit(Exit);
                        if (r.ViewShutDown) checkit(ShutDown);

                        // NEW: Facturation Permissions
                        if (r.AccessFacturation) checkit(AccessFacturation);
                        if (r.CreateFacture) checkit(CreateFacture);
                        if (r.HistoriqueFacture) checkit(HistoriqueFacture);
                        if (r.HistoryCheck) checkit(HistoryCheck);
                        if (r.FactureEnregistrees) checkit(FactureEnregistrees);

                        // NEW: Livraison Permissions
                        if (r.AccessLivraison) checkit(AccessLivraison);
                        if (r.CreationLivraison) checkit(CreationLivraison);
                        if (r.GestionLivreur) checkit(GestionLivreur);
                    }
                }
            }
        }

        List<Role> lr; int s; Role rr;
        WRoles roles;

        private void AnnulerButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Global Toggle Buttons
        private void ToutActiverBtn_Click(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(Container, true);
        }

        private void ToutDesactiverBtn_Click(object sender, RoutedEventArgs e)
        {
            SetAllCheckboxes(Container, false);
        }

        // Section-specific Toggle Buttons
        private void ActiverPages_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(PagesSection, true);
        }

        private void DesactiverPages_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(PagesSection, false);
        }

        private void ActiverClient_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ClientSection, true);
        }

        private void DesactiverClient_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ClientSection, false);
        }

        private void ActiverFournisseur_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(FournisseurSection, true);
        }

        private void DesactiverFournisseur_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(FournisseurSection, false);
        }

        private void ActiverOperations_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(OperationsSection, true);
        }

        private void DesactiverOperations_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(OperationsSection, false);
        }

        private void ActiverManagement_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ManagementSection, true);
        }

        private void DesactiverManagement_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ManagementSection, false);
        }

        private void ActiverUsers_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(UsersSection, true);
        }

        private void DesactiverUsers_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(UsersSection, false);
        }

        private void ActiverFamilly_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(FamillySection, true);
        }

        private void DesactiverFamilly_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(FamillySection, false);
        }

        private void ActiverArticles_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ArticlesSection, true);
        }

        private void DesactiverArticles_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ArticlesSection, false);
        }

        private void ActiverReports_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ReportsSection, true);
        }

        private void DesactiverReports_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(ReportsSection, false);
        }

        private void ActiverSettlements_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(SettlementsSection, true);
        }

        private void DesactiverSettlements_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(SettlementsSection, false);
        }

        private void ActiverInvoiceSettings_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(InvoiceSettingsSection, true);
        }

        private void DesactiverInvoiceSettings_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(InvoiceSettingsSection, false);
        }

        private void ActiverPaymentMethods_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(PaymentMethodsSection, true);
        }

        private void DesactiverPaymentMethods_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(PaymentMethodsSection, false);
        }

        private void ActiverSystemActions_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(SystemActionsSection, true);
        }

        private void DesactiverSystemActions_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(SystemActionsSection, false);
        }

        // NEW: Facturation Section Toggle Buttons
        private void ActiverFacturation_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(FacturationSection, true);
        }

        private void DesactiverFacturation_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(FacturationSection, false);
        }

        // NEW: Livraison Section Toggle Buttons
        private void ActiverLivraison_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(LivraisonSection, true);
        }

        private void DesactiverLivraison_Click(object sender, RoutedEventArgs e)
        {
            SetSectionCheckboxes(LivraisonSection, false);
        }

        // NEW: AccessFacturation dependency logic
        private void AccessFacturation_Checked(object sender, RoutedEventArgs e)
        {
            // When AccessFacturation is checked, check if all child permissions are checked
            UpdateAccessCheckbox(AccessFacturation, CreateFacture, HistoriqueFacture, HistoryCheck, FactureEnregistrees);
        }

        private void AccessFacturation_Unchecked(object sender, RoutedEventArgs e)
        {
            // When AccessFacturation is unchecked, uncheck all child permissions
            CreateFacture.IsChecked = false;
            HistoriqueFacture.IsChecked = false;
            HistoryCheck.IsChecked = false;
            FactureEnregistrees.IsChecked = false;
        }

        // NEW: AccessLivraison dependency logic
        private void AccessLivraison_Checked(object sender, RoutedEventArgs e)
        {
            // When AccessLivraison is checked, check if all child permissions are checked
            UpdateAccessCheckbox(AccessLivraison, CreationLivraison, GestionLivreur);
        }

        private void AccessLivraison_Unchecked(object sender, RoutedEventArgs e)
        {
            // When AccessLivraison is unchecked, uncheck all child permissions
            CreationLivraison.IsChecked = false;
            GestionLivreur.IsChecked = false;
        }

        // Helper method to update parent access checkbox based on children
        private void UpdateAccessCheckbox(CheckBox accessCheckbox, params CheckBox[] childCheckboxes)
        {
            bool allChecked = childCheckboxes.All(cb => cb.IsChecked == true);
            if (allChecked && accessCheckbox.IsChecked == false)
            {
                accessCheckbox.IsChecked = true;
            }
        }

        // Helper method to set all checkboxes in a container
        private void SetAllCheckboxes(DependencyObject parent, bool isChecked)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is CheckBox checkBox)
                {
                    checkBox.IsChecked = isChecked;
                }
                else
                {
                    SetAllCheckboxes(child, isChecked);
                }
            }
        }

        // Helper method to set checkboxes in a specific section
        private void SetSectionCheckboxes(DependencyObject section, bool isChecked)
        {
            SetAllCheckboxes(section, isChecked);
        }

        private async void AppliquerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(RoleName.Text))
                {
                    MessageBox.Show("Le nom du rôle ne peut pas être vide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (s == 0)
                {
                    Role newRole = new Role
                    {
                        RoleName = RoleName.Text,

                        // Pages Access
                        ViewClientsPage = check(ViewClientsPage),
                        ViewFournisseurPage = check(ViewFournisseurPage),
                        ViewInventrory = check(ViewInventory),
                        ViewVente = check(ViewVente),
                        ViewCreditClient = check(ViewCreditClient),
                        ViewCreditFournisseur = check(ViewCreditFournisseur),

                        // Client Permissions
                        CreateClient = check(CreateClient),
                        ModifyClient = check(ModifyClient),
                        DeleteClient = check(DeleteClient),
                        ViewClient = check(ViewClient),
                        ViewOperationClient = check(ViewOperationClient),
                        PayeClient = check(PayeClient),

                        // Fournisseur Permissions
                        CreateFournisseur = check(CreateFournisseur),
                        ModifyFournisseur = check(ModifyFournisseur),
                        DeleteFournisseur = check(DeleteFournisseur),
                        ViewFournisseur = check(ViewFournisseur),
                        ViewOperationFournisseur = check(ViewOperationFournisseur),
                        PayeFournisseur = check(PayeFournisseur),

                        // Operations / Movements
                        ReverseOperation = check(ReverseOperation),
                        ReverseMouvment = check(ReverseMouvment),
                        ViewOperation = check(ViewOperation),
                        ViewMouvment = check(ViewMouvment),

                        // Management & Settings
                        ViewProjectManagment = check(ViewProjectManagment),
                        ViewSettings = check(ViewSettings),

                        // Users & Roles
                        ViewUsers = check(ViewUsers),
                        AddUsers = check(AddUsers),
                        EditUsers = check(EditUsers),
                        DeleteUsers = check(DeleteUsers),
                        ViewRoles = check(ViewRoles),
                        AddRoles = check(AddRoles),
                        DeleteRoles = check(DeleteRoles),

                        // Familles
                        ViewFamilly = check(ViewFamilly),
                        AddFamilly = check(AddFamilly),
                        EditFamilly = check(EditFamilly),
                        DeleteFamilly = check(DeleteFamilly),

                        // Articles
                        AddArticle = check(AddArticle),
                        DeleteArticle = check(DeleteArticle),
                        EditArticle = check(EditArticle),
                        ViewArticle = check(ViewArticle),

                        // Reports & Tickets
                        Repport = check(Repport),
                        //Ticket = check(Ticket),
                        //ViewFacture = check(ViewFacture),

                        // Settlements
                        SolderFournisseur = check(SolderFournisseur),
                        SolderClient = check(SolderClient),
                        CashClient = check(CashClient),
                        CashFournisseur = check(CashFournisseur),

                        // Invoice Settings
                        ViewFactureSettings = check(ViewFactureSettings),
                        ModifyFactureSettings = check(ModifyFactureSettings),

                        // Payment Methods
                        ViewPaymentMethod = check(ViewPaymentMethod),
                        AddPaymentMethod = check(AddPaymentMethod),
                        ModifyPaymentMethod = check(ModifyPaymentMethod),
                        DeletePaymentMethod = check(DeletePaymentMethod),

                        // System Actions
                        ViewApropos = check(ViewApropos),
                        Logout = check(Logout),
                        ViewExit = check(Exit),
                        ViewShutDown = check(ShutDown),

                        // NEW: Facturation Permissions
                        AccessFacturation = check(AccessFacturation),
                        CreateFacture = check(CreateFacture),
                        HistoriqueFacture = check(HistoriqueFacture),
                        HistoryCheck = check(HistoryCheck),
                        FactureEnregistrees = check(FactureEnregistrees),

                        // NEW: Livraison Permissions
                        AccessLivraison = check(AccessLivraison),
                        CreationLivraison = check(CreationLivraison),
                        GestionLivreur = check(GestionLivreur)
                    };
                    foreach (Role r in lr)
                    {
                        if (r.RoleName.Equals(RoleName.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("Un rôle avec ce nom existe déjà.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    int id = await newRole.InsertRoleAsync();
                    newRole.RoleID = id;
                    lr.Add(newRole);
                }
                else
                {
                    rr.RoleName = RoleName.Text;

                    // Pages Access
                    rr.ViewClientsPage = check(ViewClientsPage);
                    rr.ViewFournisseurPage = check(ViewFournisseurPage);
                    rr.ViewInventrory = check(ViewInventory);
                    rr.ViewVente = check(ViewVente);
                    rr.ViewCreditClient = check(ViewCreditClient);
                    rr.ViewCreditFournisseur = check(ViewCreditFournisseur);

                    // Client Permissions
                    rr.CreateClient = check(CreateClient);
                    rr.ModifyClient = check(ModifyClient);
                    rr.DeleteClient = check(DeleteClient);
                    rr.ViewClient = check(ViewClient);
                    rr.ViewOperationClient = check(ViewOperationClient);
                    rr.PayeClient = check(PayeClient);

                    // Fournisseur Permissions
                    rr.CreateFournisseur = check(CreateFournisseur);
                    rr.ModifyFournisseur = check(ModifyFournisseur);
                    rr.DeleteFournisseur = check(DeleteFournisseur);
                    rr.ViewFournisseur = check(ViewFournisseur);
                    rr.ViewOperationFournisseur = check(ViewOperationFournisseur);
                    rr.PayeFournisseur = check(PayeFournisseur);

                    // Operations / Movements
                    rr.ReverseOperation = check(ReverseOperation);
                    rr.ReverseMouvment = check(ReverseMouvment);
                    rr.ViewOperation = check(ViewOperation);
                    rr.ViewMouvment = check(ViewMouvment);

                    // Management & Settings
                    rr.ViewProjectManagment = check(ViewProjectManagment);
                    rr.ViewSettings = check(ViewSettings);

                    // Users & Roles
                    rr.ViewUsers = check(ViewUsers);
                    rr.AddUsers = check(AddUsers);
                    rr.EditUsers = check(EditUsers);
                    rr.DeleteUsers = check(DeleteUsers);
                    rr.ViewRoles = check(ViewRoles);
                    rr.AddRoles = check(AddRoles);
                    rr.DeleteRoles = check(DeleteRoles);

                    // Familles
                    rr.ViewFamilly = check(ViewFamilly);
                    rr.AddFamilly = check(AddFamilly);
                    rr.EditFamilly = check(EditFamilly);
                    rr.DeleteFamilly = check(DeleteFamilly);

                    // Articles
                    rr.AddArticle = check(AddArticle);
                    rr.DeleteArticle = check(DeleteArticle);
                    rr.EditArticle = check(EditArticle);
                    rr.ViewArticle = check(ViewArticle);

                    // Reports & Tickets
                    rr.Repport = check(Repport);
                    //rr.Ticket = check(Ticket);
                    //rr.ViewFacture = check(ViewFacture);

                    // Settlements
                    rr.SolderFournisseur = check(SolderFournisseur);
                    rr.SolderClient = check(SolderClient);
                    rr.CashClient = check(CashClient);
                    rr.CashFournisseur = check(CashFournisseur);

                    // Invoice Settings
                    rr.ViewFactureSettings = check(ViewFactureSettings);
                    rr.ModifyFactureSettings = check(ModifyFactureSettings);

                    // Payment Methods
                    rr.ViewPaymentMethod = check(ViewPaymentMethod);
                    rr.AddPaymentMethod = check(AddPaymentMethod);
                    rr.ModifyPaymentMethod = check(ModifyPaymentMethod);
                    rr.DeletePaymentMethod = check(DeletePaymentMethod);

                    // System Actions
                    rr.ViewApropos = check(ViewApropos);
                    rr.Logout = check(Logout);
                    rr.ViewExit = check(Exit);
                    rr.ViewShutDown = check(ShutDown);

                    // NEW: Facturation Permissions
                    rr.AccessFacturation = check(AccessFacturation);
                    rr.CreateFacture = check(CreateFacture);
                    rr.HistoriqueFacture = check(HistoriqueFacture);
                    rr.HistoryCheck = check(HistoryCheck);
                    rr.FactureEnregistrees = check(FactureEnregistrees);

                    // NEW: Livraison Permissions
                    rr.AccessLivraison = check(AccessLivraison);
                    rr.CreationLivraison = check(CreationLivraison);
                    rr.GestionLivreur = check(GestionLivreur);

                    foreach (Role r in lr)
                    {
                        if (r.RoleID == rr.RoleID) { continue; }
                        if (r.RoleName.Equals(RoleName.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("Un rôle avec ce nom existe déjà.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        if (RolesAreEqual(r, rr))
                        {
                            MessageBox.Show($"Un rôle avec ces permissions existe déjà : {r.RoleName}",
                                            "Erreur",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Error);
                            return;
                        }
                    }

                    await rr.UpdateRoleAsync();
                    roles.CUM.Load_users();
                }

                foreach (Role r in roles.CUM.sp.main.lr)
                {
                    if (roles.CUM.u.RoleID == r.RoleID)
                    {
                        if (r.ViewRoles == true)
                        {
                            roles.LoadRoles();
                        }
                        break;
                    }
                }
                if (s == 0)
                {

                    WCongratulations wCongratulations = new WCongratulations("Ajout avec succès", "l'ajout a ete effectue avec succes", 1);
                    wCongratulations.ShowDialog();
                }
                else
                {

                    WCongratulations wCongratulations = new WCongratulations("Modification avec succès", "la modification a ete effectue avec succes", 1);
                    wCongratulations.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                if (s == 0)
                {
                    WCongratulations wCongratulations = new WCongratulations("Ajout échoué", "l'ajout n'a pas ete effectue: " + ex.Message, 0);
                    wCongratulations.ShowDialog();

                }
                else
                {
                    WCongratulations wCongratulations = new WCongratulations("Modification échoué", "la modification n'a pas ete effectue: " + ex.Message, 0);
                    wCongratulations.ShowDialog();
                }
            }
        }
        bool RolesAreEqual(Role a, Role b)
        {
            return
                a.ViewClientsPage == b.ViewClientsPage &&
                a.ViewFournisseurPage == b.ViewFournisseurPage &&
                a.ViewInventrory == b.ViewInventrory &&
                a.ViewVente == b.ViewVente &&
                a.ViewCreditClient == b.ViewCreditClient &&
                a.ViewCreditFournisseur == b.ViewCreditFournisseur &&

                // Client Permissions
                a.CreateClient == b.CreateClient &&
                a.ModifyClient == b.ModifyClient &&
                a.DeleteClient == b.DeleteClient &&
                a.ViewClient == b.ViewClient &&
                a.ViewOperationClient == b.ViewOperationClient &&
                a.PayeClient == b.PayeClient &&

                // Fournisseur Permissions
                a.CreateFournisseur == b.CreateFournisseur &&
                a.ModifyFournisseur == b.ModifyFournisseur &&
                a.DeleteFournisseur == b.DeleteFournisseur &&
                a.ViewFournisseur == b.ViewFournisseur &&
                a.ViewOperationFournisseur == b.ViewOperationFournisseur &&
                a.PayeFournisseur == b.PayeFournisseur &&

                // Operations / Movements
                a.ReverseOperation == b.ReverseOperation &&
                a.ReverseMouvment == b.ReverseMouvment &&
                a.ViewOperation == b.ViewOperation &&
                a.ViewMouvment == b.ViewMouvment &&

                // Management & Settings
                a.ViewProjectManagment == b.ViewProjectManagment &&
                a.ViewSettings == b.ViewSettings &&

                // Users & Roles
                a.ViewUsers == b.ViewUsers &&
                a.AddUsers == b.AddUsers &&
                a.EditUsers == b.EditUsers &&
                a.DeleteUsers == b.DeleteUsers &&
                a.ViewRoles == b.ViewRoles &&
                a.AddRoles == b.AddRoles &&
                a.DeleteRoles == b.DeleteRoles &&

                // Familles
                a.ViewFamilly == b.ViewFamilly &&
                a.AddFamilly == b.AddFamilly &&
                a.EditFamilly == b.EditFamilly &&
                a.DeleteFamilly == b.DeleteFamilly &&

                // Articles
                a.AddArticle == b.AddArticle &&
                a.DeleteArticle == b.DeleteArticle &&
                a.EditArticle == b.EditArticle &&
                a.ViewArticle == b.ViewArticle &&

                // Reports & Tickets
                a.Repport == b.Repport &&
                a.Ticket == b.Ticket &&
                a.ViewFacture == b.ViewFacture &&

                // Settlements
                a.SolderFournisseur == b.SolderFournisseur &&
                a.SolderClient == b.SolderClient &&
                a.CashClient == b.CashClient &&
                a.CashFournisseur == b.CashFournisseur &&

                // Invoice Settings
                a.ViewFactureSettings == b.ViewFactureSettings &&
                a.ModifyFactureSettings == b.ModifyFactureSettings &&

                // Payment Methods
                a.ViewPaymentMethod == b.ViewPaymentMethod &&
                a.AddPaymentMethod == b.AddPaymentMethod &&
                a.ModifyPaymentMethod == b.ModifyPaymentMethod &&
                a.DeletePaymentMethod == b.DeletePaymentMethod &&

                // System Actions
                a.ViewApropos == b.ViewApropos &&
                a.Logout == b.Logout &&
                a.ViewExit == b.ViewExit &&
                a.ViewShutDown == b.ViewShutDown &&

                // NEW: Facturation Permissions
                a.AccessFacturation == b.AccessFacturation &&
                a.CreateFacture == b.CreateFacture &&
                a.HistoriqueFacture == b.HistoriqueFacture &&
                a.HistoryCheck == b.HistoryCheck &&
                a.FactureEnregistrees == b.FactureEnregistrees &&

                // NEW: Livraison Permissions
                a.AccessLivraison == b.AccessLivraison &&
                a.CreationLivraison == b.CreationLivraison &&
                a.GestionLivreur == b.GestionLivreur;
        }


        private bool check(CheckBox cb)
        {
            return cb != null && cb.IsChecked == true;
        }
        private void checkit(CheckBox cb)
        {
            cb.IsChecked = true;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
