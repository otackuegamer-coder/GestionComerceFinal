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
    /// <summary>
    /// Interaction logic for WSelectOperation.xaml
    /// </summary>
    public partial class WSelectOperation : Window
    {
        public CMainFa main;
        private bool isCreditMode;

        public WSelectOperation(CMainFa main)
        {
            InitializeComponent();
            this.main = main;

            // Determine if we're in Credit mode by checking the ComboBox directly
            string invoiceType = "Facture";
            if (main.cmbInvoiceType?.SelectedItem is ComboBoxItem selectedItem)
            {
                invoiceType = selectedItem.Content?.ToString() ?? "Facture";
            }
            isCreditMode = invoiceType == "Credit";

            LoadOperations();
        }

        public void LoadOperations()
        {
            OperationsContainer.Children.Clear();

            // Debug: Show what mode we're in
            System.Diagnostics.Debug.WriteLine($"LoadOperations - Credit Mode: {isCreditMode}");

            int operationCount = 0;
            foreach (Operation op in main.main.lo)
            {
                System.Diagnostics.Debug.WriteLine($"Operation ID: {op.OperationID}, Type: {op.OperationType}");

                // Filter based on invoice type
                if (isCreditMode)
                {
                    // Credit mode: only show operations starting with "P"
                    if (!op.OperationType.StartsWith("P"))
                    {
                        System.Diagnostics.Debug.WriteLine($"  Skipping (not a P operation)");
                        continue;
                    }
                }
                else
                {
                    // Normal mode: only show operations starting with "V"
                    if (!op.OperationType.StartsWith("V"))
                    {
                        System.Diagnostics.Debug.WriteLine($"  Skipping (not a V operation)");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"  Adding operation");
                CSingleOperation cSingleOperation = new CSingleOperation(main, this, op, isCreditMode);
                OperationsContainer.Children.Add(cSingleOperation);
                operationCount++;
            }

            System.Diagnostics.Debug.WriteLine($"Total operations loaded: {operationCount}");
        }

        private void btnClose_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string ClientName = "";
            TextBox textBox = sender as TextBox;

            foreach (Operation op in main.main.lo)
            {
                // Apply same filter as LoadOperations
                if (isCreditMode)
                {
                    if (!op.OperationType.StartsWith("P"))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!op.OperationType.StartsWith("V"))
                    {
                        continue;
                    }
                }

                foreach (Client c in main.main.lc)
                {
                    if (op.ClientID == c.ClientID)
                    {
                        ClientName = c.Nom;
                        break;
                    }
                }

                if (op.OperationID.ToString().IndexOf(textBox.Text, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ClientName.IndexOf(textBox.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Match found, ensure the operation is visible
                    foreach (CSingleOperation so in OperationsContainer.Children)
                    {
                        if (so.op.OperationID == op.OperationID)
                        {
                            so.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                }
                else
                {
                    // No match, hide the operation
                    foreach (CSingleOperation so in OperationsContainer.Children)
                    {
                        if (so.op.OperationID == op.OperationID)
                        {
                            so.Visibility = Visibility.Collapsed;
                            break;
                        }
                    }
                }
            }
        }
    }
}