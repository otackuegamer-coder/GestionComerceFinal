using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestionComerce;

namespace Superete.Main.Comptabilite
{
    public partial class Expenses : UserControl
    {
        private int alertValue = 5;
        private string alertUnit = "days";

        private List<string> recurringTypes = new List<string>
        {
            "Une fois", "Quotidien", "Hebdomadaire", "Mensuel",
            "Trimestriel", "Semestriel", "Annuel"
        };

        public Expenses()
        {
            InitializeComponent();
            // Use Loaded event to avoid blocking the UI thread with sync HTTP calls
            this.Loaded += Expenses_Loaded;
        }

        private async void Expenses_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeForm();
            LoadRecurringTypes();
            await LoadCategoriesAsync();
            await LoadExpensesAsync();
            await CheckUpcomingPaymentsAsync();
        }

        #region Initialization

        private void InitializeForm()
        {
            txtAlertValue.Text = alertValue.ToString();
            foreach (ComboBoxItem item in cmbAlertUnit.Items)
            {
                if (item.Tag?.ToString() == alertUnit)
                {
                    cmbAlertUnit.SelectedItem = item;
                    break;
                }
            }
            dpDueDate.SelectedDate = DateTime.Today;
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                List<ExpenseCategories> categories = await ExpenseCategories.GetAllActiveAsync();
                cmbCategory.Items.Clear();
                cmbFilterCategory.Items.Clear();
                cmbFilterCategory.Items.Add(new ComboBoxItem { Content = "Toutes" });
                foreach (var category in categories)
                {
                    cmbCategory.Items.Add(category.CategoryName);
                    cmbFilterCategory.Items.Add(new ComboBoxItem { Content = category.CategoryName });
                }
                if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
                cmbFilterCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des catégories:\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecurringTypes()
        {
            cmbRecurring.Items.Clear();
            foreach (var type in recurringTypes) cmbRecurring.Items.Add(type);
            cmbRecurring.SelectedIndex = 0;
        }

        #endregion

        #region Data Loading

        private async Task LoadExpensesAsync()
        {
            try
            {
                List<GestionComerce.Expenses> expenses = await GestionComerce.Expenses.GetAllAsync();
                foreach (var expense in expenses)
                    if (expense.PaymentStatus == "Pending" && expense.DueDate < DateTime.Today)
                        expense.PaymentStatus = "Overdue";
                dgExpenses.ItemsSource = null;
                dgExpenses.ItemsSource = expenses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des dépenses:\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CheckUpcomingPaymentsAsync()
        {
            try
            {
                int daysAhead = CalculateDaysAhead();
                List<GestionComerce.Expenses> all = await GestionComerce.Expenses.GetAllAsync();
                DateTime cutoff = DateTime.Today.AddDays(daysAhead);
                var upcoming = all.Where(e =>
                    (e.PaymentStatus == "Pending" || e.PaymentStatus == "Overdue") &&
                    e.DueDate <= cutoff).ToList();

                if (upcoming.Count > 0)
                {
                    AlertBanner.Visibility = Visibility.Visible;
                    AlertList.ItemsSource = upcoming;
                    string unitText = alertUnit == "hours" ? "heures" :
                                     alertUnit == "days" ? "jours" : "semaines";
                    txtAlertInfo.Text = $"Alerte: {alertValue} {unitText} avant échéance";
                }
                else
                {
                    AlertBanner.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la vérification des paiements:\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int CalculateDaysAhead()
        {
            switch (alertUnit)
            {
                case "hours": return Math.Max(1, alertValue / 24);
                case "weeks": return alertValue * 7;
                default: return alertValue;
            }
        }

        #endregion

        #region Form Validation

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtExpenseName.Text))
            {
                MessageBox.Show("Veuillez entrer le nom de la dépense.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtExpenseName.Focus(); return false;
            }
            if (cmbCategory.SelectedItem == null && string.IsNullOrWhiteSpace(cmbCategory.Text))
            {
                MessageBox.Show("Veuillez sélectionner ou entrer une catégorie.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCategory.Focus(); return false;
            }
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Veuillez entrer un montant valide supérieur à zéro.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAmount.Focus(); return false;
            }
            if (!dpDueDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Veuillez sélectionner une date d'échéance.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpDueDate.Focus(); return false;
            }
            return true;
        }

        #endregion

        #region Event Handlers - Alert Settings

        private void BtnAlertSettings_Click(object sender, RoutedEventArgs e)
        {
            AlertSettingsCard.Visibility = AlertSettingsCard.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void BtnSaveAlertSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtAlertValue.Text, out int value) || value <= 0)
            {
                MessageBox.Show("Veuillez entrer une valeur numérique valide supérieure à zéro.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            ComboBoxItem selectedItem = cmbAlertUnit.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner une unité de temps.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            alertValue = value;
            alertUnit = selectedItem.Tag.ToString();
            MessageBox.Show($"Paramètres d'alerte sauvegardés!\n\nAlerte: {alertValue} {selectedItem.Content} avant l'échéance.",
                "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            AlertSettingsCard.Visibility = Visibility.Collapsed;
            await CheckUpcomingPaymentsAsync();
        }

        #endregion

        #region Event Handlers - Add Expense

        private async void BtnAddExpense_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;
            try
            {
                string categoryText = cmbCategory.SelectedItem?.ToString() ?? cmbCategory.Text.Trim();
                string recurringText = cmbRecurring.SelectedItem?.ToString() ?? cmbRecurring.Text.Trim();

                GestionComerce.Expenses newExpense = new GestionComerce.Expenses
                {
                    ExpenseName = txtExpenseName.Text.Trim(),
                    Category = categoryText,
                    Amount = decimal.Parse(txtAmount.Text),
                    DueDate = dpDueDate.SelectedDate.Value,
                    RecurringType = recurringText,
                    Notes = txtNotes.Text.Trim()
                };

                bool success = await newExpense.Add();
                if (success)
                {
                    MessageBox.Show("Dépense ajoutée avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadExpensesAsync();
                    await CheckUpcomingPaymentsAsync();
                }
                else
                {
                    MessageBox.Show("Échec de l'ajout de la dépense.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout:\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearForm_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            txtExpenseName.Clear(); txtAmount.Clear(); txtNotes.Clear();
            dpDueDate.SelectedDate = DateTime.Today;
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
            cmbRecurring.SelectedIndex = 0;
            cmbPaymentMethod.SelectedIndex = 0;
            txtExpenseName.Focus();
        }

        #endregion

        #region Event Handlers - Category Management

        private async void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCategoryWindow();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ExpenseCategories newCategory = new ExpenseCategories
                    {
                        CategoryName = dialog.CategoryName,
                        Description = dialog.CategoryDescription,
                        IsActive = true
                    };
                    bool success = await newCategory.Add();
                    if (success)
                    {
                        MessageBox.Show("Catégorie ajoutée avec succès!",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadCategoriesAsync();
                        cmbCategory.Text = dialog.CategoryName;
                    }
                    else
                    {
                        MessageBox.Show("Échec de l'ajout de la catégorie.",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur:\n\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Event Handlers - Recurrence Management

        private void BtnAddRecurrence_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddRecurrenceWindow();
            if (dialog.ShowDialog() == true)
            {
                string newRecurrence = dialog.RecurrenceName;
                if (!recurringTypes.Contains(newRecurrence))
                {
                    recurringTypes.Add(newRecurrence);
                    cmbRecurring.Items.Add(newRecurrence);
                    cmbRecurring.Text = newRecurrence;
                    MessageBox.Show("Type de récurrence ajouté avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ce type de récurrence existe déjà.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Event Handlers - Expense Actions

        private async void BtnPayExpense_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is GestionComerce.Expenses expense)) return;
            if (expense.PaymentStatus == "Paid")
            {
                MessageBox.Show("Cette dépense a déjà été payée.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information); return;
            }
            var result = MessageBox.Show(
                $"Confirmer le paiement de:\n\nDépense: {expense.ExpenseName}\n" +
                $"Montant: {expense.Amount:N2} MAD\nCatégorie: {expense.Category}\n\n" +
                $"Voulez-vous marquer cette dépense comme payée?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                string pm = cmbPaymentMethod.SelectedItem != null
                    ? ((ComboBoxItem)cmbPaymentMethod.SelectedItem).Content.ToString() : "Cash";
                bool success = await expense.MarkAsPaid(pm);
                if (success)
                {
                    MessageBox.Show("Paiement enregistré avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadExpensesAsync();
                    await CheckUpcomingPaymentsAsync();
                }
                else
                    MessageBox.Show("Échec de l'enregistrement du paiement.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur:\n\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEditExpense_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is GestionComerce.Expenses expense)) return;
            var dialog = new EditExpenseWindow(expense);
            if (dialog.ShowDialog() == true)
            {
                await LoadExpensesAsync();
                await CheckUpcomingPaymentsAsync();
            }
        }

        private async void BtnDeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is GestionComerce.Expenses expense)) return;
            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cette dépense?\n\n" +
                $"Nom: {expense.ExpenseName}\nMontant: {expense.Amount:N2} MAD\n\nCette action est irréversible!",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                bool success = await expense.Delete();
                if (success)
                {
                    MessageBox.Show("Dépense supprimée avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadExpensesAsync();
                    await CheckUpcomingPaymentsAsync();
                }
                else
                    MessageBox.Show("Échec de la suppression.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur:\n\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Event Handlers - Filtering

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadExpensesAsync();
            await CheckUpcomingPaymentsAsync();
            MessageBox.Show("Données actualisées!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<GestionComerce.Expenses> all = await GestionComerce.Expenses.GetAllAsync();
                List<GestionComerce.Expenses> filtered = all;

                ComboBoxItem statusItem = cmbFilterStatus.SelectedItem as ComboBoxItem;
                if (statusItem != null && statusItem.Content.ToString() != "Tous")
                    filtered = filtered.Where(exp => exp.PaymentStatus == statusItem.Content.ToString()).ToList();

                ComboBoxItem categoryItem = cmbFilterCategory.SelectedItem as ComboBoxItem;
                if (categoryItem != null && categoryItem.Content.ToString() != "Toutes")
                    filtered = filtered.Where(exp => exp.Category == categoryItem.Content.ToString()).ToList();

                ComboBoxItem periodItem = cmbFilterPeriod.SelectedItem as ComboBoxItem;
                if (periodItem != null)
                {
                    DateTime now = DateTime.Now;
                    switch (periodItem.Content.ToString())
                    {
                        case "Ce mois":
                            filtered = filtered.Where(exp =>
                                exp.DueDate.Year == now.Year && exp.DueDate.Month == now.Month).ToList(); break;
                        case "Ce trimestre":
                            int q = (now.Month - 1) / 3 + 1;
                            filtered = filtered.Where(exp =>
                                exp.DueDate.Year == now.Year && (exp.DueDate.Month - 1) / 3 + 1 == q).ToList(); break;
                        case "Cette année":
                            filtered = filtered.Where(exp => exp.DueDate.Year == now.Year).ToList(); break;
                    }
                }

                dgExpenses.ItemsSource = null;
                dgExpenses.ItemsSource = filtered;
                MessageBox.Show($"Filtre appliqué!\n\n{filtered.Count} dépense(s) trouvée(s).",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du filtrage:\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Input Validation

        private void TxtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*\.?[0-9]*$");
            string newText = txtAmount.Text.Insert(txtAmount.CaretIndex, e.Text);
            e.Handled = !regex.IsMatch(newText);
        }

        #endregion
    }
}