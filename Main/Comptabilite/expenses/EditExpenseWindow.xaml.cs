using GestionComerce;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Superete.Main.Comptabilite
{
    public partial class EditExpenseWindow : Window
    {
        private GestionComerce.Expenses expense;

        public EditExpenseWindow(GestionComerce.Expenses exp)
        {
            InitializeComponent();
            expense = exp;
            LoadRecurringTypes();
            LoadExpenseData();
            // Load categories async after control is ready
            this.Loaded += async (s, e) => await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                List<ExpenseCategories> categories = await ExpenseCategories.GetAllActiveAsync();
                cmbCategory.Items.Clear();
                foreach (var cat in categories)
                    cmbCategory.Items.Add(cat.CategoryName);
                // Restore selected category
                cmbCategory.Text = expense.Category;
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
            foreach (var t in new[] { "Une fois", "Quotidien", "Hebdomadaire", "Mensuel",
                                      "Trimestriel", "Semestriel", "Annuel" })
                cmbRecurring.Items.Add(t);
        }

        private void LoadExpenseData()
        {
            txtName.Text = expense.ExpenseName;
            cmbCategory.Text = expense.Category;
            txtAmount.Text = expense.Amount.ToString();
            dpDueDate.SelectedDate = expense.DueDate;
            cmbRecurring.Text = expense.RecurringType;
            txtNotes.Text = expense.Notes;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Veuillez entrer le nom de la dépense.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus(); return;
            }
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Veuillez entrer un montant valide supérieur à zéro.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAmount.Focus(); return;
            }
            if (!dpDueDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Veuillez sélectionner une date d'échéance.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpDueDate.Focus(); return;
            }

            try
            {
                expense.ExpenseName = txtName.Text.Trim();
                expense.Category = cmbCategory.Text.Trim();
                expense.Amount = amount;
                expense.DueDate = dpDueDate.SelectedDate.Value;
                expense.RecurringType = cmbRecurring.Text.Trim();
                expense.Notes = txtNotes.Text.Trim();

                bool success = await expense.Update();
                if (success)
                {
                    MessageBox.Show("Dépense mise à jour avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Échec de la mise à jour de la dépense.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour:\n\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}