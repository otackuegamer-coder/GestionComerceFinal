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
using System.Text.RegularExpressions;
using GestionComerce.Main.Inventory;
using GestionComerce.Vente;

namespace GestionComerce.Main.Vente
{
    /// <summary>
    /// Interaction logic for WSelectCient.xaml
    /// </summary>
    public partial class WSelectCient : Window
    {
        public WSelectCient(List<Client> lc, CMainV main, int Credit, int MethodID)
        {
            InitializeComponent();
            this.lc = lc;
            LoadClients();
            this.main = main;
            this.Credit = Credit;
            this.MethodID = MethodID;
            if (Credit == 0)
            {
                Creditt.Visibility = Visibility.Collapsed;
            }
            else if (Credit == 1)
            {
                NoClient.Visibility = Visibility.Collapsed;
            }
            else
            {
                Creditt.Visibility = Visibility.Collapsed;
                NoClient.Visibility = Visibility.Collapsed;
            }
            foreach (Role r in main.main.lr)
            {
                if (main.u.RoleID == r.RoleID)
                {
                    if (r.CreateClient == false)
                    {
                        AddClient.IsEnabled = false;
                    }
                    break;
                }
            }
        }
        List<Client> lc; public int selected = 0; CMainV main; int Credit; public int MethodID;

