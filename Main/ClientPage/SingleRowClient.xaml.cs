using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestionComerce.Main.ClientPage
{
    public partial class SingleRowClient : UserControl
    {
        private readonly MainWindow _main;
        private readonly User _currentUser;

        // Constructor that only requires MainWindow (safe default)
        public SingleRowClient(MainWindow main) : this(main, null) { }

        // Constructor that accepts a specific User instance
        public SingleRowClient(MainWindow main, User currentUser)
        {
            InitializeComponent();
            _main = main;
            _currentUser = currentUser;
            DataContextChanged += SingleRowClient_DataContextChanged;

            foreach (Role r in _main.lr)
            {
                if (_currentUser.RoleID == r.RoleID)
                {
                    if (!r.ModifyClient)
                    {
                        Update.IsEnabled = false;
                    }
                    if (!r.DeleteClient)
                    {
                        Delete.IsEnabled = false;
                    }
                    if (!r.PayeClient && !r.ViewCreditClient)
                    {
                        Paye.IsEnabled = false;
                    }
                    if (!r.ViewOperation)
                    {
                        Operation.IsEnabled = false;
                    }
                    if (!r.ViewCreditClient)
                    {
                        BalanceText.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void SingleRowClient_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Client client)
                PopulateRow(client);
        }

        private void PopulateRow(Client client)
        {
           
            NameText.Text = client.Nom ?? "N/A";

            // Show company badge if it's a company
            if (client.IsCompany)
            {
                CompanyBadge.Visibility = Visibility.Visible;

                // Display ICE instead of phone for companies
                if (!string.IsNullOrWhiteSpace(client.ICE))
                {
                    PhoneText.Text = $"ICE: {client.ICE}";
                }
                else
                {
                    PhoneText.Text = string.IsNullOrEmpty(client.Telephone) ? "-" : client.Telephone;
                }

                // Display headquarters instead of address for companies
                AddressText.Text = string.IsNullOrEmpty(client.SiegeEntreprise) ? "-" : client.SiegeEntreprise;
            }
            else
            {
                CompanyBadge.Visibility = Visibility.Collapsed;
                PhoneText.Text = string.IsNullOrEmpty(client.Telephone) ? "-" : client.Telephone;
                AddressText.Text = string.IsNullOrEmpty(client.Adresse) ? "-" : client.Adresse;
            }

            // Load balance from MainWindow list
            var clientCredits = _main.credits
                .Where(c => c.ClientID == client.ClientID && c.Etat)
                .ToList();

            decimal balance = clientCredits.Sum(c => c.Difference);
            SetBalanceText($"{balance:F2} DH", balance > 0 ? Brushes.Red : Brushes.Green);
        }

        private void SetBalanceText(string text, Brush color)
        {
            BalanceText.Text = text;
            BalanceText.Foreground = color;
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Client client)) return;

            var wnd = new ClientFormWindow(_main, client);
            wnd.ShowDialog();

            // Refresh the parent container
            var parent = this.Parent;
            while (parent != null && !(parent is CMainC))
            {
                parent = LogicalTreeHelper.GetParent(parent);
            }

            if (parent is CMainC clientPage)
            {
                clientPage.LoadAllData();
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Client client)) return;

            var wnd = new DeleteClientWindow(_main, client);
            wnd.ShowDialog();

            // Refresh the parent container
            var parent = this.Parent;
            while (parent != null && !(parent is CMainC))
            {
                parent = LogicalTreeHelper.GetParent(parent);
            }

            if (parent is CMainC clientPage)
            {
                clientPage.LoadAllData();
            }
        }

        private async void Paid_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Client client)) return;

            var wnd = new PaidClientWindow(_currentUser, _main, client);
            wnd.ShowDialog();

            // Refresh the parent container
            var parent = this.Parent;
            while (parent != null && !(parent is CMainC))
            {
                parent = LogicalTreeHelper.GetParent(parent);
            }

            if (parent is CMainC clientPage)
            {
                clientPage.LoadAllData();
            }
        }

        private void Operations_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Client client)) return;
            var wnd = new ClientOperationsWindow(client, _currentUser);
            wnd.ShowDialog();
        }
    }
}