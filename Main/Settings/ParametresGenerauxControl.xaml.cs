using GestionComerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Superete.Main.Settings
{
    public partial class ParametresGenerauxControl : UserControl
    {
        /// <summary>
        /// Fired after a successful save so the host page can reload this
        /// control and pick up the new language strings immediately.
        /// </summary>
        public event Action LanguageSaved;

        private string _connectionString;
        private int _currentUserId;
        private ParametresGeneraux _parametresActuels;
        private List<PaymentMethod> _paymentMethods;

        public ParametresGenerauxControl()
        {
            InitializeComponent();
        }

        public ParametresGenerauxControl(int userId, string connectionString) : this()
        {
            _currentUserId = userId;
            _connectionString = connectionString;
            Loaded += ParametresGenerauxControl_Loaded;
            CbVueParDefaut.SelectionChanged += CbVueParDefaut_SelectionChanged;
        }

        private async void ParametresGenerauxControl_Loaded(object sender, RoutedEventArgs e)
        {
            await ChargerMethodesPaiement();
            ChargerParametres();
        }

        private async Task ChargerMethodesPaiement()
        {
            try
            {
                PaymentMethod pm = new PaymentMethod();
                _paymentMethods = await pm.GetPaymentMethodsAsync();

                CbMethodePaiementParDefaut.Items.Clear();

                foreach (var method in _paymentMethods)
                {
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = method.PaymentMethodName,
                        Tag = method.PaymentMethodID
                    };
                    CbMethodePaiementParDefaut.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des méthodes de paiement : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerParametres()
        {
            try
            {
                _parametresActuels = ParametresGeneraux.ObtenirOuCreerParametres(_currentUserId, _connectionString);

                if (_parametresActuels != null)
                {
                    bool needsUpdate = false;

                    if (string.IsNullOrEmpty(_parametresActuels.VueParDefaut) ||
                        (_parametresActuels.VueParDefaut != "Row" && _parametresActuels.VueParDefaut != "Cartes"))
                    {
                        _parametresActuels.VueParDefaut = "Row";
                        needsUpdate = true;
                    }

                    if (string.IsNullOrEmpty(_parametresActuels.TrierParDefaut))
                    {
                        _parametresActuels.TrierParDefaut = "Plus récent au plus ancien";
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                        _parametresActuels.MettreAJourParametres(_connectionString);
                }

                RemplirInterface();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemplirInterface()
        {
            if (_parametresActuels == null) return;

            switch (_parametresActuels.AfficherClavier)
            {
                case "Oui":   CbAfficherClavier.SelectedIndex = 0; break;
                case "Non":   CbAfficherClavier.SelectedIndex = 1; break;
                default:      CbAfficherClavier.SelectedIndex = 2; break;
            }

            switch (_parametresActuels.TailleIcones)
            {
                case "Grandes":  CbTailleIcones.SelectedIndex = 0; break;
                case "Moyennes": CbTailleIcones.SelectedIndex = 1; break;
                case "Petites":  CbTailleIcones.SelectedIndex = 2; break;
                default:         CbTailleIcones.SelectedIndex = 1; break;
            }

            switch (_parametresActuels.Langue)
            {
                case "English":  CbLangue.SelectedIndex = 1; break;
                case "العربية":
                case "Arabic":   CbLangue.SelectedIndex = 2; break;
                default:         CbLangue.SelectedIndex = 0; break; // Français
            }

            ChkMasquerEtiquettesVides.IsChecked         = _parametresActuels.MasquerEtiquettesVides;
            ChkSupprimerArticlesQuantiteZero.IsChecked  = _parametresActuels.SupprimerArticlesQuantiteZero;
            ChkImprimerFactureParDefaut.IsChecked        = _parametresActuels.ImprimerFactureParDefaut;
            ChkImprimerTicketParDefaut.IsChecked         = _parametresActuels.ImprimerTicketParDefaut;

            string vueParDefaut = string.IsNullOrEmpty(_parametresActuels.VueParDefaut) ? "Row" : _parametresActuels.VueParDefaut;
            CbVueParDefaut.SelectedIndex = vueParDefaut == "Cartes" ? 0 : 1;
            TailleIconesBorder.Visibility = (vueParDefaut == "Cartes") ? Visibility.Visible : Visibility.Collapsed;

            switch (_parametresActuels.TrierParDefaut)
            {
                case "Nom (A-Z)":                    CbTrierParDefaut.SelectedIndex = 0; break;
                case "Nom (Z-A)":                    CbTrierParDefaut.SelectedIndex = 1; break;
                case "Prix croissant":               CbTrierParDefaut.SelectedIndex = 2; break;
                case "Prix décroissant":             CbTrierParDefaut.SelectedIndex = 3; break;
                case "Quantité croissante":          CbTrierParDefaut.SelectedIndex = 4; break;
                case "Quantité décroissante":        CbTrierParDefaut.SelectedIndex = 5; break;
                case "Plus récent au plus ancien":   CbTrierParDefaut.SelectedIndex = 6; break;
                case "Plus ancien au plus récent":   CbTrierParDefaut.SelectedIndex = 7; break;
                default:                             CbTrierParDefaut.SelectedIndex = 0; break;
            }

            if (!string.IsNullOrEmpty(_parametresActuels.MethodePaiementParDefaut))
            {
                for (int i = 0; i < CbMethodePaiementParDefaut.Items.Count; i++)
                {
                    if (CbMethodePaiementParDefaut.Items[i] is ComboBoxItem item &&
                        item.Content.ToString() == _parametresActuels.MethodePaiementParDefaut)
                    {
                        CbMethodePaiementParDefaut.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void CbVueParDefaut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TailleIconesBorder == null) return;
            TailleIconesBorder.Visibility = (CbVueParDefaut.SelectedIndex == 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string afficherClavier = CbAfficherClavier.SelectedIndex == 0 ? "Oui" :
                                         CbAfficherClavier.SelectedIndex == 1 ? "Non" : "Manuel";

                string vueParDefaut = CbVueParDefaut.SelectedIndex == 0 ? "Cartes" : "Row";

                string trierParDefaut = "";
                switch (CbTrierParDefaut.SelectedIndex)
                {
                    case 0: trierParDefaut = "Nom (A-Z)"; break;
                    case 1: trierParDefaut = "Nom (Z-A)"; break;
                    case 2: trierParDefaut = "Prix croissant"; break;
                    case 3: trierParDefaut = "Prix décroissant"; break;
                    case 4: trierParDefaut = "Quantité croissante"; break;
                    case 5: trierParDefaut = "Quantité décroissante"; break;
                    case 6: trierParDefaut = "Plus récent au plus ancien"; break;
                    case 7: trierParDefaut = "Plus ancien au plus récent"; break;
                    default: trierParDefaut = "Nom (A-Z)"; break;
                }

                string methodePaiement = "";
                if (CbMethodePaiementParDefaut.SelectedItem is ComboBoxItem selectedPayment)
                    methodePaiement = selectedPayment.Content.ToString();

                string tailleIcones = "";
                switch (CbTailleIcones.SelectedIndex)
                {
                    case 0: tailleIcones = "Grandes"; break;
                    case 1: tailleIcones = "Moyennes"; break;
                    case 2: tailleIcones = "Petites"; break;
                    default: tailleIcones = "Moyennes"; break;
                }

                // ── Get selected language name ──────────────────────────────
                string langue = "Français";
                if (CbLangue.SelectedItem is ComboBoxItem langItem)
                {
                    // Map combobox index to the stored language name
                    switch (CbLangue.SelectedIndex)
                    {
                        case 1: langue = "English"; break;
                        case 2: langue = "العربية"; break;
                        default: langue = "Français"; break;
                    }
                }
                // ───────────────────────────────────────────────────────────

                _parametresActuels.AfficherClavier              = afficherClavier;
                _parametresActuels.MasquerEtiquettesVides        = ChkMasquerEtiquettesVides.IsChecked ?? false;
                _parametresActuels.SupprimerArticlesQuantiteZero = ChkSupprimerArticlesQuantiteZero.IsChecked ?? false;
                _parametresActuels.ImprimerFactureParDefaut      = ChkImprimerFactureParDefaut.IsChecked ?? false;
                _parametresActuels.ImprimerTicketParDefaut       = ChkImprimerTicketParDefaut.IsChecked ?? false;
                _parametresActuels.MethodePaiementParDefaut      = methodePaiement;
                _parametresActuels.VueParDefaut                  = vueParDefaut;
                _parametresActuels.TrierParDefaut                = trierParDefaut;
                _parametresActuels.TailleIcones                  = tailleIcones;
                _parametresActuels.Langue                        = langue;

                bool success = _parametresActuels.MettreAJourParametres(_connectionString);

                if (success)
                {
                    // ── APPLY LANGUAGE IMMEDIATELY ACROSS THE WHOLE APP ─────
                    var app = Application.Current as GestionComerce.App;
                    if (app != null)
                        app.ApplyLanguage(langue);
                    // ───────────────────────────────────────────────────────

                    MessageBox.Show("Paramètres enregistrés avec succès !",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ── Notify the host page to reload this control so the
                    //    new language strings (x:Static) take effect instantly ──
                    LanguageSaved?.Invoke();
                }
                else
                {
                    MessageBox.Show("Erreur lors de l'enregistrement des paramètres.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement des paramètres : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            ChargerParametres();
        }

        public ParametresGeneraux ObtenirParametres() => _parametresActuels;

        private void CbLangue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No preview change here — language only applies when user clicks Save
        }

        public int? ObtenirPaymentMethodIdSelectionne()
        {
            if (CbMethodePaiementParDefaut.SelectedItem is ComboBoxItem selectedItem)
                return (int)selectedItem.Tag;
            return null;
        }
    }
}
