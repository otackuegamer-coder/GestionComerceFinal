using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce.Main.FournisseurPage
{
    public partial class DeleteSupplierWindow : Window
    {
        private readonly MainWindow _mainWindow;
        private readonly Fournisseur _supplier;

        public DeleteSupplierWindow(MainWindow mainWindow, Fournisseur supplier)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _supplier = supplier;
            SupplierNameLabel.Text = supplier.Nom;
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            // Soft delete: set Etat=0 in DB
            var f = new Fournisseur { FournisseurID = _supplier.FournisseurID };
            int res = await f.DeleteFournisseurAsync();

            if (res > 0)
            {
                // Update the list in MainWindow
                var supplierInList = _mainWindow.lfo.FirstOrDefault(x => x.FournisseurID == _supplier.FournisseurID);
                if (supplierInList != null)
                {
                    supplierInList.Etat = false;
                }

                // ✅ CRITICAL: Set DialogResult BEFORE showing any other dialog
                DialogResult = true;

                // Show success message
                WCongratulations wCongratulations = new WCongratulations("Suppression Succès", "Fournisseur supprimé avec succès", 1);
                wCongratulations.ShowDialog();

                // Close this window
                Close();
            }
            else
            {
                // Show error message
                WCongratulations wCongratulations = new WCongratulations("Suppression Échouée", "Fournisseur n'a pas été supprimé", 0);
                wCongratulations.ShowDialog();

                // Don't close on failure, let user try again or cancel
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}