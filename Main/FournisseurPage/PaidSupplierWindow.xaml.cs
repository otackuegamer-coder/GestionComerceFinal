using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce.Main.FournisseurPage
{
    public partial class PaidSupplierWindow : Window
    {
        private User _currentUser;
        private readonly MainWindow _mainWindow;
        private readonly Fournisseur _supplier;
        private Credit[] _supplierCredits;

        public PaidSupplierWindow(User u, MainWindow mainWindow, Fournisseur supplier)
        {
            InitializeComponent();
            _currentUser = u;
            _mainWindow = mainWindow;
            _supplier = supplier;
            Loaded += PaidSupplierWindow_Loaded;
            foreach (Role r in _mainWindow.lr)
            {
                if (_currentUser.RoleID == r.RoleID)
                {
                    if (!r.ViewCreditFournisseur)
                    {
                        Credit.Visibility = Visibility.Collapsed;
                        PayMaxButton.IsEnabled = false;
                    }
                    if (!r.PayeFournisseur)
                    {
                        ProcessPaymentButton.IsEnabled = false;
                    }
                }
            }
        }

        private void PaidSupplierWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SupplierNameLabel.Text = _supplier.Nom;
            LoadCredits();
        }

        private int GetCurrentUserId()
        {
            return _currentUser.UserID;
        }

        private void LoadCredits()
        {
            // Load from MainWindow credit list instead of database
            _supplierCredits = _mainWindow.credits
                .Where(c => c.FournisseurID == _supplier.FournisseurID && c.Etat)
                .ToArray();

            decimal total = _supplierCredits.Sum(c => c.Total);
            decimal paid = _supplierCredits.Sum(c => c.Paye);
            decimal diff = _supplierCredits.Sum(c => c.Difference);

            TotalCreditLabel.Text = $"{total:N2} DH";
            TotalPaidLabel.Text = $"{paid:N2} DH";
            DifferenceLabel.Text = $"{diff:N2} DH";
        }

        private void PayMaxButton_Click(object sender, RoutedEventArgs e)
        {
            decimal diff = _supplierCredits.Sum(c => c.Difference);
            PaymentAmountTextBox.Text = diff.ToString("F2");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private async Task CreatePaymentOperationAsync(decimal paidAmount, int credidID)
        {
            try
            {
                // Create operation with current date
                DateTime currentDate = DateTime.Now;

                var op = new Operation
                {
                    ClientID = null,
                    FournisseurID = _supplier.FournisseurID,
                    CreditID = credidID,
                    PrixOperation = paidAmount,
                    Remise = 0m,
                    CreditValue = -paidAmount,
                    UserID = GetCurrentUserId(),
                    DateOperation = currentDate,
                    Date = currentDate, // Set Date property as well
                    OperationType = "SUPPLIER_PAYMENT",
                    //Reversed = false,
                    Etat = true
                };

                int opId = await op.InsertOperationAsync();

                if (opId > 0)
                {
                    op.OperationID = opId;
                    // Ensure date is set before adding to list
                    op.DateOperation = currentDate;
                    op.Date = currentDate;
                    _mainWindow.lo.Add(op);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Payment saved but failed to record operation: {ex.Message}",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ProcessPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(PaymentAmountTextBox.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Veuillez entrer un montant de paiement valide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Calculate total outstanding credit
                decimal totalOutstanding = _supplierCredits.Sum(c => c.Difference);

                // Check if payment amount exceeds outstanding credit
                if (amount > totalOutstanding)
                {
                    MessageBox.Show($"Le montant du paiement ({amount:N2} DH) dépasse le crédit restant ({totalOutstanding:N2} DH).\n\nVeuillez entrer un montant inférieur ou égal à {totalOutstanding:N2} DH.",
                        "Montant Invalide", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                decimal remaining = amount;
                int creditId = 0;

                // Apply to oldest credits first (by CreditID)
                foreach (var credit in _supplierCredits.OrderBy(c => c.CreditID))
                {
                    if (remaining <= 0) break;
                    if (credit.Difference <= 0) continue;

                    decimal apply = Math.Min(credit.Difference, remaining);
                    credit.Paye += apply;
                    credit.Difference = credit.Total - credit.Paye;
                    remaining -= apply;
                    creditId = credit.CreditID;

                    // Persist to database
                    await credit.UpdateCreditAsync();

                    // Update the credit in MainWindow list - Get actual reference
                    var creditInList = _mainWindow.credits.FirstOrDefault(c => c.CreditID == credit.CreditID);
                    if (creditInList != null)
                    {
                        creditInList.Paye = credit.Paye;
                        creditInList.Difference = credit.Difference;
                    }
                }

                await CreatePaymentOperationAsync(amount, creditId);

                // Set DialogResult BEFORE showing success message
                DialogResult = true;

                WCongratulations wCongratulations = new WCongratulations("Paiement avec Succès", $"Paiement de {amount:N2} DH traité avec succès.", 1);
                wCongratulations.ShowDialog();

                // Close this window after showing success
                Close();
            }
            catch (Exception ex)
            {
                WCongratulations wCongratulations = new WCongratulations("Paiement Échoué", "Le paiement n'a pas été effectué", 0);
                wCongratulations.ShowDialog();
            }
        }
    }
}