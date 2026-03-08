using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GestionComerce.Main.Delivery
{
    public partial class CLivraison : UserControl
    {
        private MainWindow main;
        private User u;
        private List<Livraison> livraisons;
        private LivraisonStatistiques stats;

        public CLivraison(MainWindow main, User u)
        {
            InitializeComponent();
            this.main = main;
            this.u = u;

            // Apply permissions before loading
            ApplyPermissions();

            Loaded += CLivraison_Loaded;
        }

        private void ApplyPermissions()
        {
            // Find user's role
            Role userRole = null;
            foreach (Role r in main.lr)
            {
                if (r.RoleID == u.RoleID)
                {
                    userRole = r;
                    break;
                }
            }

            if (userRole == null) return;

            // Check permissions
            bool hasCreationLivraison = userRole.CreationLivraison;
            bool hasGestionLivreur = userRole.GestionLivreur;

            // Apply to buttons
            if (!hasCreationLivraison)
            {
                BtnNouvelleLivraison.IsEnabled = false;
                SetButtonGrayedOut(BtnNouvelleLivraison);
            }

            if (!hasGestionLivreur)
            {
                BtnGererLivreurs.IsEnabled = false;
                SetButtonGrayedOut(BtnGererLivreurs);
            }
        }

        private void SetButtonGrayedOut(Button button)
        {
            if (button != null)
            {
                button.Opacity = 0.5;
                button.Cursor = Cursors.No;
            }
        }

        private async void CLivraison_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLivraisonsAsync();
        }

        // Charger toutes les livraisons
        private async Task LoadLivraisonsAsync(string statutFilter = null)
        {
            try
            {
                Livraison livraison = new Livraison();

                // Charger les livraisons avec filtre optionnel
                livraisons = await livraison.GetLivraisonsAsync(statutFilter);

                // Mettre à jour le DataGrid
                DgLivraisons.ItemsSource = null;
                DgLivraisons.ItemsSource = livraisons;

                // Charger les statistiques
                await LoadStatistiquesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des livraisons: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Charger les statistiques
        private async Task LoadStatistiquesAsync()
        {
            try
            {
                Livraison livraison = new Livraison();
                stats = await livraison.GetStatistiquesAsync();

                // Mettre à jour les TextBlocks
                TxtTotalLivraisons.Text = stats.TotalLivraisons.ToString();
                TxtEnAttente.Text = stats.EnAttente.ToString();
                TxtEnCours.Text = stats.EnCours.ToString();
                TxtLivrees.Text = stats.Livrees.ToString();
                TxtFraisTotal.Text = $"{stats.TotalFraisLivraison:N2} DH";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des statistiques: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Bouton Nouvelle Livraison
        private void BtnNouvelleLivraison_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnNouvelleLivraison.IsEnabled) return;

            // Ouvrir la fenêtre d'ajout de livraison
            LivraisonAddWindow addWindow = new LivraisonAddWindow(main, u);
            addWindow.LivraisonAdded += async (s, args) => await LoadLivraisonsAsync();
            addWindow.ShowDialog();
        }

        // Bouton Gérer Livreurs
        private void BtnGererLivreurs_Click(object sender, RoutedEventArgs e)
        {
            if (!BtnGererLivreurs.IsEnabled) return;

            // Ouvrir la fenêtre de gestion des livreurs
            LivreurManagementWindow livreurWindow = new LivreurManagementWindow(main, u);
            livreurWindow.ShowDialog();
        }

        // Bouton Retour
        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            main.load_main(u);
        }

        // Bouton Actualiser
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string filter = null;
            var selectedItem = CmbStatutFilter.SelectedItem as ComboBoxItem;

            if (selectedItem != null && selectedItem.Content.ToString() != "Tous les statuts")
            {
                filter = ConvertStatutToDbFormat(selectedItem.Content.ToString());
            }

            await LoadLivraisonsAsync(filter);
        }

        // Filtre par statut
        private async void CmbStatutFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbStatutFilter.SelectedItem == null) return;

            var selectedItem = CmbStatutFilter.SelectedItem as ComboBoxItem;
            string filter = null;

            if (selectedItem.Content.ToString() != "Tous les statuts")
            {
                filter = ConvertStatutToDbFormat(selectedItem.Content.ToString());
            }

            await LoadLivraisonsAsync(filter);
        }

        // Double-clic sur une ligne pour afficher les détails
        private void DgLivraisons_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgLivraisons.SelectedItem == null) return;

            Livraison selectedLivraison = (Livraison)DgLivraisons.SelectedItem;

            // Ouvrir la fenêtre de détails/modification
            LivraisonDetailsWindow detailsWindow = new LivraisonDetailsWindow(main, u, selectedLivraison);
            detailsWindow.LivraisonUpdated += async (s, args) => await LoadLivraisonsAsync();
            detailsWindow.ShowDialog();
        }

        // Convertir le texte du statut en format DB
        private string ConvertStatutToDbFormat(string statut)
        {
            switch (statut)
            {
                case "En attente":
                    return "en_attente";
                case "Confirmée":
                    return "confirmee";
                case "En préparation":
                    return "en_preparation";
                case "En cours":
                    return "en_cours";
                case "Livrée":
                    return "livree";
                case "Annulée":
                    return "annulee";
                default:
                    return null;
            }
        }
    }
}