using GestionComerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestionComerce.Main.ClientPage
{
    public partial class PaidClientWindow : Window
    {
        private readonly MainWindow _mainWindow;
        private User _currentUser;
        private readonly Client _client;
        private Credit[] _clientCredits;
        private Dictionary<int, decimal> _originalPaidAmounts;

        public PaidClientWindow(User u, MainWindow mainWindow, Client client)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _client = client;
            _currentUser = u;
            _originalPaidAmounts = new Dictionary<int, decimal>();
            SetupEventHandlers();
            Loaded += PaidClientWindow_Loaded;
            foreach (Role r in _mainWindow.lr)
            {
                if (_currentUser.RoleID == r.RoleID)
                {
                    if (!r.ViewCreditClient)
                    {
                        Credit.Visibility = Visibility.Collapsed;
                        // Removed: Remaining.Visibility = Visibility.Collapsed;    
                        PayMaxButton.IsEnabled = false;
                    }
                    if (!r.PayeClient)
                    {
                        ProcessPaymentButton.IsEnabled = false;
                    }
                }
            }
        }

        private void SetupEventHandlers()
        {
            PaymentAmountTextBox.TextChanged += PaymentAmountTextBox_TextChanged;
            PaymentAmountTextBox.PreviewTextInput += PaymentAmountTextBox_PreviewTextInput;
        }

        private void PaidClientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ClientNameLabel.Text = _client.Nom;
            LoadCredits();
        }

        private void LoadCredits()
        {
            try
            {
                // Load from MainWindow credit list instead of database
                _clientCredits = _mainWindow.credits
                    .Where(c => c.ClientID == _client.ClientID && c.Etat)
                    .ToArray();

                decimal total = _clientCredits.Sum(c => c.Total);
                decimal paid = _clientCredits.Sum(c => c.Paye);
                decimal diff = _clientCredits.Sum(c => c.Difference);

                TotalCreditLabel.Text = $"{total:N2} DH";
                TotalPaidLabel.Text = $"{paid:N2} DH";
                DifferenceLabel.Text = $"{diff:N2} DH";
                // Removed: RemainingBalanceLabel.Text = $"{diff:N2} DH";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading credits: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PaymentAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Removed entire method content since RemainingBalanceLabel no longer exists
            // This method can be removed or kept empty if needed for future use
        }

        private void PaymentAmountTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            if (!System.Text.RegularExpressions.Regex.IsMatch(fullText, @"^\d*\.?\d*$"))
                e.Handled = true;
        }

        private void PayMaxButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientCredits == null || _clientCredits.Length == 0)
            {
                MessageBox.Show("No outstanding credits found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            decimal diff = _clientCredits.Sum(c => c.Difference);
            PaymentAmountTextBox.Text = diff.ToString("F2");
            // Removed: RemainingBalanceLabel updates
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        private async void ProcessPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(PaymentAmountTextBox.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Veuillez entrer un montant de paiement valide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal totalDifference = _clientCredits.Sum(c => c.Difference);

                if (amount > totalDifference)
                {
                    MessageBox.Show(
                        $"Le montant du paiement ({amount:N2} DH) dépasse le crédit restant ({totalDifference:N2} DH).\n\nVeuillez entrer un montant inférieur ou égal à {totalDifference:N2} DH.",
                        "Montant Invalide",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Save original paye
                _originalPaidAmounts.Clear();
                foreach (var credit in _clientCredits)
                    _originalPaidAmounts[credit.CreditID] = credit.Paye;

                decimal remaining = amount;
                var creditsToUpdate = new List<Credit>();

                // FIFO allocation
                int creditId = 0;
                foreach (var credit in _clientCredits.OrderBy(c => c.CreditID))
                {
                    if (remaining <= 0) break;
                    if (credit.Difference <= 0) continue;

                    decimal payForThisCredit = Math.Min(credit.Difference, remaining);

                    credit.Paye += payForThisCredit;
                    credit.Difference = credit.Total - credit.Paye;
                    creditId = credit.CreditID;
                    creditsToUpdate.Add(credit);

                    remaining -= payForThisCredit;
                }

                // Update credits in database
                foreach (var credit in creditsToUpdate)
                {
                    int res = await credit.UpdateCreditAsync();
                    if (res == 0)
                    {
                        MessageBox.Show("Erreur lors de la mise à jour des crédits.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Update in MainWindow list
                    var creditInList = _mainWindow.credits.FirstOrDefault(c => c.CreditID == credit.CreditID);
                    if (creditInList != null)
                    {
                        creditInList.Paye = credit.Paye;
                        creditInList.Difference = credit.Difference;
                    }
                }

                // Reload from list
                LoadCredits();

                // Create operations
                await CreatePaymentOperationAsync(amount, creditId);

                //MessageBox.Show(
                //    $"Paiement de {amount:N2} DH traité avec succès.",
                //    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);


                PaymentAmountTextBox.Clear();

                //this.DialogResult = true;
                //this.Close();
                WCongratulations wCongratulations = new WCongratulations("Paiement avec Succes", $"Paiement de {amount:N2} DH traité avec succès.", 1);
                wCongratulations.ShowDialog();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Erreur lors du traitement du paiement: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

                WCongratulations wCongratulations = new WCongratulations("Payement Echoue", "Payement n'a pas ete effectue", 0);
                wCongratulations.ShowDialog();
            }
        }

        private async Task CreatePaymentOperationAsync(decimal paidAmount, int creditID)
        {
            try
            {
                // Create operation with current date
                DateTime currentDate = DateTime.Now;

                var op = new Operation
                {
                    ClientID = _client.ClientID,
                    FournisseurID = null,
                    CreditID = creditID,
                    PrixOperation = paidAmount,
                    Remise = 0m,
                    CreditValue = -paidAmount,
                    UserID = GetCurrentUserId(),
                    DateOperation = currentDate,
                    Date = currentDate, // Important: Set both Date properties
                    OperationType = "PAYMENT",
                    //Reversed = false,
                    Etat = true
                };

                int opId = await op.InsertOperationAsync();

                if (opId > 0)
                {
                    op.OperationID = opId;
                    _mainWindow.lo.Add(op);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Paiement enregistré mais échec de l'enregistrement de l'opération: {ex.Message}",
                    "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private int GetCurrentUserId()
        {
            return _currentUser.UserID;
        }
    }
}