using Superete.Main.Comptabilite.Models;
using Superete.Main.Comptabilite.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Superete.Main.Comptabilite.gestionemployes
{
    public partial class PayerSalaireWindow : Window
    {
        private readonly EmployeModel employe;
        private readonly SalaireService salaireService;
        private bool isCalculating = false;

        private const string CONNECTION_STRING = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";

        // ── Translation strings (set in ApplyLanguage) ──────────────────────
        private string T_Title, T_EmployeInfo;
        private string T_PeriodeSection, T_MoisLabel, T_AnneeLabel, T_DatePaiementLabel;
        private string T_SalaireBaseSection, T_SalaireBase, T_TauxHoraire, T_HeuresNormales;
        private string T_HeuresSuppSection, T_HS25, T_HS50, T_HS100, T_MontantHS;
        private string T_PrimesSection, T_PrimeAnc, T_PrimeRend, T_PrimeResp;
        private string T_IndemTransport, T_IndemLogement, T_AutresPrimes, T_TotalPrimesLabel;
        private string T_RetenuesSection, T_CNSS, T_AMO, T_CIMR, T_IR;
        private string T_Avance, T_Pret, T_Penalites, T_AutresRetenues, T_TotalRetenuesLabel;
        private string T_RemarquesLabel;
        private string T_BrutLabel, T_RetenuesSummaryLabel, T_NetLabel;
        private string T_BtnAnnuler, T_BtnAttente, T_BtnPayer;
        private string[] T_Months;
        // ────────────────────────────────────────────────────────────────────

        public PayerSalaireWindow(EmployeModel employe)
        {
            InitializeComponent();
            this.employe = employe;
            salaireService = new SalaireService(CONNECTION_STRING);

            // Detect language from running app
            string langue = "Français";
            if (Application.Current is GestionComerce.App app)
                langue = app.CurrentLanguage ?? "Français";

            ApplyLanguage(langue);
            InitializeDefaults();
        }

        // ── Build all UI strings for the given language ──────────────────────
        private void ApplyLanguage(string langue)
        {
            bool ar = langue == "العربية" || langue == "Arabic";
            bool en = langue == "English";

            if (ar)
            {
                this.FlowDirection = FlowDirection.RightToLeft;

                T_PeriodeSection = "📅  فترة الدفع";
                T_MoisLabel = "الشهر";
                T_AnneeLabel = "السنة";
                T_DatePaiementLabel = "تاريخ الدفع";
                T_SalaireBaseSection = "🏦  الراتب الأساسي";
                T_SalaireBase = "الراتب الأساسي (درهم)";
                T_TauxHoraire = "السعر بالساعة (درهم/س)";
                T_HeuresNormales = "الساعات العادية";
                T_HeuresSuppSection = "⏰  ساعات إضافية";
                T_HS25 = "ساعات إضافية 25%";
                T_HS50 = "ساعات إضافية 50%";
                T_HS100 = "ساعات إضافية 100%";
                T_MontantHS = "المبلغ المحسوب (درهم)";
                T_PrimesSection = "🎁  المكافآت والتعويضات";
                T_PrimeAnc = "علاوة الأقدمية (درهم)";
                T_PrimeRend = "علاوة المردودية (درهم)";
                T_PrimeResp = "علاوة المسؤولية (درهم)";
                T_IndemTransport = "تعويض النقل (درهم)";
                T_IndemLogement = "تعويض السكن (درهم)";
                T_AutresPrimes = "مكافآت أخرى (درهم)";
                T_TotalPrimesLabel = "مجموع المكافآت";
                T_RetenuesSection = "📉  الاستقطاعات";
                T_CNSS = "اشتراك CNSS (درهم)";
                T_AMO = "اشتراك AMO (درهم)";
                T_CIMR = "اشتراك CIMR (درهم)";
                T_IR = "ضريبة الدخل (درهم)";
                T_Avance = "سلفة على الراتب (درهم)";
                T_Pret = "قرض شخصي (درهم)";
                T_Penalites = "غرامات (درهم)";
                T_AutresRetenues = "استقطاعات أخرى (درهم)";
                T_TotalRetenuesLabel = "مجموع الاستقطاعات";
                T_RemarquesLabel = "📝  ملاحظات";
                T_BrutLabel = "الراتب الإجمالي";
                T_RetenuesSummaryLabel = "الاستقطاعات";
                T_NetLabel = "💵  صافي الراتب";
                T_BtnAnnuler = "إلغاء";
                T_BtnAttente = "💾  حفظ (قيد الانتظار)";
                T_BtnPayer = "✅  تحقق والدفع";
                T_Months = new[] { "يناير","فبراير","مارس","أبريل","مايو","يونيو",
                                               "يوليوز","غشت","شتنبر","أكتوبر","نونبر","دجنبر" };
            }
            else if (en)
            {
                this.FlowDirection = FlowDirection.LeftToRight;

                T_PeriodeSection = "📅  Payment Period";
                T_MoisLabel = "Month";
                T_AnneeLabel = "Year";
                T_DatePaiementLabel = "Payment Date";
                T_SalaireBaseSection = "🏦  Base Salary";
                T_SalaireBase = "Base Salary (DH)";
                T_TauxHoraire = "Hourly Rate (DH/h)";
                T_HeuresNormales = "Normal Hours";
                T_HeuresSuppSection = "⏰  Overtime Hours";
                T_HS25 = "Overtime 25%";
                T_HS50 = "Overtime 50%";
                T_HS100 = "Overtime 100%";
                T_MontantHS = "Calculated Amount (DH)";
                T_PrimesSection = "🎁  Bonuses & Allowances";
                T_PrimeAnc = "Seniority Bonus (DH)";
                T_PrimeRend = "Performance Bonus (DH)";
                T_PrimeResp = "Responsibility Bonus (DH)";
                T_IndemTransport = "Transport Allowance (DH)";
                T_IndemLogement = "Housing Allowance (DH)";
                T_AutresPrimes = "Other Bonuses (DH)";
                T_TotalPrimesLabel = "Total Bonuses";
                T_RetenuesSection = "📉  Deductions";
                T_CNSS = "CNSS Contribution (DH)";
                T_AMO = "AMO Contribution (DH)";
                T_CIMR = "CIMR Contribution (DH)";
                T_IR = "Income Tax (DH)";
                T_Avance = "Salary Advance (DH)";
                T_Pret = "Personal Loan (DH)";
                T_Penalites = "Penalties (DH)";
                T_AutresRetenues = "Other Deductions (DH)";
                T_TotalRetenuesLabel = "Total Deductions";
                T_RemarquesLabel = "📝  Remarks";
                T_BrutLabel = "Gross Salary";
                T_RetenuesSummaryLabel = "Total Deductions";
                T_NetLabel = "💵  Net Salary";
                T_BtnAnnuler = "Cancel";
                T_BtnAttente = "💾  Save (Pending)";
                T_BtnPayer = "✅  Validate & Pay";
                T_Months = new[] { "January","February","March","April","May","June",
                                               "July","August","September","October","November","December" };
            }
            else // Français (default)
            {
                this.FlowDirection = FlowDirection.LeftToRight;

                T_PeriodeSection = "📅  Période de Paiement";
                T_MoisLabel = "Mois";
                T_AnneeLabel = "Année";
                T_DatePaiementLabel = "Date de Paiement";
                T_SalaireBaseSection = "🏦  Salaire de Base";
                T_SalaireBase = "Salaire de Base (DH)";
                T_TauxHoraire = "Taux Horaire (DH/h)";
                T_HeuresNormales = "Heures Normales";
                T_HeuresSuppSection = "⏰  Heures Supplémentaires";
                T_HS25 = "H. Supp. 25%";
                T_HS50 = "H. Supp. 50%";
                T_HS100 = "H. Supp. 100%";
                T_MontantHS = "Montant Calculé (DH)";
                T_PrimesSection = "🎁  Primes & Indemnités";
                T_PrimeAnc = "Prime d'Ancienneté (DH)";
                T_PrimeRend = "Prime de Rendement (DH)";
                T_PrimeResp = "Prime de Responsabilité (DH)";
                T_IndemTransport = "Indemnité Transport (DH)";
                T_IndemLogement = "Indemnité Logement (DH)";
                T_AutresPrimes = "Autres Primes (DH)";
                T_TotalPrimesLabel = "Total Primes";
                T_RetenuesSection = "📉  Retenues & Déductions";
                T_CNSS = "Cotisation CNSS (DH)";
                T_AMO = "Cotisation AMO (DH)";
                T_CIMR = "Cotisation CIMR (DH)";
                T_IR = "Montant IR (DH)";
                T_Avance = "Avance sur Salaire (DH)";
                T_Pret = "Prêt Personnel (DH)";
                T_Penalites = "Pénalités (DH)";
                T_AutresRetenues = "Autres Retenues (DH)";
                T_TotalRetenuesLabel = "Total Retenues";
                T_RemarquesLabel = "📝  Remarques";
                T_BrutLabel = "Salaire Brut";
                T_RetenuesSummaryLabel = "Total Retenues";
                T_NetLabel = "💵  Salaire Net";
                T_BtnAnnuler = "Annuler";
                T_BtnAttente = "💾  Enregistrer (En Attente)";
                T_BtnPayer = "✅  Valider & Payer";
                T_Months = new[] { "Janvier","Février","Mars","Avril","Mai","Juin",
                                               "Juillet","Août","Septembre","Octobre","Novembre","Décembre" };
            }

            // Apply to controls (if already initialized)
            ApplyStringsToControls();
        }

        private void ApplyStringsToControls()
        {
            // Section labels
            if (lblPeriode != null) lblPeriode.Text = T_PeriodeSection;
            if (lblMois != null) lblMois.Text = T_MoisLabel;
            if (lblAnnee != null) lblAnnee.Text = T_AnneeLabel;
            if (lblDatePaiement != null) lblDatePaiement.Text = T_DatePaiementLabel;
            if (lblSalaireBaseSection != null) lblSalaireBaseSection.Text = T_SalaireBaseSection;
            if (lblSalaireBase != null) lblSalaireBase.Text = T_SalaireBase;
            if (lblTauxHoraire != null) lblTauxHoraire.Text = T_TauxHoraire;
            if (lblHeuresNormales != null) lblHeuresNormales.Text = T_HeuresNormales;
            if (lblHeuresSupp != null) lblHeuresSupp.Text = T_HeuresSuppSection;
            if (lblHS25 != null) lblHS25.Text = T_HS25;
            if (lblHS50 != null) lblHS50.Text = T_HS50;
            if (lblHS100 != null) lblHS100.Text = T_HS100;
            if (lblMontantHS != null) lblMontantHS.Text = T_MontantHS;
            if (lblPrimesSection != null) lblPrimesSection.Text = T_PrimesSection;
            if (lblPrimeAnc != null) lblPrimeAnc.Text = T_PrimeAnc;
            if (lblPrimeRend != null) lblPrimeRend.Text = T_PrimeRend;
            if (lblPrimeResp != null) lblPrimeResp.Text = T_PrimeResp;
            if (lblIndemTransport != null) lblIndemTransport.Text = T_IndemTransport;
            if (lblIndemLogement != null) lblIndemLogement.Text = T_IndemLogement;
            if (lblAutresPrimes != null) lblAutresPrimes.Text = T_AutresPrimes;
            if (lblTotalPrimes != null) lblTotalPrimes.Text = T_TotalPrimesLabel;
            if (lblRetenuesSection != null) lblRetenuesSection.Text = T_RetenuesSection;
            if (lblCNSS != null) lblCNSS.Text = T_CNSS;
            if (lblAMO != null) lblAMO.Text = T_AMO;
            if (lblCIMR != null) lblCIMR.Text = T_CIMR;
            if (lblIR != null) lblIR.Text = T_IR;
            if (lblAvance != null) lblAvance.Text = T_Avance;
            if (lblPret != null) lblPret.Text = T_Pret;
            if (lblPenalites != null) lblPenalites.Text = T_Penalites;
            if (lblAutresRetenues != null) lblAutresRetenues.Text = T_AutresRetenues;
            if (lblTotalRetenues != null) lblTotalRetenues.Text = T_TotalRetenuesLabel;
            if (lblRemarques != null) lblRemarques.Text = T_RemarquesLabel;
            if (lblSalaireBrut != null) lblSalaireBrut.Text = T_BrutLabel;
            if (lblTotalRetenuesSummary != null) lblTotalRetenuesSummary.Text = T_RetenuesSummaryLabel;
            if (lblSalaireNet != null) lblSalaireNet.Text = T_NetLabel;
            if (btnAnnuler != null) btnAnnuler.Content = T_BtnAnnuler;
            if (btnEnregistrerAttente != null) btnEnregistrerAttente.Content = T_BtnAttente;
            if (btnValiderPayer != null) btnValiderPayer.Content = T_BtnPayer;

            // Month ComboBox
            if (cmbMois != null && T_Months != null)
            {
                int prevIndex = cmbMois.SelectedIndex;
                cmbMois.Items.Clear();
                for (int i = 0; i < T_Months.Length; i++)
                {
                    cmbMois.Items.Add(new ComboBoxItem { Content = $"{i + 1:00} - {T_Months[i]}" });
                }
                cmbMois.SelectedIndex = prevIndex >= 0 ? prevIndex : DateTime.Now.Month - 1;
            }
        }

        private void InitializeDefaults()
        {
            txtWindowTitle.Text = employe.NomComplet;
            txtEmployeInfo.Text = $"CIN: {employe.CIN ?? "—"}   |   CNSS: {employe.CNSS ?? "—"}   |   {employe.Poste ?? "—"}";

            ApplyStringsToControls();

            // Block all TextChanged events while filling fields
            isCalculating = true;

            cmbMois.SelectedIndex = DateTime.Now.Month - 1;
            txtAnnee.Text = DateTime.Now.Year.ToString();
            dpDatePaiement.SelectedDate = DateTime.Today;

            // ── Use plain ToString("F2") — no thousands separator, safe to parse back ──
            txtSalaireBase.Text = employe.SalaireBase > 0
                                            ? employe.SalaireBase.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                                            : "0";
            txtTauxHoraire.Text = "0";
            txtHeuresNormales.Text = "191";

            txtHeuresSupp25.Text = "0";
            txtHeuresSupp50.Text = "0";
            txtHeuresSupp100.Text = "0";

            txtPrimeAnciennete.Text = "0";
            txtPrimeRendement.Text = "0";
            txtPrimeResponsabilite.Text = "0";
            txtIndemniteTransport.Text = "0";
            txtIndemniteLogement.Text = "0";
            txtAutresPrimes.Text = "0";

            txtCotisationCNSS.Text = "0";
            txtCotisationAMO.Text = "0";
            txtCotisationCIMR.Text = "0";
            txtMontantIR.Text = "0";
            txtAvanceSurSalaire.Text = "0";
            txtPretPersonnel.Text = "0";
            txtPenalites.Text = "0";
            txtAutresRetenues.Text = "0";

            // All fields set — now calculate once cleanly
            isCalculating = false;
            RecalculateAll();
        }

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0m;

            // Strip thousands separators (space, nbsp, comma) then normalize decimal point
            string cleaned = text
                .Replace("\u00A0", "") // non-breaking space (fr-FR thousands sep)
                .Replace(" ", "")      // regular space
                .Replace(",", ".");    // French decimal comma → dot

            if (decimal.TryParse(cleaned,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal result))
                return result;
            return 0m;
        }

        private void OnAmountChanged(object sender, TextChangedEventArgs e)
        {
            if (isCalculating) return;
            RecalculateAll();
        }

        private void RecalculateAll()
        {
            isCalculating = true;
            try
            {
                // ── Base salary ─────────────────────────────────────────────
                decimal salaireBase = ParseDecimal(txtSalaireBase.Text);
                decimal tauxHoraire = ParseDecimal(txtTauxHoraire.Text);

                // ── Overtime ────────────────────────────────────────────────
                decimal h25 = ParseDecimal(txtHeuresSupp25.Text);
                decimal h50 = ParseDecimal(txtHeuresSupp50.Text);
                decimal h100 = ParseDecimal(txtHeuresSupp100.Text);
                decimal montantHS = (h25 * tauxHoraire * 1.25m)
                                  + (h50 * tauxHoraire * 1.50m)
                                  + (h100 * tauxHoraire * 2.00m);
                txtMontantHeuresSupp.Text = montantHS.ToString("N2");

                // ── Primes ──────────────────────────────────────────────────
                decimal primeAnc = ParseDecimal(txtPrimeAnciennete.Text);
                decimal primeRend = ParseDecimal(txtPrimeRendement.Text);
                decimal primeResp = ParseDecimal(txtPrimeResponsabilite.Text);
                decimal indemTransport = ParseDecimal(txtIndemniteTransport.Text);
                decimal indemLogement = ParseDecimal(txtIndemniteLogement.Text);
                decimal autresPrimes = ParseDecimal(txtAutresPrimes.Text);
                decimal totalPrimes = primeAnc + primeRend + primeResp
                                       + indemTransport + indemLogement + autresPrimes;
                txtTotalPrimes.Text = $"{totalPrimes:N2} DH";

                // ── Brut = Base + Overtime + Primes ─────────────────────────
                decimal salaireBrut = salaireBase + montantHS + totalPrimes;
                txtSalaireBrut.Text = $"{salaireBrut:N2} DH";

                // ── Retenues ────────────────────────────────────────────────
                decimal cnss = ParseDecimal(txtCotisationCNSS.Text);
                decimal amo = ParseDecimal(txtCotisationAMO.Text);
                decimal cimr = ParseDecimal(txtCotisationCIMR.Text);
                decimal ir = ParseDecimal(txtMontantIR.Text);
                decimal avance = ParseDecimal(txtAvanceSurSalaire.Text);
                decimal pret = ParseDecimal(txtPretPersonnel.Text);
                decimal penalites = ParseDecimal(txtPenalites.Text);
                decimal autres = ParseDecimal(txtAutresRetenues.Text);
                decimal totalRetenues = cnss + amo + cimr + ir + avance + pret + penalites + autres;
                txtTotalRetenues.Text = $"{totalRetenues:N2} DH";
                txtTotalRetenuesSummary.Text = $"{totalRetenues:N2} DH";

                // ── Net ─────────────────────────────────────────────────────
                decimal salaireNet = salaireBrut - totalRetenues;
                txtSalaireNet.Text = $"{salaireNet:N2} DH";
            }
            finally
            {
                isCalculating = false;
            }
        }

        private SalaireModel BuildSalaireModel(string statut)
        {
            int mois = cmbMois.SelectedIndex + 1;
            int annee = int.TryParse(txtAnnee.Text, out int yr) ? yr : DateTime.Now.Year;

            decimal salaireBase = ParseDecimal(txtSalaireBase.Text);
            decimal tauxHoraire = ParseDecimal(txtTauxHoraire.Text);
            decimal heuresNormales = ParseDecimal(txtHeuresNormales.Text);
            decimal h25 = ParseDecimal(txtHeuresSupp25.Text);
            decimal h50 = ParseDecimal(txtHeuresSupp50.Text);
            decimal h100 = ParseDecimal(txtHeuresSupp100.Text);
            decimal montantHS = ParseDecimal(txtMontantHeuresSupp.Text);

            decimal primeAnc = ParseDecimal(txtPrimeAnciennete.Text);
            decimal primeRend = ParseDecimal(txtPrimeRendement.Text);
            decimal primeResp = ParseDecimal(txtPrimeResponsabilite.Text);
            decimal indemTransport = ParseDecimal(txtIndemniteTransport.Text);
            decimal indemLogement = ParseDecimal(txtIndemniteLogement.Text);
            decimal autresPrimes = ParseDecimal(txtAutresPrimes.Text);
            decimal totalPrimes = primeAnc + primeRend + primeResp + indemTransport + indemLogement + autresPrimes;
            decimal salaireBrut = salaireBase + montantHS + totalPrimes;

            decimal cnss = ParseDecimal(txtCotisationCNSS.Text);
            decimal amo = ParseDecimal(txtCotisationAMO.Text);
            decimal cimr = ParseDecimal(txtCotisationCIMR.Text);
            decimal ir = ParseDecimal(txtMontantIR.Text);
            decimal avance = ParseDecimal(txtAvanceSurSalaire.Text);
            decimal pret = ParseDecimal(txtPretPersonnel.Text);
            decimal penalites = ParseDecimal(txtPenalites.Text);
            decimal autres = ParseDecimal(txtAutresRetenues.Text);
            decimal totalRetenues = cnss + amo + cimr + ir + avance + pret + penalites + autres;
            decimal salaireNet = salaireBrut - totalRetenues;

            return new SalaireModel
            {
                EmployeID = employe.EmployeID,
                NomComplet = employe.NomComplet,
                CIN = employe.CIN,
                CNSS = employe.CNSS,
                Mois = mois,
                Annee = annee,
                DatePaiement = statut == "Payé" ? dpDatePaiement.SelectedDate : null,
                SalaireBase = salaireBase,
                TauxHoraire = tauxHoraire,
                HeuresNormales = heuresNormales,
                HeuresSupp25 = h25,
                HeuresSupp50 = h50,
                HeuresSupp100 = h100,
                MontantHeuresSupp = montantHS,
                PrimeAnciennete = primeAnc,
                PrimeRendement = primeRend,
                PrimeResponsabilite = primeResp,
                IndemniteTransport = indemTransport,
                IndemniteLogement = indemLogement,
                AutresPrimes = autresPrimes,
                SalaireBrut = salaireBrut,
                CotisationCNSS = cnss,
                CotisationAMO = amo,
                CotisationCIMR = cimr,
                MontantIR = ir,
                AvanceSurSalaire = avance,
                PretPersonnel = pret,
                Penalites = penalites,
                AutresRetenues = autres,
                TotalRetenues = totalRetenues,
                SalaireNet = salaireNet,
                CotisationPatronaleCNSS = salaireBrut * 0.1048m,
                CotisationPatronaleAMO = salaireBrut * 0.0226m,
                Statut = statut,
                Remarques = txtRemarques.Text,
                CreePar = Environment.UserName
            };
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnEnregistrerAttente_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                salaireService.Create(BuildSalaireModel("En Attente"));
                MessageBox.Show(
                    T_BtnAttente.Contains("Save") ? "Salary saved as pending!" :
                    T_BtnAttente.Contains("حفظ") ? "تم الحفظ بنجاح!" :
                                                     "Salaire enregistré en attente avec succès!",
                    "✓", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnValiderPayer_Click(object sender, RoutedEventArgs e)
        {
            string confirmMsg = $"{employe.NomComplet}\n{T_NetLabel}: {txtSalaireNet.Text}";
            if (MessageBox.Show(confirmMsg, "?", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                salaireService.Create(BuildSalaireModel("Payé"));
                MessageBox.Show(
                    T_BtnPayer.Contains("Pay") ? "Salary paid and saved!" :
                    T_BtnPayer.Contains("تحقق") ? "تم الدفع بنجاح!" :
                                                     "Salaire payé et enregistré avec succès!",
                    "✓", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}