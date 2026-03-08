using GestionComerce;
using GestionComerce;
using GestionComerce.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Superete.Main.Comptabilite.Views
{
    /// <summary>
    /// Interaction logic for CPCView.xaml
    /// </summary>
    public partial class CPCView : UserControl
    {
        private readonly User _user;
        private readonly ComptabiliteService _service;
        private CPCDetailDTO _currentData;

        public CPCView(User user)
        {
            InitializeComponent();
            _user = user;
            _service = new ComptabiliteService();

            // Default: current year
            DateDebutPicker.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1);
            DateFinPicker.SelectedDate = DateTime.Now;

            LoadData();
        }

        // ─────────────────────────────────────────────────────────────
        //  DATA LOADING
        // ─────────────────────────────────────────────────────────────

        private void LoadData()
        {
            try
            {
                DateTime debut = DateDebutPicker.SelectedDate ?? new DateTime(DateTime.Now.Year, 1, 1);
                DateTime fin = DateFinPicker.SelectedDate ?? DateTime.Now;

                if (debut > fin)
                {
                    MessageBox.Show("La date de début doit être antérieure à la date de fin.",
                                    "Erreur de dates", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoadingIndicator.Visibility = Visibility.Visible;
                TxtPeriodLabel.Text = string.Format("Période : {0:dd/MM/yyyy}  →  {1:dd/MM/yyyy}", debut, fin);

                // Run on background thread
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var data = _service.GetCPCData(debut, fin);

                        Dispatcher.Invoke(() =>
                        {
                            _currentData = data;
                            BindUI(data);
                            LoadingIndicator.Visibility = Visibility.Collapsed;
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoadingIndicator.Visibility = Visibility.Collapsed;
                            MessageBox.Show("Erreur lors du chargement du CPC :\n" + ex.Message,
                                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                MessageBox.Show("Erreur : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BindUI(CPCDetailDTO data)
        {
            // ── Produits ──
            ProduitsExploitationList.ItemsSource = ToViewModels(data.ProduitsExploitation);
            ProduitsFinanciersList.ItemsSource = ToViewModels(data.ProduitsFinanciers);
            ProduitsNonCourantsList.ItemsSource = ToViewModels(data.ProduitsNonCourants);

            TxtTotalProduitsExpl.Text = FormatAmount(data.TotalProduitsExploitation);
            TxtTotalProduitsFinancier.Text = FormatAmount(data.TotalProduitsFinanciers);
            TxtTotalProduitsNonCourant.Text = FormatAmount(data.TotalProduitsNonCourants);

            // ── Charges ──
            ChargesExploitationList.ItemsSource = ToViewModels(data.ChargesExploitation);
            ChargesFinancieresList.ItemsSource = ToViewModels(data.ChargesFinancieres);
            ChargesNonCourantesList.ItemsSource = ToViewModels(data.ChargesNonCourantes);

            TxtTotalChargesExpl.Text = FormatAmount(data.TotalChargesExploitation);
            TxtTotalChargesFinancier.Text = FormatAmount(data.TotalChargesFinancieres);
            TxtTotalChargesNonCourant.Text = FormatAmount(data.TotalChargesNonCourantes);

            // ── Sub-results ──
            decimal resultExpl = data.TotalProduitsExploitation - data.TotalChargesExploitation;
            decimal resultFinancier = data.TotalProduitsFinanciers - data.TotalChargesFinancieres;
            decimal resultNonCourant = data.TotalProduitsNonCourants - data.TotalChargesNonCourantes;

            TxtResultatExploitation.Text = FormatResultAmount(resultExpl);
            TxtResultatFinancier.Text = FormatResultAmount(resultFinancier);
            TxtResultatNonCourant.Text = FormatResultAmount(resultNonCourant);

            // Sub-result colour
            TxtResultatExploitation.Foreground = resultExpl >= 0 ? Brushes.LimeGreen : Brushes.OrangeRed;
            TxtResultatFinancier.Foreground = resultFinancier >= 0 ? Brushes.DeepSkyBlue : Brushes.OrangeRed;
            TxtResultatNonCourant.Foreground = resultNonCourant >= 0 ? Brushes.Orchid : Brushes.OrangeRed;

            // ── Net result ──
            decimal resultNet = data.ResultatNet;
            TxtResultatNet.Text = FormatResultAmount(resultNet);

            bool isProfit = resultNet >= 0;
            TxtResultatNetLabel.Text = isProfit ? "BÉNÉFICE" : "PERTE";

            // Card colour: green for profit, red for loss
            ResultatNetCard.Background = new LinearGradientBrush(
                isProfit
                    ? new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(16,  185, 129), 0),
                            new GradientStop(Color.FromRgb(5,   150, 105), 1)
                        }
                    : new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(239, 68,  68), 0),
                            new GradientStop(Color.FromRgb(185, 28,  28), 1)
                        },
                new Point(0, 0), new Point(1, 1));

            ResultatFooter.Background = isProfit
                ? new SolidColorBrush(Color.FromRgb(240, 253, 244))
                : new SolidColorBrush(Color.FromRgb(254, 242, 242));
        }

        // ─────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────

        private IEnumerable<CPCLigneViewModel> ToViewModels(List<CPCLigneDTO> lignes)
        {
            return (lignes ?? new List<CPCLigneDTO>())
                   .Select(l => new CPCLigneViewModel(l));
        }

        private string FormatAmount(decimal amount)
            => string.Format("{0:N2} MAD", amount);

        private string FormatResultAmount(decimal amount)
            => string.Format("{0}{1:N2} MAD", amount < 0 ? "- " : "", Math.Abs(amount));

        // ─────────────────────────────────────────────────────────────
        //  BUTTON EVENTS
        // ─────────────────────────────────────────────────────────────

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
            => LoadData();

       // Add these using statements at the top


// Replace the BtnPrint_Click method with this:
private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentData == null)
            {
                MessageBox.Show("Aucune donnée à imprimer. Veuillez d'abord générer le CPC.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument();
                doc.PagePadding = new Thickness(50);
                doc.ColumnWidth = double.PositiveInfinity;
                doc.FontFamily = new FontFamily("Arial");

                // Title
                Paragraph titlePara = new Paragraph(new Run("COMPTE DE PRODUITS ET CHARGES (CPC)"));
                titlePara.FontSize = 20;
                titlePara.FontWeight = FontWeights.Bold;
                titlePara.TextAlignment = TextAlignment.Center;
                doc.Blocks.Add(titlePara);

                // Period
                Paragraph periodPara = new Paragraph(new Run(TxtPeriodLabel.Text));
                periodPara.FontSize = 12;
                periodPara.TextAlignment = TextAlignment.Center;
                periodPara.Margin = new Thickness(0, 5, 0, 20);
                doc.Blocks.Add(periodPara);

                // ========== PRODUITS ==========
                Paragraph produitsTitle = new Paragraph(new Run("I - PRODUITS"));
                produitsTitle.FontSize = 16;
                produitsTitle.FontWeight = FontWeights.Bold;
                produitsTitle.Foreground = Brushes.DarkGreen;
                produitsTitle.Margin = new Thickness(0, 10, 0, 5);
                doc.Blocks.Add(produitsTitle);

                // Produits d'Exploitation
                AddSection(doc, "1. Produits d'Exploitation", _currentData.ProduitsExploitation,
                    _currentData.TotalProduitsExploitation);

                // Produits Financiers
                AddSection(doc, "2. Produits Financiers", _currentData.ProduitsFinanciers,
                    _currentData.TotalProduitsFinanciers);

                // Produits Non Courants
                AddSection(doc, "3. Produits Non Courants", _currentData.ProduitsNonCourants,
                    _currentData.TotalProduitsNonCourants);

                // ========== CHARGES ==========
                Paragraph chargesTitle = new Paragraph(new Run("II - CHARGES"));
                chargesTitle.FontSize = 16;
                chargesTitle.FontWeight = FontWeights.Bold;
                chargesTitle.Foreground = Brushes.DarkRed;
                chargesTitle.Margin = new Thickness(0, 20, 0, 5);
                doc.Blocks.Add(chargesTitle);

                // Charges d'Exploitation
                AddSection(doc, "1. Charges d'Exploitation", _currentData.ChargesExploitation,
                    _currentData.TotalChargesExploitation);

                // Charges Financières
                AddSection(doc, "2. Charges Financières", _currentData.ChargesFinancieres,
                    _currentData.TotalChargesFinancieres);

                // Charges Non Courantes
                AddSection(doc, "3. Charges Non Courantes", _currentData.ChargesNonCourantes,
                    _currentData.TotalChargesNonCourantes);

                // ========== RESULTAT NET ==========
                Paragraph resultTitle = new Paragraph(new Run("III - RÉSULTAT NET"));
                resultTitle.FontSize = 16;
                resultTitle.FontWeight = FontWeights.Bold;
                resultTitle.Margin = new Thickness(0, 20, 0, 10);
                doc.Blocks.Add(resultTitle);

                Table resultTable = new Table();
                resultTable.CellSpacing = 0;
                resultTable.BorderBrush = Brushes.Black;
                resultTable.BorderThickness = new Thickness(1);
                resultTable.Columns.Add(new TableColumn { Width = new GridLength(400) });
                resultTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
                resultTable.RowGroups.Add(new TableRowGroup());

                AddResultRow(resultTable, "Résultat d'Exploitation",
                    _currentData.TotalProduitsExploitation - _currentData.TotalChargesExploitation);
                AddResultRow(resultTable, "Résultat Financier",
                    _currentData.TotalProduitsFinanciers - _currentData.TotalChargesFinancieres);
                AddResultRow(resultTable, "Résultat Non Courant",
                    _currentData.TotalProduitsNonCourants - _currentData.TotalChargesNonCourantes);

                TableRow finalRow = new TableRow();
                finalRow.Background = _currentData.ResultatNet >= 0
                    ? Brushes.LightGreen
                    : Brushes.LightCoral;
                finalRow.FontWeight = FontWeights.Bold;
                finalRow.FontSize = 14;
                finalRow.Cells.Add(new TableCell(new Paragraph(new Run(
                    _currentData.ResultatNet >= 0 ? "BÉNÉFICE NET" : "PERTE NETTE"))));
                finalRow.Cells.Add(new TableCell(new Paragraph(new Run(
                    $"{_currentData.ResultatNet:N2} MAD"))));
                resultTable.RowGroups[0].Rows.Add(finalRow);

                doc.Blocks.Add(resultTable);

                // Print
                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "CPC");

                MessageBox.Show("Impression lancée avec succès!", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'impression:\n\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Helper method for print sections
    private void AddSection(FlowDocument doc, string title, List<CPCLigneDTO> lignes, decimal total)
    {
        Paragraph sectionTitle = new Paragraph(new Run(title));
        sectionTitle.FontSize = 13;
        sectionTitle.FontWeight = FontWeights.SemiBold;
        sectionTitle.Margin = new Thickness(0, 8, 0, 3);
        doc.Blocks.Add(sectionTitle);

        if (lignes == null || lignes.Count == 0)
        {
            Paragraph emptyPara = new Paragraph(new Run("   Aucune écriture"));
            emptyPara.FontSize = 11;
            emptyPara.Foreground = Brushes.Gray;
            doc.Blocks.Add(emptyPara);
            return;
        }

        Table table = new Table();
        table.CellSpacing = 0;
        table.BorderBrush = Brushes.Gray;
        table.BorderThickness = new Thickness(0.5);
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });
        table.Columns.Add(new TableColumn { Width = new GridLength(300) });
        table.Columns.Add(new TableColumn { Width = new GridLength(120) });
        table.RowGroups.Add(new TableRowGroup());

        foreach (var ligne in lignes)
        {
            TableRow row = new TableRow();
            row.FontSize = 10;
            row.Cells.Add(new TableCell(new Paragraph(new Run(ligne.CodeCompte))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(ligne.Libelle))));
            row.Cells.Add(new TableCell(new Paragraph(new Run($"{ligne.Montant:N2}"))));
            table.RowGroups[0].Rows.Add(row);
        }

        // Total row
        TableRow totalRow = new TableRow();
        totalRow.FontWeight = FontWeights.Bold;
        totalRow.Background = Brushes.LightYellow;
        totalRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
        totalRow.Cells.Add(new TableCell(new Paragraph(new Run("TOTAL"))));
        totalRow.Cells.Add(new TableCell(new Paragraph(new Run($"{total:N2} MAD"))));
        table.RowGroups[0].Rows.Add(totalRow);

        doc.Blocks.Add(table);
    }

    // Helper for result rows
    private void AddResultRow(Table table, string label, decimal amount)
    {
        TableRow row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(label))));
        row.Cells.Add(new TableCell(new Paragraph(new Run($"{amount:N2} MAD"))));
        row.Cells[1].TextAlignment = TextAlignment.Right;
        if (amount < 0)
            row.Foreground = Brushes.Red;
        table.RowGroups[0].Rows.Add(row);
    }

    // Replace the BtnExport_Click method with this:
    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentData == null)
            {
                MessageBox.Show("Aucune donnée à exporter. Veuillez d'abord générer le CPC.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"CPC_{DateTime.Now:yyyyMMdd}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("CPC");

                    int row = 1;

                    // Title
                    ws.Cells[row, 1].Value = "COMPTE DE PRODUITS ET CHARGES (CPC)";
                    ws.Cells[row, 1, row, 4].Merge = true;
                    ws.Cells[row, 1].Style.Font.Size = 16;
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    row++;

                    // Period
                    ws.Cells[row, 1].Value = TxtPeriodLabel.Text;
                    ws.Cells[row, 1, row, 4].Merge = true;
                    ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    row += 2;

                    // ========== PRODUITS ==========
                    ws.Cells[row, 1].Value = "I - PRODUITS";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1].Style.Font.Size = 14;
                    row++;

                    row = AddExcelSection(ws, row, "1. Produits d'Exploitation",
                        _currentData.ProduitsExploitation, _currentData.TotalProduitsExploitation);
                    row = AddExcelSection(ws, row, "2. Produits Financiers",
                        _currentData.ProduitsFinanciers, _currentData.TotalProduitsFinanciers);
                    row = AddExcelSection(ws, row, "3. Produits Non Courants",
                        _currentData.ProduitsNonCourants, _currentData.TotalProduitsNonCourants);

                    row++;

                    // ========== CHARGES ==========
                    ws.Cells[row, 1].Value = "II - CHARGES";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1].Style.Font.Size = 14;
                    row++;

                    row = AddExcelSection(ws, row, "1. Charges d'Exploitation",
                        _currentData.ChargesExploitation, _currentData.TotalChargesExploitation);
                    row = AddExcelSection(ws, row, "2. Charges Financières",
                        _currentData.ChargesFinancieres, _currentData.TotalChargesFinancieres);
                    row = AddExcelSection(ws, row, "3. Charges Non Courantes",
                        _currentData.ChargesNonCourantes, _currentData.TotalChargesNonCourantes);

                    row++;

                    // ========== RESULTAT NET ==========
                    ws.Cells[row, 1].Value = "III - RÉSULTAT NET";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1].Style.Font.Size = 14;
                    row++;

                    decimal resultExpl = _currentData.TotalProduitsExploitation - _currentData.TotalChargesExploitation;
                    decimal resultFin = _currentData.TotalProduitsFinanciers - _currentData.TotalChargesFinancieres;
                    decimal resultNC = _currentData.TotalProduitsNonCourants - _currentData.TotalChargesNonCourantes;

                    ws.Cells[row, 1].Value = "Résultat d'Exploitation";
                    ws.Cells[row, 2].Value = resultExpl;
                    ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                    row++;

                    ws.Cells[row, 1].Value = "Résultat Financier";
                    ws.Cells[row, 2].Value = resultFin;
                    ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                    row++;

                    ws.Cells[row, 1].Value = "Résultat Non Courant";
                    ws.Cells[row, 2].Value = resultNC;
                    ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                    row++;

                    ws.Cells[row, 1].Value = _currentData.ResultatNet >= 0 ? "BÉNÉFICE NET" : "PERTE NETTE";
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1].Style.Font.Size = 12;
                    ws.Cells[row, 2].Value = _currentData.ResultatNet;
                    ws.Cells[row, 2].Style.Font.Bold = true;
                    ws.Cells[row, 2].Style.Font.Size = 12;
                    ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";

                    // Auto-fit
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    // Save
                    package.SaveAs(new FileInfo(saveDialog.FileName));
                }

                MessageBox.Show($"CPC exporté avec succès !\n\n{saveDialog.FileName}",
                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'export Excel:\n\n{ex.Message}",
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Helper method for Excel sections
    private int AddExcelSection(ExcelWorksheet ws, int startRow, string title,
        List<CPCLigneDTO> lignes, decimal total)
    {
        int row = startRow;

        ws.Cells[row, 1].Value = title;
        ws.Cells[row, 1].Style.Font.Bold = true;
        row++;

        if (lignes == null || lignes.Count == 0)
        {
            ws.Cells[row, 1].Value = "   Aucune écriture";
            ws.Cells[row, 1].Style.Font.Italic = true;
            row++;
            return row;
        }

        // Header
        ws.Cells[row, 1].Value = "Code";
        ws.Cells[row, 2].Value = "Libellé";
        ws.Cells[row, 3].Value = "Montant (MAD)";
        ws.Cells[row, 1, row, 3].Style.Font.Bold = true;
        row++;

        int dataStart = row;

        // Data
        foreach (var ligne in lignes)
        {
            ws.Cells[row, 1].Value = ligne.CodeCompte;
            ws.Cells[row, 2].Value = ligne.Libelle;
            ws.Cells[row, 3].Value = ligne.Montant;
            ws.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
            row++;
        }

        // Total
        ws.Cells[row, 2].Value = "TOTAL";
        ws.Cells[row, 2].Style.Font.Bold = true;
        ws.Cells[row, 3].Value = $"=SUM(C{dataStart}:C{row - 1})";
        ws.Cells[row, 3].Style.Font.Bold = true;
        ws.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
        row++;

        return row + 1; // Add spacing
    } 

    // Quick period shortcuts
    private void BtnCurrentYear_Click(object sender, RoutedEventArgs e)
        {
            DateDebutPicker.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1);
            DateFinPicker.SelectedDate = DateTime.Now;
            LoadData();
        }

        private void BtnCurrentMonth_Click(object sender, RoutedEventArgs e)
        {
            DateDebutPicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateFinPicker.SelectedDate = DateTime.Now;
            LoadData();
        }

        private void BtnQ1_Click(object sender, RoutedEventArgs e)
            => SetQuarter(1);

        private void BtnQ2_Click(object sender, RoutedEventArgs e)
            => SetQuarter(2);

        private void BtnQ3_Click(object sender, RoutedEventArgs e)
            => SetQuarter(3);

        private void BtnQ4_Click(object sender, RoutedEventArgs e)
            => SetQuarter(4);

        private void SetQuarter(int q)
        {
            int year = DateTime.Now.Year;
            int startMonth = (q - 1) * 3 + 1;
            DateDebutPicker.SelectedDate = new DateTime(year, startMonth, 1);
            DateFinPicker.SelectedDate = new DateTime(year, startMonth + 2,
                DateTime.DaysInMonth(year, startMonth + 2));
            LoadData();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  VIEW-MODEL WRAPPER for ItemsControl binding
    // ─────────────────────────────────────────────────────────────

    public class CPCLigneViewModel
    {
        public string CodeCompte { get; }
        public string Libelle { get; }
        public decimal Montant { get; }
        public string MontantFormatted { get; }

        public CPCLigneViewModel(CPCLigneDTO dto)
        {
            CodeCompte = dto.CodeCompte;
            Libelle = dto.Libelle;
            Montant = dto.Montant;
            MontantFormatted = string.Format("{0:N2} MAD", dto.Montant);
        }
    }
}
