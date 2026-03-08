using GestionComerce;
using GestionComerce.Main.ClientPage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Main.ClientPage
{
    public partial class CMainC : UserControl
    {
        private List<Client> _allClients = new List<Client>();
        private List<Credit> _credits = new List<Credit>();
        private MainWindow _main;
        private User _currentUser;

        public CMainC(User u, MainWindow main)
        {
            InitializeComponent();
            _main = main;
            _currentUser = u;
            Loaded += ClientWindow_Loaded;
        }

        private void ClientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Role r in _main.lr)
            {
                if (_currentUser.RoleID == r.RoleID)
                {
                    if (r.ViewClient)
                    {
                        LoadAllData();
                    }
                    if (!r.CreateClient)
                    {
                        AddBtn.IsEnabled = false;
                    }
                }
            }
        }

        public void LoadAllData()
        {
            try
            {
                // Load from MainWindow lists (already loaded in memory)
                _allClients = _main.lc ?? new List<Client>();
                _credits = _main.credits ?? new List<Credit>();

                System.Diagnostics.Debug.WriteLine($"Clients from list: {_allClients.Count}, Credits from list: {_credits.Count}");

                RefreshClientDisplay();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error in LoadAllData: {ex}");
            }
        }

        private void RefreshClientDisplay()
        {
            foreach (Role r in _main.lr)
            {
                if (_currentUser.RoleID == r.RoleID)
                {
                    if (r.ViewClient)
                    {
                        try
                        {
                            ClientsContainer.Children.Clear();

                            if (_allClients == null || _allClients.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("No clients to display");
                                return;
                            }

                            var activeClients = _allClients.Where(c => c.Etat).ToList();
                            System.Diagnostics.Debug.WriteLine($"Active clients: {activeClients.Count}");

                            foreach (var client in activeClients)
                            {
                                var clientRow = CreateClientRow(client);
                                ClientsContainer.Children.Add(clientRow);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error refreshing client display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            System.Diagnostics.Debug.WriteLine($"Error in RefreshClientDisplay: {ex}");
                        }
                    }
                }
            }
        }

        private UserControl CreateClientRow(Client client)
        {
            var clientRow = new SingleRowClient(_main, _currentUser);
            clientRow.DataContext = client;
            return clientRow;
        }

        private void UpdateStatistics()
        {
            try
            {
                var activeClients = _allClients?.Where(c => c.Etat).ToList() ?? new List<Client>();

                // Filter only CLIENT credits (where ClientID is not null)
                var clientCredits = _credits?.Where(c => c.Etat && c.ClientID.HasValue).ToList() ?? new List<Credit>();

                int totalClients = activeClients.Count;
                decimal totalCredit = clientCredits.Sum(c => c.Total);
                decimal totalPaid = clientCredits.Sum(c => c.Paye);
                decimal pending = clientCredits.Sum(c => c.Difference);

                if (TotalClientText != null)
                    TotalClientText.Text = totalClients.ToString();

                if (TotalCreditText != null)
                    TotalCreditText.Text = $"{totalCredit:N2} DH";

                if (PaidThisMonthText != null)
                    PaidThisMonthText.Text = $"{totalPaid:N2} DH";

                if (PendingText != null)
                    PendingText.Text = $"{pending:N2} DH";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_allClients == null) return;

                string query = (SearchTextBox?.Text ?? "").Trim().ToLowerInvariant();

                ClientsContainer.Children.Clear();

                List<Client> filteredClients;

                if (string.IsNullOrEmpty(query))
                {
                    filteredClients = _allClients.Where(c => c.Etat).ToList();
                }
                else
                {
                    filteredClients = _allClients.Where(c => c.Etat &&
                        ((!string.IsNullOrEmpty(c.Nom) && c.Nom.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(c.Telephone) && c.Telephone.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(c.ICE) && c.ICE.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(c.Adresse) && c.Adresse.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(c.SiegeEntreprise) && c.SiegeEntreprise.ToLowerInvariant().Contains(query)))
                    ).ToList();
                }

                foreach (var client in filteredClients)
                {
                    var clientRow = CreateClientRow(client);
                    ClientsContainer.Children.Add(clientRow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in search: {ex.Message}");
            }
        }

        public void ReloadClients()
        {
            LoadAllData();
        }

        private async void AddNewClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wnd = new ClientFormWindow(_main, null);
                wnd.ShowDialog();

                // Reload data after the window closes
                LoadAllData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening client form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Example: call a method on MainWindow to go back
            _main.load_main(_currentUser);
        }
    }
}