        public void LoadClients()
        {
            ClientContainer.Children.Clear();
            foreach (Client c in lc)
            {
                CSingleClient ar = new CSingleClient(c, this);
                ClientContainer.Children.Add(ar);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NoClientButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedClient.Text = "Aucun client sélectionné";
            selected = 0;
        }

        private void NewClientButton_Click(object sender, RoutedEventArgs e)
        {
            WAddCleint wAddCleint = new WAddCleint(lc, this);
            wAddCleint.ShowDialog();
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable button to prevent double-click
                ValidateButton.IsEnabled = false;

                // Variables to store operation data
                Client selectedClientObj = null;
                List<TicketArticleData> ticketArticles = new List<TicketArticleData>();
                decimal remiseValue = 0;
                decimal creditValue = 0;
                string operationTypeLabel = "";

                // Get selected client object if exists
                if (selected != 0)
                {
                    selectedClientObj = lc.FirstOrDefault(c => c.ClientID == selected);
                }

                // Collect article data for ticket BEFORE any operations
                foreach (CSingleArticle2 sa2 in main.SelectedArticles.Children)
                {
                    ticketArticles.Add(new TicketArticleData
                    {
                        ArticleName = sa2.a.ArticleName,
                        Quantity = sa2.qte,
                        UnitPrice = sa2.a.PrixVente,
                        Total = sa2.a.PrixVente * sa2.qte,
                        TVA = sa2.a.tva
                    });
                }

                // Validate inputs based on Credit type
                if (Credit == 0) // VENTE COMPTANT
                {
                    operationTypeLabel = "VENTE COMPTANT";
                    if (Remise.Text != "")
                    {
                        remiseValue = Convert.ToDecimal(Remise.Text);
                        if (remiseValue > main.TotalNett)
                        {
                            MessageBox.Show("La remise est plus grande que le total.");
                            ValidateButton.IsEnabled = true;
                            return;
                        }
                    }
                }
                else if (Credit == 1) // VENTE PARTIELLE
                {
                    operationTypeLabel = "VENTE PARTIELLE";
                    if (selected == 0)
                    {
                        MessageBox.Show("Veuillez sélectionner un client pour le crédit.");
                        ValidateButton.IsEnabled = true;
                        return;
                    }
                    if (Credittext.Text == "")
                    {
                        MessageBox.Show("Donnez une valeur de crédit.");
                        ValidateButton.IsEnabled = true;
                        return;
                    }

                    creditValue = Convert.ToDecimal(Credittext.Text);

                    if (Remise.Text != "")
                    {
                        remiseValue = Convert.ToDecimal(Remise.Text);
                        if (creditValue > main.TotalNett - remiseValue)
                        {
                            MessageBox.Show("La valeur de crédit est plus grande que le total moins la remise.");
                            ValidateButton.IsEnabled = true;
                            return;
                        }
                    }
                    else
                    {
                        if (creditValue > main.TotalNett)
                        {
                            MessageBox.Show("La valeur de crédit est plus grande que le total.");
                            ValidateButton.IsEnabled = true;
                            return;
                        }
                    }
                }
                else // VENTE À CRÉDIT (Credit == 2)
                {
                    operationTypeLabel = "VENTE À CRÉDIT";
                    if (selected == 0)
                    {
                        MessageBox.Show("Veuillez sélectionner un client pour le crédit.");
                        ValidateButton.IsEnabled = true;
                        return;
                    }
                    if (Remise.Text != "")
                    {
                        remiseValue = Convert.ToDecimal(Remise.Text);
                        if (remiseValue > main.TotalNett)
                        {
                            MessageBox.Show("La remise est plus grande que le total.");
                            ValidateButton.IsEnabled = true;
                            return;
                        }
                    }

                    if (Remise.Text != "")
                    {
                        creditValue = main.TotalNett - remiseValue;
                    }
                    else
                    {
                        creditValue = main.TotalNett;
                    }
                }

                // Show preview window BEFORE doing any database operations
                if (main.Ticket != null && main.Ticket.IsChecked == true)
                {
                    try
                    {
                        // Load facture settings from database
                        FactureSettings settings = await FactureSettings.LoadSettingsAsync();
                        if (settings == null)
                        {
                            settings = new FactureSettings(); // Use defaults
                        }

                        // Get payment method name
                        string paymentMethodName = "Espèces";
                        var paymentMethod = main.main.lp.FirstOrDefault(p => p.PaymentMethodID == MethodID);
                        if (paymentMethod != null)
                        {
                            paymentMethodName = paymentMethod.PaymentMethodName;
                        }

                        // Open ticket preview window with temporary operation ID
                        WFacturePreview factureWindow = new WFacturePreview(
                            settings,
                            0, // Temporary ID - will be replaced after operation is saved
                            DateTime.Now,
                            selectedClientObj,
                            ticketArticles,
                            main.TotalNett,
                            remiseValue,
                            creditValue,
                            paymentMethodName,
                            operationTypeLabel
                        );

                        // Show the dialog and wait for user decision
                        bool? result = factureWindow.ShowDialog();

                        // Check if user cancelled
                        if (result == false || !factureWindow.ShouldPrint)
                        {
                            // User cancelled - do NOT proceed with operation
                            MessageBox.Show("Opération annulée par l'utilisateur.",
                                "Opération annulée", MessageBoxButton.OK, MessageBoxImage.Information);

                            ValidateButton.IsEnabled = true;
                            return; // Exit without doing any database operations
                        }

                        // User confirmed - proceed with database operations
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erreur lors de l'affichage du ticket: " + ex.Message,
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ValidateButton.IsEnabled = true;
                        return;
                    }
                }

                // NOW perform all database operations after confirmation
                int operationId = 0;

                if (Credit == 0) // VENTE COMPTANT
                {
                    Operation Operation = new Operation();
                    Operation.OperationType = "VenteCa";
                    Operation.PrixOperation = main.TotalNett;
                    Operation.PaymentMethodID = MethodID;
                    if (remiseValue > 0)
                    {
                        Operation.Remise = remiseValue;
                    }
                    Operation.UserID = main.u.UserID;
                    Operation.ClientID = selected == 0 ? (int?)null : selected;

                    operationId = await Operation.InsertOperationAsync();

                    foreach (CSingleArticle2 sa2 in main.SelectedArticles.Children)
                    {
                        OperationArticle oca = new OperationArticle();
                        oca.ArticleID = sa2.a.ArticleID;
                        oca.OperationID = operationId;
                        oca.QteArticle = Convert.ToInt32(sa2.qte);

                        await oca.InsertOperationArticleAsync();
                        sa2.a.Quantite -= Convert.ToInt32(sa2.qte);
                        await sa2.a.UpdateArticleAsync();

                        foreach (Article ar in main.la)
                        {
                            if (ar.ArticleID == sa2.a.ArticleID)
                            {
                                main.la[main.la.IndexOf(ar)].Quantite = sa2.a.Quantite;
                                
                                main.LoadArticles(main.la);
                                break;
                            }
                        }
                    }
                }
                else if (Credit == 1) // VENTE PARTIELLE
                {
                    int creditId = 0;
                    bool creditExists = false;
                    Credit Credit = new Credit();
                    List<Credit> lcc = await Credit.GetCreditsAsync();

                    foreach (Credit cc in lcc)
                    {
                        if (cc.ClientID == selected)
                        {
                            cc.Total += creditValue;
                            await cc.UpdateCreditAsync();
                            creditExists = true;
                            creditId = cc.CreditID;
                            break;
                        }
                    }

                    if (!creditExists)
                    {
                        Credit newCredit = new Credit();
                        newCredit.ClientID = selected;
                        newCredit.Total = creditValue;
                        creditId = await newCredit.InsertCreditAsync();
                    }

                    Operation Operation = new Operation();
                    Operation.OperationType = "Vente50";
                    Operation.PaymentMethodID = MethodID;
                    Operation.PrixOperation = main.TotalNett;
                    Operation.CreditValue = creditValue;
                    Operation.CreditID = creditId;
                    if (remiseValue > 0)
                    {
                        Operation.Remise = remiseValue;
                    }
                    Operation.UserID = main.u.UserID;
                    Operation.ClientID = selected;

                    operationId = await Operation.InsertOperationAsync();

                    foreach (CSingleArticle2 sa2 in main.SelectedArticles.Children)
                    {
                        OperationArticle oca = new OperationArticle();
                        oca.ArticleID = sa2.a.ArticleID;
                        oca.OperationID = operationId;
                        oca.QteArticle = Convert.ToInt32(sa2.qte);
                        await oca.InsertOperationArticleAsync();
                        sa2.a.Quantite -= Convert.ToInt32(sa2.qte);
                        await sa2.a.UpdateArticleAsync();

                        foreach (Article ar in main.la)
                        {
                            if (ar.ArticleID == sa2.a.ArticleID)
                            {
                                main.la[main.la.IndexOf(ar)].Quantite = sa2.a.Quantite;
                                
                                main.LoadArticles(main.la);
                                break;
                            }
                        }
                    }
                }
                else // VENTE À CRÉDIT (Credit == 2)
                {
                    int creditId = 0;
                    bool creditExists = false;
                    Credit Credit = new Credit();
                    List<Credit> lcc = await Credit.GetCreditsAsync();
                    Operation Operation = new Operation();
                    Operation.PaymentMethodID = MethodID;

                    foreach (Credit cc in lcc)
                    {
                        if (cc.ClientID == selected)
                        {
                            cc.Total += creditValue;
                            Operation.CreditValue = creditValue;
                            await cc.UpdateCreditAsync();
                            creditExists = true;
                            creditId = cc.CreditID;
                            break;
                        }
                    }

                    if (!creditExists)
                    {
                        Credit newCredit = new Credit();
                        newCredit.ClientID = selected;
                        newCredit.Total = creditValue;
                        creditId = await newCredit.InsertCreditAsync();
                        Operation.CreditValue = creditValue;
                    }

                    Operation.OperationType = "VenteCr";
                    Operation.PrixOperation = main.TotalNett;
                    Operation.CreditID = creditId;
                    if (remiseValue > 0)
                    {
                        Operation.Remise = remiseValue;
                    }
                    Operation.UserID = main.u.UserID;
                    Operation.ClientID = selected;

                    operationId = await Operation.InsertOperationAsync();

                    foreach (CSingleArticle2 sa2 in main.SelectedArticles.Children)
                    {
                        OperationArticle oca = new OperationArticle();
                        oca.ArticleID = sa2.a.ArticleID;
                        oca.OperationID = operationId;
                        oca.QteArticle = Convert.ToInt32(sa2.qte);
                        await oca.InsertOperationArticleAsync();
                        sa2.a.Quantite -= Convert.ToInt32(sa2.qte);
                        await sa2.a.UpdateArticleAsync();

                        foreach (Article ar in main.la)
                        {
                            if (ar.ArticleID == sa2.a.ArticleID)
                            {
                                main.la[main.la.IndexOf(ar)].Quantite = sa2.a.Quantite;
                                
                                main.LoadArticles(main.la);
                                break;
                            }
                        }
                    }
                }

                // Print ticket if user confirmed earlier
                if (main.Ticket != null && main.Ticket.IsChecked == true)
                {
                    try
                    {
                        // Load facture settings again
                        FactureSettings settings = await FactureSettings.LoadSettingsAsync();
                        if (settings == null)
                        {
                            settings = new FactureSettings();
                        }

                        string paymentMethodName = "Espèces";
                        var paymentMethod = main.main.lp.FirstOrDefault(p => p.PaymentMethodID == MethodID);
                        if (paymentMethod != null)
                        {
                            paymentMethodName = paymentMethod.PaymentMethodName;
                        }

                        // Create preview window with actual operation ID
                        WFacturePreview factureWindow = new WFacturePreview(
                            settings,
                            operationId,
                            DateTime.Now,
                            selectedClientObj,
                            ticketArticles,
                            main.TotalNett,
                            remiseValue,
                            creditValue,
                            paymentMethodName,
                            operationTypeLabel
                        );

                        // Print directly without showing dialog again
                        factureWindow.PrintFacture();
                    }
                    catch (Exception printEx)
                    {
                        MessageBox.Show("Erreur lors de l'impression: " + printEx.Message,
                            "Erreur d'impression", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Clear cart after everything is done
                main.SelectedArticles.Children.Clear();
                main.TotalNet.Text = " 0.00 DH";
                main.ArticleCount.Text = " 0";
                main.TotalNett = 0;
                main.NbrA = 0;

                WCongratulations wCongratulations = new WCongratulations("Opération réussie", "Opération a été effectuée avec succès", 1);
                wCongratulations.ShowDialog();

                // **NEW: Check if Facture checkbox is checked and navigate to CMainIn with the operation**
                if (main.Facture != null && main.Facture.IsChecked == true)
                {
                    try
                    {
                        // Get the operation object that was just created
                        Operation op = new Operation();
                        List<Operation> operations = await op.GetOperationsAsync();
                        Operation createdOperation = operations.FirstOrDefault(o => o.OperationID == operationId);

                        if (createdOperation != null)
                        {
                            // Navigate to CMainIn with the operation
                            main.main.load_facture(main.u, createdOperation);
                        }
                    }
                    catch (Exception navEx)
                    {
                        MessageBox.Show("Erreur lors de la navigation vers la facture: " + navEx.Message,
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                this.Close();
            }
            catch (Exception ex)
            {
                ValidateButton.IsEnabled = true;
                WCongratulations wCongratulations = new WCongratulations("Opération échouée", "Opération n'a pas été effectuée: " + ex.Message, 0);
                wCongratulations.ShowDialog();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClientContainer.Children.Clear();
            string code = sender is TextBox tb ? tb.Text : "";
            foreach (Client c in lc)
            {
                if (c.Nom.Contains(code))
                {
                    CSingleClient ar = new CSingleClient(c, this);
                    ClientContainer.Children.Add(ar);
                }
            }
        }

        private void ClientsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits and optionally one decimal separator
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]*\\.?[0-9]*$");
        }
    }

    // TicketArticleData class for passing article info to ticket
    public class TicketArticleData
    {
        public string ArticleName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public decimal TVA { get; set; }  // Add TVA property
    }
}