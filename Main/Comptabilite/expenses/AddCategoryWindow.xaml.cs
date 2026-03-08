using System;
using System.Windows;
using System.Windows.Input;

namespace Superete.Main.Comptabilite
{
    public partial class AddCategoryWindow : Window
    {
        public string CategoryName { get; private set; }
        public string CategoryDescription { get; private set; }

        public AddCategoryWindow()
        {
            InitializeComponent();

            // Set focus to the category name field
            Loaded += (s, e) => txtCategoryName.Focus();

            // Allow Enter key to submit
            txtCategoryName.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtCategoryName.Text))
                {
                    BtnAdd_Click(s, new RoutedEventArgs());
                }
            };
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCategoryName.Text))
            {
                MessageBox.Show("⚠️ Veuillez entrer le nom de la catégorie.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtCategoryName.Focus();
                return;
            }

            CategoryName = txtCategoryName.Text.Trim();
            CategoryDescription = txtDescription.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}