using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestionComerce;

namespace Superete.Main.Comptabilite.Views
{
    public partial class CompteDialog : Window
    {
        public PlanComptable CompteData { get; private set; }
        private bool isEditMode;

        public CompteDialog(PlanComptable compte)
        {
            InitializeComponent();

            isEditMode = compte != null;
            CompteData = compte ?? new PlanComptable { EstActif = true };

            LoadData();
        }

        private void LoadData()
        {
            if (isEditMode)
            {
                TitleText.Text = "Modifier Compte";
                TxtCodeCompte.Text = CompteData.CodeCompte;
                TxtCodeCompte.IsReadOnly = true;
                TxtCodeCompte.Background = System.Windows.Media.Brushes.LightGray;
            }

            TxtLibelle.Text = CompteData.Libelle;
            ChkActif.IsChecked = CompteData.EstActif;

            // Set Classe
            if (CompteData.Classe > 0 && CompteData.Classe <= 8)
            {
                CmbClasse.SelectedIndex = CompteData.Classe - 1;
            }

            // Set Type
            if (!string.IsNullOrEmpty(CompteData.SensNormal))
            {
                foreach (ComboBoxItem item in CmbSens.Items)
                {
                    // CHANGE THIS LINE:
                    if (item.Tag?.ToString() == CompteData.SensNormal)
                    {
                        CmbSens.SelectedItem = item;
                        break;
                    }
                }
            }

            // Set Sens
            if (!string.IsNullOrEmpty(CompteData.SensNormal))
            {
                foreach (ComboBoxItem item in CmbSens.Items)
                {
                    if (item.Content.ToString() == CompteData.SensNormal)
                    {
                        CmbSens.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TxtCodeCompte.Text))
            {
                MessageBox.Show("⚠️ Le code compte est obligatoire", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCodeCompte.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtLibelle.Text))
            {
                MessageBox.Show("⚠️ Le libellé est obligatoire", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtLibelle.Focus();
                return;
            }

            if (CmbClasse.SelectedItem == null)
            {
                MessageBox.Show("⚠️ La classe est obligatoire", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbClasse.Focus();
                return;
            }

            if (CmbType.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Le type de compte est obligatoire", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbType.Focus();
                return;
            }

            if (CmbSens.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Le sens normal est obligatoire", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbSens.Focus();
                return;
            }

            // Validate code format (should be numeric)
            if (!int.TryParse(TxtCodeCompte.Text, out _))
            {
                MessageBox.Show("⚠️ Le code compte doit être numérique", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCodeCompte.Focus();
                return;
            }

            // Save data
            CompteData.CodeCompte = TxtCodeCompte.Text.Trim();
            CompteData.Libelle = TxtLibelle.Text.Trim();
            CompteData.Classe = Convert.ToInt32(((ComboBoxItem)CmbClasse.SelectedItem).Tag);
            CompteData.TypeCompte = ((ComboBoxItem)CmbType.SelectedItem).Content.ToString();
            var sensItem = (ComboBoxItem)CmbSens.SelectedItem;
            CompteData.SensNormal = sensItem.Tag?.ToString() ?? sensItem.Content.ToString();
            CompteData.EstActif = ChkActif.IsChecked == true;

            DialogResult = true;
        }
    }
}