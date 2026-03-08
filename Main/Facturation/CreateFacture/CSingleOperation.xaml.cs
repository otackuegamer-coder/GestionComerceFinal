using GestionComerce;
using GestionComerce.Main.Facturation;
using GestionComerce.Main.ProjectManagment;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    /// <summary>
    /// Interaction logic for CSingleOperation.xaml
    /// </summary>
    public partial class CSingleOperation : UserControl
    {
        private const int ETAT_FACTURE_NORMAL = 0;
        private const int ETAT_FACTURE_REVERSED = 1;

        public WSelectOperation sc;
        public CMainFa mainfa;
        public Operation op;
        private bool isCreditMode;

        public CSingleOperation(CMainFa mainfa, WSelectOperation sc, Operation op, bool isCreditMode = false)
        {
            InitializeComponent();
            this.mainfa = mainfa;
            this.sc = sc;
            this.op = op;
            this.isCreditMode = isCreditMode;

            // Initialize UI elements
            OperationPrice.Text = op.PrixOperation.ToString("0.00") + " DH";
            OperationDate.Text = op.DateOperation.ToString();

            // Set operation type label and side color based on mode
            if (isCreditMode)
            {
                OperationType.Text = "Paiement #" + op.OperationID.ToString();
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#d3f705"));
            }
            else
            {
                OperationType.Text = "Vente #" + op.OperationID.ToString();
            }

            // **MODIFIED: Display reversed status only for UI information, don't affect invoice status**
            if (op.Reversed == true)
            {
                OperationType.Text += " (Reversed)";
                SideColor.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#828181"));
            }

            // Load client name
            LoadClientName();

            // Hide articles button in Credit mode
            if (isCreditMode)
            {
                btnArticle.Visibility = Visibility.Collapsed;
            }

            // Add click handler for removal
            MyBorder.MouseLeftButtonDown += Border_MouseLeftButtonDown;
        }

        private void LoadClientName()
        {
            // Get the client list from the appropriate source
            var clientList = sc == null ? mainfa?.main?.lc : sc?.main?.main?.lc;

            if (clientList == null)
                return;

            // Find and display client name
            var client = clientList.FirstOrDefault(c => c.ClientID == op.ClientID);
            if (client != null)
            {
                OperationName.Text = "Client : " + client.Nom;
            }
        }

        private void btnArticle_Click(object sender, RoutedEventArgs e)
        {
            // Stop event propagation to prevent triggering border click
            e.Handled = true;

            // Don't open articles in Credit mode
            if (isCreditMode)
            {
                return;
            }

            WMouvments wMouvments = new WMouvments(this, op);
            wMouvments.ShowDialog();

            // Recalculate totals after window closes
            mainfa?.RecalculateTotals();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check if click was on the articles button - if so, don't handle removal
            var clickedElement = e.OriginalSource as FrameworkElement;

            // Traverse up the visual tree to check if we clicked on btnArticle or its children
            while (clickedElement != null)
            {
                if (clickedElement == btnArticle)
                {
                    // Click was on the articles button, don't remove operation
                    e.Handled = true;
                    return;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement) as FrameworkElement;
            }

            // Mark event as handled to prevent double processing
            e.Handled = true;

            // If we're in selection mode (WSelectOperation is open)
            if (sc != null)
            {
                HandleOperationSelection();
            }
            else if (mainfa != null)
            {
                // If we're in main view, remove this operation
                HandleOperationRemoval();
            }
        }

        // **MODIFIED: Removed all reversed-based logic for invoice status**
        private void HandleOperationSelection()
        {
            if (sc?.main?.main == null)
                return;

            // Check if operation already exists in main
            if (sc.main.SelectedOperations.Any(o => o.OperationID == op.OperationID))
            {
                MessageBox.Show(
                    "Cette opération a déjà été ajoutée !",
                    "Opération dupliquée",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Handle Credit operations specially - ADD to existing total
            if (isCreditMode)
            {
                // Get current total from txtApresTVAAmount
                decimal currentTotal = 0;
                if (sc.main.txtApresTVAAmount != null && !string.IsNullOrEmpty(sc.main.txtApresTVAAmount.Text))
                {
                    string cleanAmount = sc.main.txtApresTVAAmount.Text.Replace("DH", "").Replace(" ", "").Trim();
                    decimal.TryParse(cleanAmount, out currentTotal);
                }

                // ADD operation price to existing total
                decimal newTotal = currentTotal + op.PrixOperation;

                System.Diagnostics.Debug.WriteLine($"=== Adding Credit Operation ===");
                System.Diagnostics.Debug.WriteLine($"  Current Total: {currentTotal}");
                System.Diagnostics.Debug.WriteLine($"  Operation Price: {op.PrixOperation}");
                System.Diagnostics.Debug.WriteLine($"  New Total: {newTotal}");

                // Update the total amount (TTC)
                if (sc.main.txtApresTVAAmount != null)
                {
                    sc.main.txtApresTVAAmount.Text = newTotal.ToString("0.00") + " DH";
                }

                // Update total HT as well
                if (sc.main.txtTotalAmount != null)
                {
                    sc.main.txtTotalAmount.Text = newTotal.ToString("0.00") + " DH";
                }

                // Update CreditMontant (same as total)
                if (sc.main.txtApresRemiseAmount != null)
                {
                    sc.main.txtApresRemiseAmount.Text = newTotal.ToString("0.00") + " DH";
                }

                // Get client name for credit display
                string clientName = "";
                var client = sc.main.main.lc?.FirstOrDefault(c => c.ClientID == op.ClientID);
                if (client != null)
                {
                    clientName = client.Nom;
                }

                // Store client info
                if (sc.main.txtClientName != null)
                {
                    sc.main.txtClientName.Text = clientName;
                }
            }
            else
            {
                // **MODIFIED: Normal operation handling - invoice status is always user's choice**
                // Add operation to main
                sc.main.AddOperation(op);

                // Invoice status is always enabled for user to choose
                if (sc.main.EtatFacture != null)
                {
                    sc.main.EtatFacture.SelectedIndex = ETAT_FACTURE_NORMAL;
                    sc.main.EtatFacture.IsEnabled = true; // Always let user choose status
                }

                // Display discount
                if (sc.main.Remise != null)
                {
                    sc.main.Remise.Text = op.Remise.ToString("0.00");
                }
            }

            // For credit mode, we still need to add to SelectedOperations list
            if (isCreditMode)
            {
                sc.main.SelectedOperations.Add(op);
            }

            // Add this control to the main operation container
            CSingleOperation newControl = new CSingleOperation(sc.main, null, op, isCreditMode);
            sc.main.OperationContainer.Children.Add(newControl);

            // Clear the selection window's operations container
            sc.OperationsContainer.Children.Clear();

            sc.Close();
        }

        private void HandleOperationRemoval()
        {
            if (mainfa == null)
                return;

            // Ask for confirmation
            var result = MessageBox.Show(
                $"Voulez-vous supprimer l'opération #{op.OperationID} ?",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Handle Credit operations specially - SUBTRACT from existing total
                if (isCreditMode)
                {
                    // Get current total
                    decimal currentTotal = 0;
                    if (mainfa.txtApresTVAAmount != null && !string.IsNullOrEmpty(mainfa.txtApresTVAAmount.Text))
                    {
                        string cleanAmount = mainfa.txtApresTVAAmount.Text.Replace("DH", "").Replace(" ", "").Trim();
                        decimal.TryParse(cleanAmount, out currentTotal);
                    }

                    // SUBTRACT operation price from existing total
                    decimal newTotal = Math.Max(0, currentTotal - op.PrixOperation); // Ensure it doesn't go negative

                    System.Diagnostics.Debug.WriteLine($"=== Removing Credit Operation ===");
                    System.Diagnostics.Debug.WriteLine($"  Current Total: {currentTotal}");
                    System.Diagnostics.Debug.WriteLine($"  Operation Price: {op.PrixOperation}");
                    System.Diagnostics.Debug.WriteLine($"  New Total: {newTotal}");

                    // Update the total amount (TTC)
                    if (mainfa.txtApresTVAAmount != null)
                    {
                        mainfa.txtApresTVAAmount.Text = newTotal.ToString("0.00") + " DH";
                    }

                    // Update total HT as well
                    if (mainfa.txtTotalAmount != null)
                    {
                        mainfa.txtTotalAmount.Text = newTotal.ToString("0.00") + " DH";
                    }

                    // Update CreditMontant (same as total)
                    if (mainfa.txtApresRemiseAmount != null)
                    {
                        mainfa.txtApresRemiseAmount.Text = newTotal.ToString("0.00") + " DH";
                    }
                }

                // Remove operation from main
                mainfa.RemoveOperation(op);

                // Remove this control from the container
                var parent = this.Parent as Panel;
                if (parent != null)
                {
                    parent.Children.Remove(this);
                }

                // If no operations left, clear all amounts
                if (mainfa.SelectedOperations.Count == 0)
                {
                    ResetAmounts();
                }
            }
        }

        // Public method to add articles to the shared InvoiceArticles list with merging
        public void AddArticlesToInvoice(List<InvoiceArticle> articles)
        {
            if (mainfa?.InvoiceArticles == null)
            {
                mainfa.InvoiceArticles = new List<InvoiceArticle>();
            }

            foreach (var article in articles)
            {
                // Skip articles with quantity 0
                if (article.Quantite <= 0)
                    continue;

                // Ensure the article has the correct OperationID
                article.OperationID = op.OperationID;

                // Check if article already exists in InvoiceArticles with same name, price, and TVA
                var existingArticle = mainfa.InvoiceArticles.FirstOrDefault(a =>
                    a.ArticleID == article.ArticleID &&
                    a.ArticleName == article.ArticleName &&
                    a.Prix == article.Prix &&
                    a.TVA == article.TVA);

                if (existingArticle != null)
                {
                    // Merge quantities across operations
                    existingArticle.Quantite += article.Quantite;

                    // Show notification
                    MessageBox.Show(
                        $"Quantité mise à jour pour {article.ArticleName} : {existingArticle.Quantite}",
                        "Quantité fusionnée",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Add new article
                    mainfa.InvoiceArticles.Add(article);
                }
            }

            // Update totals
            UpdateInvoiceTotals();
        }

        private void UpdateInvoiceTotals()
        {
            if (mainfa?.InvoiceArticles == null || mainfa.InvoiceArticles.Count == 0)
            {
                ResetAmounts();
                return;
            }

            decimal totalHT = 0;
            decimal totalTVA = 0;
            decimal totalTTC = 0;

            foreach (var article in mainfa.InvoiceArticles)
            {
                totalHT += article.TotalHT;
                totalTVA += article.MontantTVA;
                totalTTC += article.TotalTTC;
            }

            // Apply discount if exists
            decimal remise = 0;
            if (decimal.TryParse(mainfa.Remise?.Text ?? "0", out decimal remiseValue))
            {
                remise = remiseValue;
            }

            decimal totalApresRemise = totalTTC - remise;

            // Update UI - Using TextBox instead of TextBlock
            UpdateAmountTextBox(mainfa.txtTotalAmount, totalHT);
            UpdateAmountTextBox(mainfa.txtTVAAmount, totalTVA);
            UpdateAmountTextBox(mainfa.txtApresTVAAmount, totalTTC);
            UpdateAmountTextBox(mainfa.txtApresRemiseAmount, totalApresRemise);

            // Calculate average TVA rate
            if (totalHT > 0 && mainfa.txtTVARate != null)
            {
                decimal avgTVA = (totalTVA / totalHT) * 100;
                mainfa.txtTVARate.Text = avgTVA.ToString("0.00");
            }
        }

        private void UpdateAmountTextBox(TextBox textBox, decimal amount)
        {
            if (textBox != null)
            {
                textBox.Text = amount.ToString("0.00") + " DH";
            }
        }

        private void ResetAmounts()
        {
            if (mainfa == null) return;

            UpdateAmountTextBox(mainfa.txtTotalAmount, 0);
            UpdateAmountTextBox(mainfa.txtTVAAmount, 0);
            UpdateAmountTextBox(mainfa.txtApresTVAAmount, 0);
            UpdateAmountTextBox(mainfa.txtApresRemiseAmount, 0);

            if (mainfa.txtTVARate != null)
                mainfa.txtTVARate.Text = "0.00";
        }

        public void MyBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This method is kept for compatibility but should not be used
            // All logic is now in Border_MouseLeftButtonDown
        }
    }
}
