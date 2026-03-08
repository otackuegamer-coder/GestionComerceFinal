using Microsoft.Win32;
using Superete.Main.Comptabilite.Models;
using Superete.Main.Comptabilite.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Superete.Main.Comptabilite
{
    public partial class HistoriquePaiementWindow : Window
    {
        private readonly EmployeModel employe;
        private readonly SalaireService salaireService;
        private List<SalaireModel> allSalaires;
        private List<SalaireModel> filteredSalaires;

        private const string CONNECTION_STRING = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";

        // Translation
        private string _langue = "Français";
        private string T_Fermer, T_Exporter, T_AllStatuts;
        private string T_AllYears, T_Records, T_TotalAnnuel, T_TotalNet;
        private string T_ColPeriode, T_ColDate, T_ColBase, T_ColHS;
        private string T_ColPrimes, T_ColBrut, T_ColRetenues, T_ColNet, T_ColStatut;
        private string T_NoData, T_ExportTitle, T_ExportSuccess, T_ExportError;

        public HistoriquePaiementWindow(EmployeModel employe)
        {
            InitializeComponent();
            this.employe = employe;
            salaireService = new SalaireService(CONNECTION_STRING);

            _langue = "Français";
            if (Application.Current is GestionComerce.App app)
                _langue = app.CurrentLanguage ?? "Français";

            ApplyLanguage();
            LoadData();
        }

        private void ApplyLanguage()
        {
            bool ar = _langue == "العربية" || _langue == "Arabic";
            bool en = _langue == "English";

            this.FlowDirection = ar ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            if (ar)
            {
                T_Fermer = "إغلاق";
                T_Exporter = "تصدير Excel";
                T_AllStatuts = "كل الحالات";
                T_AllYears = "كل السنوات";
                T_Records = "سجل";
                T_TotalAnnuel = "إجمالي السنة:";
                T_TotalNet = "صافي الرواتب:";
                T_ColPeriode = "الفترة";
                T_ColDate = "تاريخ الدفع";
                T_ColBase = "الراتب الأساسي";
                T_ColHS = "ساعات إضافية";
                T_ColPrimes = "المكافآت";
                T_ColBrut = "الراتب الإجمالي";
                T_ColRetenues = "الاستقطاعات";
                T_ColNet = "صافي الراتب";
                T_ColStatut = "الحالة";
                T_NoData = "لا توجد سجلات دفع.";
                T_ExportTitle = "تصدير إلى Excel";
                T_ExportSuccess = "تم تصدير الملف بنجاح!";
                T_ExportError = "خطأ في التصدير:";
            }
            else if (en)
            {
                T_Fermer = "Close";
                T_Exporter = "Export Excel";
                T_AllStatuts = "All Statuses";
                T_AllYears = "All Years";
                T_Records = "record(s)";
                T_TotalAnnuel = "Year total:";
                T_TotalNet = "Net salary total:";
                T_ColPeriode = "Period";
                T_ColDate = "Payment Date";
                T_ColBase = "Base Salary";
                T_ColHS = "Overtime";
                T_ColPrimes = "Bonuses";
                T_ColBrut = "Gross Salary";
                T_ColRetenues = "Deductions";
                T_ColNet = "Net Salary";
                T_ColStatut = "Status";
                T_NoData = "No payment records found.";
                T_ExportTitle = "Export to Excel";
                T_ExportSuccess = "File exported successfully!";
                T_ExportError = "Export error:";
            }
            else
            {
                T_Fermer = "Fermer";
                T_Exporter = "Exporter Excel";
                T_AllStatuts = "Tous les statuts";
                T_AllYears = "Toutes les années";
                T_Records = "enregistrement(s)";
                T_TotalAnnuel = "Total année:";
                T_TotalNet = "Total salaires nets:";
                T_ColPeriode = "Période";
                T_ColDate = "Date Paiement";
                T_ColBase = "Salaire Base";
                T_ColHS = "H. Supp.";
                T_ColPrimes = "Primes";
                T_ColBrut = "Salaire Brut";
                T_ColRetenues = "Retenues";
                T_ColNet = "Salaire Net";
                T_ColStatut = "Statut";
                T_NoData = "Aucun historique de paiement trouvé.";
                T_ExportTitle = "Exporter vers Excel";
                T_ExportSuccess = "Fichier exporté avec succès !";
                T_ExportError = "Erreur d'export :";
            }

            // Apply to controls
            txtWindowTitle.Text = employe?.NomComplet ?? "";
            txtSubtitle.Text = $"CIN: {employe?.CIN ?? "—"}   |   {employe?.Poste ?? "—"}";
            btnFermerLabel.Text = T_Fermer;

            // Column headers
            colPeriode.Header = T_ColPeriode;
            colDatePmt.Header = T_ColDate;
            colBase.Header = T_ColBase;
            colHS.Header = T_ColHS;
            colPrimes.Header = T_ColPrimes;
            colBrut.Header = T_ColBrut;
            colRetenues.Header = T_ColRetenues;
            colNet.Header = T_ColNet;
            colStatut.Header = T_ColStatut;

            // Export button label (inside template — use Tag trick)
            btnExporter.Tag = T_Exporter;
            if (btnExporter.Template.FindName("btnExportLabel", btnExporter) is TextBlock lbl)
                lbl.Text = T_Exporter;
        }

        private void LoadData()
        {
            try
            {
                allSalaires = salaireService.GetByEmploye(employe.EmployeID);

                // Populate year filter
                cmbFiltreAnnee.Items.Clear();
                cmbFiltreAnnee.Items.Add(new ComboBoxItem { Content = T_AllYears, Tag = 0 });
                var years = allSalaires.Select(s => s.Annee).Distinct().OrderByDescending(y => y);
                foreach (var y in years)
                    cmbFiltreAnnee.Items.Add(new ComboBoxItem { Content = y.ToString(), Tag = y });
                cmbFiltreAnnee.SelectedIndex = 0;

                // Populate status filter
                cmbFiltreStatut.Items.Clear();
                cmbFiltreStatut.Items.Add(new ComboBoxItem { Content = T_AllStatuts, Tag = "" });
                cmbFiltreStatut.Items.Add(new ComboBoxItem { Content = "Payé", Tag = "Payé" });
                cmbFiltreStatut.Items.Add(new ComboBoxItem { Content = "En Attente", Tag = "En Attente" });
                cmbFiltreStatut.Items.Add(new ComboBoxItem { Content = "Validé", Tag = "Validé" });
                cmbFiltreStatut.SelectedIndex = 0;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            filteredSalaires = allSalaires.ToList();

            // Status filter
            if (cmbFiltreStatut.SelectedItem is ComboBoxItem statItem && statItem.Tag?.ToString() != "")
                filteredSalaires = filteredSalaires.Where(s => s.Statut == statItem.Tag?.ToString()).ToList();

            // Year filter
            if (cmbFiltreAnnee.SelectedItem is ComboBoxItem yearItem && (int)yearItem.Tag != 0)
                filteredSalaires = filteredSalaires.Where(s => s.Annee == (int)yearItem.Tag).ToList();

            dgHistorique.ItemsSource = filteredSalaires;

            // Update footer totals
            decimal totalBrut = filteredSalaires.Sum(s => s.SalaireBrut);
            decimal totalNet = filteredSalaires.Sum(s => s.SalaireNet);
            txtTotalRecords.Text = $"{filteredSalaires.Count} {T_Records}";
            txtTotalAnnuel.Text = $"{T_TotalAnnuel} {totalBrut:N2} DH";
            txtTotalNet.Text = $"{T_TotalNet} {totalNet:N2} DH";
        }

        private void FiltreStatut_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void FiltreAnnee_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void BtnFermer_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnExporter_Click(object sender, RoutedEventArgs e)
        {
            if (filteredSalaires == null || filteredSalaires.Count == 0)
            {
                MessageBox.Show(T_NoData, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = T_ExportTitle,
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"Salaires_{employe.NomComplet.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv",
                DefaultExt = ".csv"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();

                // BOM for Excel UTF-8
                sb.Append('\uFEFF');

                // Header row
                sb.AppendLine(string.Join(";",
                    T_ColPeriode, T_ColDate, T_ColBase, T_ColHS,
                    T_ColPrimes, T_ColBrut, T_ColRetenues, T_ColNet, T_ColStatut,
                    "CNSS", "AMO", "CIMR", "IR", "Remarques"));

                // Data rows
                foreach (var s in filteredSalaires)
                {
                    decimal totalPrimes = s.PrimeAnciennete + s.PrimeRendement + s.PrimeResponsabilite
                                        + s.IndemniteTransport + s.IndemniteLogement + s.AutresPrimes;
                    sb.AppendLine(string.Join(";",
                        s.PeriodeDisplay,
                        s.DatePaiement?.ToString("dd/MM/yyyy") ?? "",
                        s.SalaireBase.ToString("N2"),
                        s.MontantHeuresSupp.ToString("N2"),
                        totalPrimes.ToString("N2"),
                        s.SalaireBrut.ToString("N2"),
                        s.TotalRetenues.ToString("N2"),
                        s.SalaireNet.ToString("N2"),
                        s.Statut,
                        s.CotisationCNSS.ToString("N2"),
                        s.CotisationAMO.ToString("N2"),
                        s.CotisationCIMR.ToString("N2"),
                        s.MontantIR.ToString("N2"),
                        (s.Remarques ?? "").Replace(";", ",")));
                }

                // Summary row
                decimal sumBase = filteredSalaires.Sum(x => x.SalaireBase);
                decimal sumHS = filteredSalaires.Sum(x => x.MontantHeuresSupp);
                decimal sumPrimes = filteredSalaires.Sum(x => x.PrimeAnciennete + x.PrimeRendement + x.PrimeResponsabilite + x.IndemniteTransport + x.IndemniteLogement + x.AutresPrimes);
                decimal sumBrut = filteredSalaires.Sum(x => x.SalaireBrut);
                decimal sumRetenues = filteredSalaires.Sum(x => x.TotalRetenues);
                decimal sumNet = filteredSalaires.Sum(x => x.SalaireNet);
                sb.AppendLine();
                sb.AppendLine(string.Join(";",
                    "TOTAL", "",
                    sumBase.ToString("N2"), sumHS.ToString("N2"),
                    sumPrimes.ToString("N2"), sumBrut.ToString("N2"),
                    sumRetenues.ToString("N2"), sumNet.ToString("N2"), "", "", "", "", "", ""));

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show(T_ExportSuccess, "✓", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{T_ExportError} {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}