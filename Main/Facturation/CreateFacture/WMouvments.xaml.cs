using GestionComerce;
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

namespace GestionComerce.Main.Facturation.CreateFacture
{
    public partial class WMouvments : Window
    {
        public CSingleOperation wso;
        Operation op;
        private bool isExpeditionMode = false;

        public WMouvments(CSingleOperation wso, Operation op)
        {
            InitializeComponent();
            this.wso = wso;
            this.op = op;

            // Determine if we're in Expedition mode
            string invoiceType = null;
            if (wso.mainfa != null)
            {
                invoiceType = wso.mainfa.InvoiceType;
            }
            else if (wso.sc?.main != null)
            {
                invoiceType = wso.sc.main.InvoiceType;
            }

            isExpeditionMode = invoiceType == "Expedition";

            // Get the main reference - check both paths
            MainWindow main = null;
            if (wso.mainfa != null && wso.mainfa.main != null)
            {
                // We're in the main view (CSingleOperation was added to CMainFa)
                main = wso.mainfa.main;
            }
            else if (wso.sc?.main?.main != null)
            {
                // We're in the selection view (WSelectOperation is open)
                main = wso.sc.main.main;
            }

            if (main == null || op == null)
            {
                MessageBox.Show("Unable to load operation articles", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadMouvments(main);

            // Show/hide expedition column
            UpdateExpeditionColumnVisibility();

            // Show save button and header for all invoice types
            UpdateSaveButtonVisibility();
        }

        // **CRITICAL FIX: Auto-save when window closes**
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Force save all quantities when window closes
            ForceSaveAllQuantities();

            base.OnClosing(e);
        }

        private void ForceSaveAllQuantities()
        {
            if (wso?.mainfa == null || op == null) return;

            // Go through ALL mouvments and save their current state
            foreach (var child in MouvmentsContainer.Children)
            {
                if (child is CSingleMouvment mouvment)
                {
                    try
                    {
                        mouvment.UpdateInvoiceArticle();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving mouvment: {ex.Message}");
                    }
                }
            }
        }

        private void UpdateExpeditionColumnVisibility()
        {
            if (isExpeditionMode)
            {
                TxtExpeditionTotalHeader.Text = "Qté Expédié";
                TxtExpeditionTotalHeader.Visibility = Visibility.Visible;
            }
            else
            {
                TxtExpeditionTotalHeader.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSaveButtonVisibility()
        {
            // Always show save button and header
            btnSaveExpedition.Visibility = Visibility.Visible;
            HeaderGrid.Visibility = Visibility.Visible;

            // Update window title
            this.Title = "Définir les quantités et prix";
        }

        public async void LoadMouvments(MainWindow main)
        {
            MouvmentsContainer.Children.Clear();

            // Load ALL OperationArticles from database (including those linked to deleted articles)
            List<OperationArticle> allOperationArticles = await LoadAllOperationArticlesFromDatabaseAsync();

            if (allOperationArticles == null || allOperationArticles.Count == 0)
            {
                MessageBox.Show("No articles found in database", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Filter articles for this operation
            var operationArticles = allOperationArticles
                .Where(oa => oa.OperationID == op.OperationID)
                .ToList();

            if (operationArticles.Count == 0)
            {
                MessageBox.Show("No articles found for this operation", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (OperationArticle oa in operationArticles)
            {
                // CSingleMouvment will now load the article from database (including deleted ones)
                CSingleMouvment csm = new CSingleMouvment(this, oa);
                MouvmentsContainer.Children.Add(csm);
            }
        }

        // Load ALL OperationArticles from database
        private async Task<List<OperationArticle>> LoadAllOperationArticlesFromDatabaseAsync()
        {
            try
            {
                // Use static GetAllOperationArticlesAsync to get all operation articles
                var allArticles = await OperationArticle.GetAllOperationArticlesAsync();

                return allArticles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading operation articles: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<OperationArticle>();
            }
        }

        private void btnClose_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSaveExpedition_Click(object sender, RoutedEventArgs e)
        {
            // Update all mouvments
            foreach (var child in MouvmentsContainer.Children)
            {
                if (child is CSingleMouvment mouvment)
                {
                    mouvment.UpdateInvoiceArticle();
                }
            }

            MessageBox.Show("Quantités enregistrées", "Succès",
                MessageBoxButton.OK, MessageBoxImage.Information);

            this.Close();
        }
    }
}