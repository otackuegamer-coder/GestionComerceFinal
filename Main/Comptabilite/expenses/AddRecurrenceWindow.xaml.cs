using System;
using System.Windows;
using System.Windows.Input;

namespace Superete.Main.Comptabilite
{
    public partial class AddRecurrenceWindow : Window
    {
        public string RecurrenceName { get; private set; }

        public AddRecurrenceWindow()
        {
            InitializeComponent();

            // Set focus to the recurrence name field
            Loaded += (s, e) => txtRecurrenceName.Focus();

            // Allow Enter key to submit
            txtRecurrenceName.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtRecurrenceName.Text))
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
            if (string.IsNullOrWhiteSpace(txtRecurrenceName.Text))
            {
                MessageBox.Show("⚠️ Veuillez entrer le nom du type de récurrence.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtRecurrenceName.Focus();
                return;
            }

            RecurrenceName = txtRecurrenceName.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}