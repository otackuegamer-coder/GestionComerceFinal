using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestionComerce.Main.FournisseurPage
{
    public partial class SingleRowSupplier : UserControl
    {
        private readonly MainWindow _main;
        private readonly User _currentUser;
        private Fournisseur _currentSupplier;

        public SingleRowSupplier(MainWindow main, User currentUser)
        {
            InitializeComponent();
            _main = main;
            _currentUser = currentUser;
            DataContextChanged += SingleRowSupplier_DataContextChanged;

            // Apply role-based permissions
            foreach (Role r in _main.lr)
            {
                if (_currentUser.RoleID == r.RoleID)
                {
                    if (!r.ModifyFournisseur)
                    {
                        Update.IsEnabled = false;
                    }
                    if (!r.DeleteFournisseur)
                    {
                        Delete.IsEnabled = false;
                    }
                    if (!r.PayeFournisseur && !r.ViewCreditFournisseur)
                    {
                        Paid.IsEnabled = false;
                    }
                    if (!r.ViewOperation)
                    {
                        Operation.IsEnabled = false;
                    }
                }
            }
        }

        private void SingleRowSupplier_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Fournisseur fournisseur)
            {
                _currentSupplier = fournisseur;
                PopulateRow(fournisseur);
            }
        }

        private void PopulateRow(Fournisseur fournisseur)
        {
            // ID
          

            // Name
            NameText.Text = fournisseur.Nom ?? "N/A";

            // Code
            CodeText.Text = string.IsNullOrWhiteSpace(fournisseur.Code) ? "-" : fournisseur.Code;

            // Phone
            PhoneText.Text = string.IsNullOrWhiteSpace(fournisseur.Telephone) ? "-" : fournisseur.Telephone;

            // ICE
            ICEText.Text = string.IsNullOrWhiteSpace(fournisseur.ICE) ? "-" : fournisseur.ICE;

            // État Juridique
            EtatJuridicText.Text = string.IsNullOrWhiteSpace(fournisseur.EtatJuridic) ? "-" : fournisseur.EtatJuridic;

            // Balance calculation
            var supplierCredits = _main.credits
                .Where(c => c.FournisseurID == fournisseur.FournisseurID && c.Etat)
                .ToList();

            decimal balance = supplierCredits.Sum(c => c.Difference);
            BalanceText.Text = $"{balance:F2} DH";
            BalanceText.Foreground = balance > 0 ? Brushes.Red : Brushes.Green;
        }

        public void RefreshRow()
        {
            if (_currentSupplier != null)
            {
                PopulateRow(_currentSupplier);
            }
        }

        private void Paid_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Fournisseur fournisseur)) return;

            var wnd = new PaidSupplierWindow(_currentUser, _main, fournisseur);
            bool? result = wnd.ShowDialog();

            if (result == true)
            {
                // Payment was successful - refresh balance
                System.Diagnostics.Debug.WriteLine("[PAYMENT] Payment successful, refreshing balance");

                // Refresh this row to show new balance
                PopulateRow(fournisseur);

                // Refresh parent to update statistics
                var parent = FindParentCMainF();
                if (parent != null)
                {
                    parent.UpdateStatistics();
                    parent.RefreshSupplierDisplay();
                }
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSupplier == null) return;
            if (_main == null) return;

            System.Diagnostics.Debug.WriteLine($"[UPDATE] Before update - Name: {_currentSupplier.Nom}, Code: {_currentSupplier.Code}");

            var wnd = new SupplierFormWindow(_main, _currentSupplier); // edit mode
            wnd.SupplierSaved += (s, ev) =>
            {
                System.Diagnostics.Debug.WriteLine($"[UPDATE] SupplierSaved event fired - Name: {_currentSupplier.Nom}, Code: {_currentSupplier.Code}");

                // Force refresh this row
                PopulateRow(_currentSupplier);
            };

            bool? res = wnd.ShowDialog();

            if (res == true)
            {
                System.Diagnostics.Debug.WriteLine($"[UPDATE] After update - Name: {_currentSupplier.Nom}, Code: {_currentSupplier.Code}");

                // Force refresh this row again
                PopulateRow(_currentSupplier);

                // Refresh the parent CMainF
                var parent = FindParentCMainF();
                if (parent != null)
                {
                    parent.RefreshSupplierDisplay();
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Fournisseur f)) return;
            if (_main == null) return;

            var wnd = new DeleteSupplierWindow(_main, f);
            bool? res = wnd.ShowDialog();

            if (res == true)
            {
                // DeleteSupplierWindow already:
                // 1. Called DeleteFournisseurAsync() 
                // 2. Updated MainWindow.lfo
                // We just need to refresh the UI

                var parent = FindParentCMainF();
                parent?.LoadAllData();
            }
        }

        private void Operations_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Fournisseur f)) return;
            if (_main == null) return;

            var window = new SupplierOperationsWindow(_main, f, _currentUser);
            window.ShowDialog();
        }

        // Helper method to find parent CMainF
        private CMainF FindParentCMainF()
        {
            DependencyObject current = this;
            while (current != null)
            {
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                if (current is CMainF cmainf)
                    return cmainf;
            }
            return null;
        }
    }
}