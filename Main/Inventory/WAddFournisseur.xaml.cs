using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce.Main.Inventory
{
    public partial class WAddFournisseur : Window
    {
        public event EventHandler SupplierSaved;
        private readonly MainWindow _mainWindow;
        private readonly Fournisseur _editingSupplier;
        private readonly bool _isEdit;

        // Constructor for Add mode
        public WAddFournisseur(MainWindow mainWindow) : this(mainWindow, null)
        {
        }

        // if supplier == null -> Add mode; else Edit mode.
        public WAddFournisseur(MainWindow mainWindow, Fournisseur supplier = null)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            if (supplier == null)
            {
                _isEdit = false;
                _editingSupplier = new Fournisseur();
                UpdateButton.Content = "Ajouter";
                this.Title = "Ajouter Fournisseur";
            }
            else
            {
                _isEdit = true;
                _editingSupplier = supplier;
                UpdateButton.Content = "Modifier";
                this.Title = "Modifier Fournisseur";

                // Populate all fields
                NameTextBox.Text = _editingSupplier.Nom ?? string.Empty;
                CodeTextBox.Text = _editingSupplier.Code ?? string.Empty;
                PhoneTextBox.Text = _editingSupplier.Telephone ?? string.Empty;
                EtatJuridicTextBox.Text = _editingSupplier.EtatJuridic ?? string.Empty;
                ICETextBox.Text = _editingSupplier.ICE ?? string.Empty;
                SiegeEntrepriseTextBox.Text = _editingSupplier.SiegeEntreprise ?? string.Empty;
                AdresseTextBox.Text = _editingSupplier.Adresse ?? string.Empty;

                // Load balance from MainWindow credit list
                var myCredits = _mainWindow.credits
                    .Where(c => c.FournisseurID == _editingSupplier.FournisseurID && c.Etat)
                    .ToList();
                decimal diff = myCredits.Count > 0 ? myCredits.Sum(x => x.Difference) : 0m;
                BalanceTextBox.Text = diff.ToString("F2");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation - only name is required
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Veuillez entrer un nom.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate balance if provided
            decimal parsedBalance = 0m;
            if (!string.IsNullOrWhiteSpace(BalanceTextBox.Text) &&
                !decimal.TryParse(BalanceTextBox.Text, out parsedBalance))
            {
                MessageBox.Show("Le solde doit être un nombre.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get trimmed values
            string newName = NameTextBox.Text.Trim();
            string newCode = CodeTextBox.Text.Trim();
            string newPhone = PhoneTextBox.Text.Trim();
            string newEtatJuridic = EtatJuridicTextBox.Text.Trim();
            string newICE = ICETextBox.Text.Trim();
            string newSiege = SiegeEntrepriseTextBox.Text.Trim();
            string newAdresse = AdresseTextBox.Text.Trim();

            // Check for duplicates
            if (_mainWindow.lfo != null)
            {
                // Check for duplicate name
                var existingName = _mainWindow.lfo.FirstOrDefault(f =>
                    f.Nom.Equals(newName, StringComparison.OrdinalIgnoreCase) &&
                    f.Etat &&
                    (!_isEdit || f.FournisseurID != _editingSupplier.FournisseurID));

                if (existingName != null)
                {
                    MessageBox.Show($"Un fournisseur avec le nom '{newName}' existe déjà.",
                        "Nom Dupliqué", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicate code (only if code is provided)
                if (!string.IsNullOrWhiteSpace(newCode))
                {
                    var existingCode = _mainWindow.lfo.FirstOrDefault(f =>
                        !string.IsNullOrWhiteSpace(f.Code) &&
                        f.Code.Equals(newCode, StringComparison.OrdinalIgnoreCase) &&
                        f.Etat &&
                        (!_isEdit || f.FournisseurID != _editingSupplier.FournisseurID));

                    if (existingCode != null)
                    {
                        MessageBox.Show($"Un fournisseur avec le code '{newCode}' existe déjà.",
                            "Code Dupliqué", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Check for duplicate phone number (only if phone is provided)
                if (!string.IsNullOrWhiteSpace(newPhone))
                {
                    var existingPhone = _mainWindow.lfo.FirstOrDefault(f =>
                        !string.IsNullOrWhiteSpace(f.Telephone) &&
                        f.Telephone.Equals(newPhone, StringComparison.OrdinalIgnoreCase) &&
                        f.Etat &&
                        (!_isEdit || f.FournisseurID != _editingSupplier.FournisseurID));

                    if (existingPhone != null)
                    {
                        MessageBox.Show($"Un fournisseur avec le téléphone '{newPhone}' existe déjà.",
                            "Téléphone Dupliqué", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Check for duplicate ICE (only if ICE is provided)
                if (!string.IsNullOrWhiteSpace(newICE))
                {
                    var existingICE = _mainWindow.lfo.FirstOrDefault(f =>
                        !string.IsNullOrWhiteSpace(f.ICE) &&
                        f.ICE.Equals(newICE, StringComparison.OrdinalIgnoreCase) &&
                        f.Etat &&
                        (!_isEdit || f.FournisseurID != _editingSupplier.FournisseurID));

                    if (existingICE != null)
                    {
                        MessageBox.Show($"Un fournisseur avec l'ICE '{newICE}' existe déjà.",
                            "ICE Dupliqué", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            if (_isEdit)
            {
                // Update fournisseur with all new fields
                _editingSupplier.Nom = newName;
                _editingSupplier.Code = string.IsNullOrWhiteSpace(newCode) ? null : newCode;
                _editingSupplier.Telephone = string.IsNullOrWhiteSpace(newPhone) ? null : newPhone;
                _editingSupplier.EtatJuridic = string.IsNullOrWhiteSpace(newEtatJuridic) ? null : newEtatJuridic;
                _editingSupplier.ICE = string.IsNullOrWhiteSpace(newICE) ? null : newICE;
                _editingSupplier.SiegeEntreprise = string.IsNullOrWhiteSpace(newSiege) ? null : newSiege;
                _editingSupplier.Adresse = string.IsNullOrWhiteSpace(newAdresse) ? null : newAdresse;

                int result = await _editingSupplier.UpdateFournisseurAsync();

                if (result > 0)
                {
                    // Update in MainWindow list
                    var supplierInList = _mainWindow.lfo.FirstOrDefault(f => f.FournisseurID == _editingSupplier.FournisseurID);
                    if (supplierInList != null)
                    {
                        supplierInList.Nom = _editingSupplier.Nom;
                        supplierInList.Code = _editingSupplier.Code;
                        supplierInList.Telephone = _editingSupplier.Telephone;
                        supplierInList.EtatJuridic = _editingSupplier.EtatJuridic;
                        supplierInList.ICE = _editingSupplier.ICE;
                        supplierInList.SiegeEntreprise = _editingSupplier.SiegeEntreprise;
                        supplierInList.Adresse = _editingSupplier.Adresse;
                    }

                    // If user filled Balance field and it's >0, create a new credit record
                    if (parsedBalance > 0)
                    {
                        var credit = new Credit
                        {
                            FournisseurID = _editingSupplier.FournisseurID,
                            Total = parsedBalance,
                            Paye = 0,
                            Difference = parsedBalance,
                            Etat = true
                        };
                        int creditId = await credit.InsertCreditAsync();

                        // Add to MainWindow credit list
                        if (creditId > 0)
                        {
                            credit.CreditID = creditId;
                            _mainWindow.credits.Add(credit);
                        }
                    }

                    // Set DialogResult BEFORE showing success message
                    if (this.Owner != null)
                    {
                        DialogResult = true;
                    }

                    SupplierSaved?.Invoke(this, EventArgs.Empty);
                    WCongratulations wCongratulations = new WCongratulations("Modification Succès", "Fournisseur modifié avec succès", 1);
                    wCongratulations.ShowDialog();

                    Close();
                }
                else
                {
                    WCongratulations wCongratulations = new WCongratulations("Modification Échouée", "Le fournisseur n'a pas été modifié", 0);
                    wCongratulations.ShowDialog();
                }
            }
            else
            {
                // Insert new fournisseur with all fields
                _editingSupplier.Nom = newName;
                _editingSupplier.Code = string.IsNullOrWhiteSpace(newCode) ? null : newCode;
                _editingSupplier.Telephone = string.IsNullOrWhiteSpace(newPhone) ? null : newPhone;
                _editingSupplier.EtatJuridic = string.IsNullOrWhiteSpace(newEtatJuridic) ? null : newEtatJuridic;
                _editingSupplier.ICE = string.IsNullOrWhiteSpace(newICE) ? null : newICE;
                _editingSupplier.SiegeEntreprise = string.IsNullOrWhiteSpace(newSiege) ? null : newSiege;
                _editingSupplier.Adresse = string.IsNullOrWhiteSpace(newAdresse) ? null : newAdresse;
                _editingSupplier.Etat = true;

                int newId = await _editingSupplier.InsertFournisseurAsync();

                if (newId > 0)
                {
                    // Add to MainWindow list
                    _editingSupplier.FournisseurID = newId;
                    _mainWindow.lfo.Add(_editingSupplier);

                    // If initial balance > 0, create a credit record
                    if (parsedBalance > 0)
                    {
                        var credit = new Credit
                        {
                            FournisseurID = newId,
                            Total = parsedBalance,
                            Paye = 0,
                            Difference = parsedBalance,
                            Etat = true
                        };
                        int creditId = await credit.InsertCreditAsync();

                        // Add to MainWindow credit list
                        if (creditId > 0)
                        {
                            credit.CreditID = creditId;
                            _mainWindow.credits.Add(credit);
                        }
                    }

                    // Set DialogResult BEFORE showing success message
                    if (this.Owner != null)
                    {
                        DialogResult = true;
                    }

                    SupplierSaved?.Invoke(this, EventArgs.Empty);
                    MessageBox.Show("Fournisseur ajouté avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
                else
                {

                    MessageBox.Show("Fournisseur n'est pas ajouter.", "Échoué", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}