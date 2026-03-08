
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace GestionComerce.Main.Vente
{
    public partial class WAddCleint : Window
    {
        private readonly List<Client> _clientList;
        private readonly WSelectCient _parentWindow;

        public WAddCleint(List<Client> lc, WSelectCient parent)
        {
            InitializeComponent();
            _clientList = lc;
            _parentWindow = parent;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Veuillez entrer un nom.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newName = NameTextBox.Text.Trim();
            string newPhone = PhoneTextBox.Text.Trim();
            bool isCompany = IsCompanyCheckBox.IsChecked == true;

            // Validate company-specific fields if company is selected
            if (isCompany && string.IsNullOrWhiteSpace(ICETextBox.Text))
            {
                MessageBox.Show("Veuillez entrer l'ICE pour l'entreprise.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate balance
            decimal parsedBalance = 0m;
            if (!string.IsNullOrWhiteSpace(BalanceTextBox.Text) &&
                !decimal.TryParse(BalanceTextBox.Text, out parsedBalance))
            {
                MessageBox.Show("Le solde doit être un nombre.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for duplicates in the list
            if (_clientList != null)
            {
                // Check for duplicate name
                var existingName = _clientList.FirstOrDefault(c =>
                    c.Nom.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                    c.Etat);

                if (existingName != null)
                {
                    MessageBox.Show($"Un client avec le nom '{newName}' existe déjà.",
                        "Nom en double", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate phone number (only if phone is provided)
                if (!string.IsNullOrWhiteSpace(newPhone))
                {
                    var existingPhone = _clientList.FirstOrDefault(c =>
                        !string.IsNullOrWhiteSpace(c.Telephone) &&
                        c.Telephone.Equals(newPhone, StringComparison.OrdinalIgnoreCase) &&
                        c.Etat);

                    if (existingPhone != null)
                    {
                        MessageBox.Show($"Un client avec le numéro '{newPhone}' existe déjà.",
                            "Téléphone en double", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Check for duplicate ICE (only if company and ICE is provided)
                if (isCompany && !string.IsNullOrWhiteSpace(ICETextBox.Text))
                {
                    string newICE = ICETextBox.Text.Trim();
                    var existingICE = _clientList.FirstOrDefault(c =>
                        c.IsCompany &&
                        !string.IsNullOrWhiteSpace(c.ICE) &&
                        c.ICE.Equals(newICE, StringComparison.OrdinalIgnoreCase) &&
                        c.Etat);

                    if (existingICE != null)
                    {
                        MessageBox.Show($"Une entreprise avec l'ICE '{newICE}' existe déjà.",
                            "ICE en double", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            // Create new client object
            Client newClient = new Client
            {
                Nom = newName,
                Telephone = newPhone,
                Adresse = AdresseTextBox.Text.Trim(),
                IsCompany = isCompany,
                Etat = true
            };

            if (isCompany)
            {
                newClient.EtatJuridique = EtatJuridiqueTextBox.Text.Trim();
                newClient.ICE = ICETextBox.Text.Trim();
                newClient.SiegeEntreprise = SiegeEntrepriseTextBox.Text.Trim();
                newClient.Code = CodeTextBox.Text.Trim();
            }

            // Save to database
            int newId = await newClient.InsertClientAsync();

            if (newId > 0)
            {
                // Update the client object with the new ID
                newClient.ClientID = newId;

                // Add to the list
                if (_clientList != null)
                {
                    _clientList.Add(newClient);
                }

                // Create credit if balance is provided
                if (parsedBalance > 0)
                {
                    Credit credit = new Credit
                    {
                        ClientID = newId,
                        Total = parsedBalance,
                        Paye = 0,
                        Difference = parsedBalance,
                        Etat = true
                    };

                    try
                    {
                        await credit.InsertCreditAsync();
                    }
                    catch
                    {
                        // Credit creation failed but client was added
                        MessageBox.Show("Client ajouté mais le crédit n'a pas pu être créé.",
                            "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Reload the client list in parent window
                _parentWindow.LoadClients();

                MessageBox.Show("Client ajouté avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);  

                Close();
            }
            else
            {
                MessageBox.Show("Échec de l'ajout du client.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IsCompanyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isCompany = IsCompanyCheckBox.IsChecked == true;
            CompanyFieldsPanel.Visibility = isCompany ? Visibility.Visible : Visibility.Collapsed;
            NameLabel.Text = isCompany ? "Nom de l'entreprise" : "Nom du client";

            // Clear company fields if unchecked
            if (!isCompany)
            {
                EtatJuridiqueTextBox.Text = string.Empty;
                ICETextBox.Text = string.Empty;
                SiegeEntrepriseTextBox.Text = string.Empty;
                CodeTextBox.Text = string.Empty;
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits and optionally one decimal separator
            Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
            e.Handled = !regex.IsMatch(e.Text);
        }
    }
}