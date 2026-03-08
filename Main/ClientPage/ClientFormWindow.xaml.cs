using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GestionComerce.Main.ClientPage
{
    public partial class ClientFormWindow : Window
    {
        private readonly Client _editingClient;
        private readonly bool _isEdit;
        private MainWindow _main;

        // keep track if we inserted a credit during this session
        private Credit _insertedCredit = null;

        public ClientFormWindow(MainWindow main, Client client = null)
        {
            InitializeComponent();
            _main = main;

            if (client == null)
            {
                _isEdit = false;
                _editingClient = new Client();
                UpdateButton.Content = "Add";
                this.Title = "Add Client";

                // Set default remise to 0
                RemiseTextBox.Text = "0";
            }
            else
            {
                _isEdit = true;
                _editingClient = client;
                UpdateButton.Content = "Update";
                this.Title = "Update Client";

                // populate fields
                NameTextBox.Text = _editingClient.Nom;
                PhoneTextBox.Text = _editingClient.Telephone;
                AdresseTextBox.Text = _editingClient.Adresse;
                IsCompanyCheckBox.IsChecked = _editingClient.IsCompany;

                // Populate Remise field
                RemiseTextBox.Text = (_editingClient.Remise ?? 0).ToString("F2");

                // Populate company fields if it's a company
                if (_editingClient.IsCompany)
                {
                    EtatJuridiqueTextBox.Text = _editingClient.EtatJuridique;
                    ICETextBox.Text = _editingClient.ICE;
                    SiegeEntrepriseTextBox.Text = _editingClient.SiegeEntreprise;
                    CodeTextBox.Text = _editingClient.Code;
                    CompanyFieldsPanel.Visibility = Visibility.Visible;
                    NameLabel.Text = "Company Name";
                }

                // read balance from MainWindow in-memory credits
                try
                {
                    var credits = _main?.credits ?? new System.Collections.Generic.List<Credit>();
                    var my = credits.FindAll(c => c.ClientID == _editingClient.ClientID && c.Etat);
                    decimal diff = my.Count > 0 ? my.Sum(x => x.Difference) : 0m;
                    BalanceTextBox.Text = diff.ToString("F2");
                }
                catch
                {
                    // keep silent — balance is optional for display
                }

                // Disable balance field when editing - it's display only
                BalanceTextBox.IsReadOnly = true;
                BalanceTextBox.Background = System.Windows.Media.Brushes.LightGray;
            }
        }

        private void IsCompanyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isCompany = IsCompanyCheckBox.IsChecked == true;
            CompanyFieldsPanel.Visibility = isCompany ? Visibility.Visible : Visibility.Collapsed;
            NameLabel.Text = isCompany ? "Company Name" : "Client Name";

            // Clear company fields if unchecked
            if (!isCompany)
            {
                EtatJuridiqueTextBox.Text = string.Empty;
                ICETextBox.Text = string.Empty;
                SiegeEntrepriseTextBox.Text = string.Empty;
                CodeTextBox.Text = string.Empty;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Please enter a name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isCompany = IsCompanyCheckBox.IsChecked == true;

            // Validate company-specific fields if company is selected
            if (isCompany)
            {
                if (string.IsNullOrWhiteSpace(ICETextBox.Text))
                {
                    MessageBox.Show("Please enter ICE for the company.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            decimal parsedBalance = 0m;
            // Only validate and use balance when adding new client (not editing)
            if (!_isEdit)
            {
                if (!string.IsNullOrWhiteSpace(BalanceTextBox.Text) &&
                    !decimal.TryParse(BalanceTextBox.Text, out parsedBalance))
                {
                    MessageBox.Show("Balance must be a number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Validate Remise (must be a percentage between 0 and 100)
            decimal parsedRemise = 0m;
            if (!string.IsNullOrWhiteSpace(RemiseTextBox.Text))
            {
                if (!decimal.TryParse(RemiseTextBox.Text, out parsedRemise))
                {
                    MessageBox.Show("Remise must be a valid number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (parsedRemise < 0 || parsedRemise > 100)
                {
                    MessageBox.Show("Remise must be between 0 and 100 percent.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Check for duplicates (only when adding new client or when name/phone changed)
            string newName = NameTextBox.Text.Trim();
            string newPhone = PhoneTextBox.Text.Trim();

            if (_main.lc != null)
            {
                // Check for duplicate name
                var existingName = _main.lc.FirstOrDefault(c =>
                    c.Nom.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                    c.Etat &&
                    (!_isEdit || c.ClientID != _editingClient.ClientID));

                if (existingName != null)
                {
                    MessageBox.Show($"A client with the name '{newName}' already exists.",
                        "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate phone number (only if phone is provided)
                if (!string.IsNullOrWhiteSpace(newPhone))
                {
                    var existingPhone = _main.lc.FirstOrDefault(c =>
                        !string.IsNullOrWhiteSpace(c.Telephone) &&
                        c.Telephone.Equals(newPhone, StringComparison.OrdinalIgnoreCase) &&
                        c.Etat &&
                        (!_isEdit || c.ClientID != _editingClient.ClientID));

                    if (existingPhone != null)
                    {
                        MessageBox.Show($"A client with the phone number '{newPhone}' already exists.",
                            "Duplicate Phone", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Check for duplicate ICE (only if company and ICE is provided)
                if (isCompany && !string.IsNullOrWhiteSpace(ICETextBox.Text))
                {
                    string newICE = ICETextBox.Text.Trim();
                    var existingICE = _main.lc.FirstOrDefault(c =>
                        c.IsCompany &&
                        !string.IsNullOrWhiteSpace(c.ICE) &&
                        c.ICE.Equals(newICE, StringComparison.OrdinalIgnoreCase) &&
                        c.Etat &&
                        (!_isEdit || c.ClientID != _editingClient.ClientID));

                    if (existingICE != null)
                    {
                        MessageBox.Show($"A company with the ICE '{newICE}' already exists.",
                            "Duplicate ICE", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            // Populate client object
            _editingClient.Nom = newName;
            _editingClient.Telephone = newPhone;
            _editingClient.Adresse = AdresseTextBox.Text.Trim();
            _editingClient.IsCompany = isCompany;
            _editingClient.Remise = parsedRemise; // Set the remise percentage

            if (isCompany)
            {
                _editingClient.EtatJuridique = EtatJuridiqueTextBox.Text.Trim();
                _editingClient.ICE = ICETextBox.Text.Trim();
                _editingClient.SiegeEntreprise = SiegeEntrepriseTextBox.Text.Trim();
                _editingClient.Code = CodeTextBox.Text.Trim();
            }
            else
            {
                _editingClient.EtatJuridique = null;
                _editingClient.ICE = null;
                _editingClient.SiegeEntreprise = null;
                _editingClient.Code = null;
            }

            if (_isEdit)
            {
                // Save to database
                int result = await _editingClient.UpdateClientAsync();

                if (result > 0)
                {
                    // Update MainWindow list
                    var existing = _main.lc.FirstOrDefault(c => c.ClientID == _editingClient.ClientID);
                    if (existing != null)
                    {
                        existing.Nom = _editingClient.Nom;
                        existing.Telephone = _editingClient.Telephone;
                        existing.Adresse = _editingClient.Adresse;
                        existing.IsCompany = _editingClient.IsCompany;
                        existing.EtatJuridique = _editingClient.EtatJuridique;
                        existing.ICE = _editingClient.ICE;
                        existing.SiegeEntreprise = _editingClient.SiegeEntreprise;
                        existing.Code = _editingClient.Code;
                        existing.Remise = _editingClient.Remise; // Update remise
                    }

                    // DO NOT create a new credit when updating - balance is display only

                    // Set DialogResult to true for successful operation
                    this.DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to update client.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Save to database
                int newId = await _editingClient.InsertClientAsync();
                if (newId > 0)
                {
                    // Update the client object with the new ID
                    _editingClient.ClientID = newId;
                    _editingClient.Etat = true;

                    // Add to MainWindow list
                    if (_main.lc == null)
                        _main.lc = new System.Collections.Generic.List<Client>();
                    _main.lc.Add(_editingClient);

                    // Only create credit for new clients
                    if (parsedBalance > 0)
                    {
                        var credit = new Credit
                        {
                            ClientID = newId,
                            Total = parsedBalance,
                            Paye = 0,
                            Difference = parsedBalance,
                            Etat = true
                        };

                        try
                        {
                            var insertRes = await credit.InsertCreditAsync();
                            _insertedCredit = credit;

                            // Add to MainWindow list
                            if (_main.credits == null)
                                _main.credits = new System.Collections.Generic.List<Credit>();
                            _main.credits.Add(credit);
                        }
                        catch
                        {
                            // ignore - we still added the client
                        }
                    }

                    // Set DialogResult to true for successful operation
                    this.DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to add client.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// This method is kept for backward compatibility but is no longer needed
        /// since we update lists directly in UpdateButton_Click
        /// </summary>
        public async Task SaveClientToDatabaseAndList()
        {
            // No longer needed - kept for compatibility
            await Task.CompletedTask;
        }

        /// <summary>
        /// Validates and formats the Remise input (percentage only, 0-100)
        /// </summary>
        private void RemiseTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only numbers and one decimal point
            Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Handles text changes in the Remise TextBox
        /// </summary>
        private void RemiseTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Optional: You can add real-time validation or formatting here
            if (string.IsNullOrWhiteSpace(RemiseTextBox.Text))
            {
                return;
            }

            if (decimal.TryParse(RemiseTextBox.Text, out decimal value))
            {
                // Optionally limit the value between 0 and 100 in real-time
                if (value > 100)
                {
                    RemiseTextBox.Text = "100";
                    RemiseTextBox.SelectionStart = RemiseTextBox.Text.Length;
                }
                else if (value < 0)
                {
                    RemiseTextBox.Text = "0";
                    RemiseTextBox.SelectionStart = RemiseTextBox.Text.Length;
                }
            }
        }
    }
}