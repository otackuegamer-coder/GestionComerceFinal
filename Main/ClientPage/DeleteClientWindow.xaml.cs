using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GestionComerce.Main.ClientPage
{
    public partial class DeleteClientWindow : Window
    {
        private readonly Client _client;
        private readonly MainWindow _main;

        public DeleteClientWindow(MainWindow main, Client client)
        {
            InitializeComponent();
            _main = main;
            _client = client;
            SupplierNameLabel.Text = client.Nom;
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save to database (soft delete)
                var c = new Client { ClientID = _client.ClientID };
                int res = await c.DeleteClientAsync();

                if (res > 0)
                {
                    // Update MainWindow in-memory list: mark as soft-deleted (Etat = false)
                    var existing = _main.lc?.FirstOrDefault(x => x.ClientID == _client.ClientID);
                    if (existing != null)
                    {
                        existing.Etat = false; // keep object in list but mark inactive
                    }

                    //MessageBox.Show("Client hidden (soft deleted).", "Done",
                    //    MessageBoxButton.OK, MessageBoxImage.Information);
                    //DialogResult = true;
                    //Close();

                    WCongratulations wCongratulations = new WCongratulations("Suppression Succes", "Client Supprimer avec succes", 1);
                    wCongratulations.ShowDialog();
                }
                else
                {
                    //MessageBox.Show("Operation failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    WCongratulations wCongratulations = new WCongratulations("Suppression Echoue", "Client n'a pas ete Supprimer", 0);
                    wCongratulations.ShowDialog();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error while deleting client: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}