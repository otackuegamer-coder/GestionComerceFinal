using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Main.Delivery
{
    public partial class LivreurManagementWindow : Window
    {
        private MainWindow main;
        private User u;
        private List<Livreur> livreurs;
        private Livreur selectedLivreur;
        private bool isEditMode = false;

        public LivreurManagementWindow(MainWindow main, User u)
        {
            InitializeComponent();
            this.main = main;
            this.u = u;

            Loaded += LivreurManagementWindow_Loaded;
        }

        private async void LivreurManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLivreursAsync();
        }

        // Charger tous les livreurs
        private async Task LoadLivreursAsync()
        {
            try
            {
                Livreur livreur = new Livreur();
                livreurs = await livreur.GetLivreursAsync(actifSeulement: true);

                DgLivreurs.ItemsSource = null;
                DgLivreurs.ItemsSource = livreurs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des livreurs: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sélection d'un livreur dans le DataGrid
        private void DgLivreurs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgLivreurs.SelectedItem != null)
            {
                selectedLivreur = (Livreur)DgLivreurs.SelectedItem;
                BtnModifier.IsEnabled = true;
                BtnSupprimer.IsEnabled = true;
            }
            else
            {
                selectedLivreur = null;
                BtnModifier.IsEnabled = false;
                BtnSupprimer.IsEnabled = false;
            }
        }

        // Bouton Enregistrer (Ajouter ou Modifier)
        private async void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNom.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtPrenom.Text))
            {
                MessageBox.Show("Le prénom est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrenom.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtTelephone.Text))
            {
                MessageBox.Show("Le téléphone est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTelephone.Focus();
                return;
            }

            try
            {
                BtnEnregistrer.IsEnabled = false;
                BtnEnregistrer.Content = "⏳ Enregistrement...";

                if (isEditMode && selectedLivreur != null)
                {
                    // Mode Modification
                    selectedLivreur.Nom = TxtNom.Text.Trim();
                    selectedLivreur.Prenom = TxtPrenom.Text.Trim();
                    selectedLivreur.Telephone = TxtTelephone.Text.Trim();
                    selectedLivreur.Email = TxtEmail.Text.Trim();
                    selectedLivreur.VehiculeType = CmbVehiculeType.SelectedItem != null
                        ? ((ComboBoxItem)CmbVehiculeType.SelectedItem).Content.ToString()
                        : null;
                    selectedLivreur.VehiculeImmatriculation = TxtImmatriculation.Text.Trim();
                    selectedLivreur.Statut = CmbStatut.SelectedItem != null
                        ? ((ComboBoxItem)CmbStatut.SelectedItem).Tag.ToString()
                        : "disponible";
                    selectedLivreur.ZoneCouverture = TxtZoneCouverture.Text.Trim();

                    int result = await selectedLivreur.UpdateLivreurAsync();

                    if (result > 0)
                    {
                        MessageBox.Show("✅ Livreur modifié avec succès!", "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadLivreursAsync();
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la modification.", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Mode Ajout
                    Livreur newLivreur = new Livreur
                    {
                        Nom = TxtNom.Text.Trim(),
                        Prenom = TxtPrenom.Text.Trim(),
                        Telephone = TxtTelephone.Text.Trim(),
                        Email = TxtEmail.Text.Trim(),
                        VehiculeType = CmbVehiculeType.SelectedItem != null
                            ? ((ComboBoxItem)CmbVehiculeType.SelectedItem).Content.ToString()
                            : null,
                        VehiculeImmatriculation = TxtImmatriculation.Text.Trim(),
                        Statut = CmbStatut.SelectedItem != null
                            ? ((ComboBoxItem)CmbStatut.SelectedItem).Tag.ToString()
                            : "disponible",
                        ZoneCouverture = TxtZoneCouverture.Text.Trim(),
                        DateEmbauche = DateTime.Now,
                        Actif = true
                    };

                    int result = await newLivreur.InsertLivreurAsync();

                    if (result > 0)
                    {
                        MessageBox.Show("✅ Livreur ajouté avec succès!", "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadLivreursAsync();
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de l'ajout.", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnEnregistrer.IsEnabled = true;
                BtnEnregistrer.Content = "💾 Enregistrer";
            }
        }

        // Bouton Modifier
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLivreur == null)
            {
                MessageBox.Show("Veuillez sélectionner un livreur à modifier.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Passer en mode édition
            isEditMode = true;
            TxtFormTitle.Text = "✏️ Modifier Livreur";

            // Remplir les champs
            TxtNom.Text = selectedLivreur.Nom;
            TxtPrenom.Text = selectedLivreur.Prenom;
            TxtTelephone.Text = selectedLivreur.Telephone;
            TxtEmail.Text = selectedLivreur.Email ?? string.Empty;
            TxtImmatriculation.Text = selectedLivreur.VehiculeImmatriculation ?? string.Empty;
            TxtZoneCouverture.Text = selectedLivreur.ZoneCouverture ?? string.Empty;

            // Sélectionner le type de véhicule
            if (!string.IsNullOrEmpty(selectedLivreur.VehiculeType))
            {
                foreach (ComboBoxItem item in CmbVehiculeType.Items)
                {
                    if (item.Content.ToString() == selectedLivreur.VehiculeType)
                    {
                        CmbVehiculeType.SelectedItem = item;
                        break;
                    }
                }
            }

            // Sélectionner le statut
            foreach (ComboBoxItem item in CmbStatut.Items)
            {
                if (item.Tag.ToString() == selectedLivreur.Statut)
                {
                    CmbStatut.SelectedItem = item;
                    break;
                }
            }

            TxtNom.Focus();
        }

        // Bouton Supprimer
        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLivreur == null)
            {
                MessageBox.Show("Veuillez sélectionner un livreur à supprimer.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le livreur '{selectedLivreur.NomComplet}'?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int deleteResult = await selectedLivreur.DeleteLivreurAsync();

                    if (deleteResult > 0)
                    {
                        MessageBox.Show("✅ Livreur supprimé avec succès!", "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadLivreursAsync();
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la suppression.", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Bouton Nouveau
        private void BtnNouveau_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        // Bouton Actualiser
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadLivreursAsync();
        }

        // Vider le formulaire
        private void ClearForm()
        {
            isEditMode = false;
            selectedLivreur = null;
            TxtFormTitle.Text = "➕ Nouveau Livreur";

            TxtNom.Clear();
            TxtPrenom.Clear();
            TxtTelephone.Clear();
            TxtEmail.Clear();
            TxtImmatriculation.Clear();
            TxtZoneCouverture.Clear();

            CmbVehiculeType.SelectedIndex = -1;
            CmbStatut.SelectedIndex = 0;

            DgLivreurs.SelectedItem = null;
            BtnModifier.IsEnabled = false;
            BtnSupprimer.IsEnabled = false;

            TxtNom.Focus();
        }
    }
}