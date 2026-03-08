using GestionComerce;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Superete.Main.Comptabilite.Views
{
    public partial class JournalGeneralView : UserControl
    {
        private User currentUser;
        private ComptabiliteService comptabiliteService;
        private List<EcrituresComptables> ecritures;

        public JournalGeneralView(User u)
        {
            InitializeComponent();
            currentUser = u;
            comptabiliteService = new ComptabiliteService();

            // Set default dates
            DateDebut.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1);
            DateFin.SelectedDate = DateTime.Now;

            // Use Loaded event to avoid blocking the UI thread
            this.Loaded += async (s, e) => await LoadJournalAsync();
        }

        private async System.Threading.Tasks.Task LoadJournalAsync()
        {
            try
            {
                DateTime? dateDebut = DateDebut.SelectedDate;
                DateTime? dateFin = DateFin.SelectedDate;

                string typeOperation = null;
                if (TypeFilter.SelectedIndex > 0)
                {
                    typeOperation = ((ComboBoxItem)TypeFilter.SelectedItem).Content.ToString();
                }

                ecritures = await comptabiliteService.ObtenirJournalGeneralAsync(dateDebut, dateFin, typeOperation);

                JournalDataGrid.ItemsSource = ecritures;

                // Calculate totals
                decimal totalDebit = ecritures.Sum(e => e.Debit);
                decimal totalCredit = ecritures.Sum(e => e.Credit);
                decimal difference = totalDebit - totalCredit;

                TxtTotalDebit.Text = string.Format("{0:N2} DH", totalDebit);
                TxtTotalCredit.Text = string.Format("{0:N2} DH", totalCredit);
                TxtDifference.Text = string.Format("{0:N2} DH", Math.Abs(difference));
                TxtNombreEcritures.Text = string.Format("{0} écritures", ecritures.Count);

                // Color the difference based on balance
                if (Math.Abs(difference) < 0.01m)
                {
                    TxtDifference.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
                }
                else
                {
                    TxtDifference.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur: {0}", ex.Message), "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Filter_Click(object sender, RoutedEventArgs e)
        {
            await LoadJournalAsync();
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ecritures == null || ecritures.Count == 0)
                {
                    MessageBox.Show("Aucune écriture à exporter.", "Attention",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"Journal_General_{DateTime.Now:yyyyMMdd}.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("Journal Général");

                        int row = 1;

                        // Title
                        ws.Cells[row, 1].Value = "JOURNAL GÉNÉRAL";
                        ws.Cells[row, 1, row, 7].Merge = true;
                        ws.Cells[row, 1].Style.Font.Size = 16;
                        ws.Cells[row, 1].Style.Font.Bold = true;
                        ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;

                        // Period
                        string periode = $"Période : {DateDebut.SelectedDate:dd/MM/yyyy} au {DateFin.SelectedDate:dd/MM/yyyy}";
                        ws.Cells[row, 1].Value = periode;
                        ws.Cells[row, 1, row, 7].Merge = true;
                        ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        row++;

                        // Filter info
                        if (TypeFilter.SelectedIndex > 0)
                        {
                            string typeOp = ((ComboBoxItem)TypeFilter.SelectedItem).Content.ToString();
                            ws.Cells[row, 1].Value = $"Type d'opération : {typeOp}";
                            ws.Cells[row, 1, row, 7].Merge = true;
                            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            row++;
                        }

                        row++;

                        // Headers
                        ws.Cells[row, 1].Value = "Date";
                        ws.Cells[row, 2].Value = "Code Compte";
                        ws.Cells[row, 3].Value = "Libellé Compte";
                        ws.Cells[row, 4].Value = "Libellé Écriture";
                        ws.Cells[row, 5].Value = "Débit (DH)";
                        ws.Cells[row, 6].Value = "Crédit (DH)";
                        ws.Cells[row, 7].Value = "Solde (DH)";

                        // Header styling
                        for (int col = 1; col <= 7; col++)
                        {
                            ws.Cells[row, col].Style.Font.Bold = true;
                            ws.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            ws.Cells[row, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        }
                        row++;

                        int dataStartRow = row;
                        decimal runningSolde = 0;

                        // Data rows
                        foreach (var ecriture in ecritures)
                        {
                            ws.Cells[row, 1].Value = ecriture.DateEcriture;
                            ws.Cells[row, 1].Style.Numberformat.Format = "dd/mm/yyyy";

                            ws.Cells[row, 2].Value = ecriture.CodeCompte;
                            ws.Cells[row, 3].Value = ecriture.LibelleCompte ?? "";
                            ws.Cells[row, 4].Value = ecriture.Libelle;

                            ws.Cells[row, 5].Value = ecriture.Debit;
                            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";

                            ws.Cells[row, 6].Value = ecriture.Credit;
                            ws.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";

                            // Running balance
                            runningSolde += ecriture.Debit - ecriture.Credit;
                            ws.Cells[row, 7].Value = runningSolde;
                            ws.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";

                            row++;
                        }

                        int dataEndRow = row - 1;

                        // Totals row
                        row++;
                        ws.Cells[row, 4].Value = "TOTAUX";
                        ws.Cells[row, 4].Style.Font.Bold = true;
                        ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 5].Value = $"=SUM(E{dataStartRow}:E{dataEndRow})";
                        ws.Cells[row, 5].Style.Font.Bold = true;
                        ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";

                        ws.Cells[row, 6].Value = $"=SUM(F{dataStartRow}:F{dataEndRow})";
                        ws.Cells[row, 6].Style.Font.Bold = true;
                        ws.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";

                        // Difference row
                        row++;
                        ws.Cells[row, 4].Value = "DIFFÉRENCE";
                        ws.Cells[row, 4].Style.Font.Bold = true;
                        ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        ws.Cells[row, 7].Value = $"=E{row - 1}-F{row - 1}";
                        ws.Cells[row, 7].Style.Font.Bold = true;
                        ws.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";

                        // Summary section
                        row += 2;
                        ws.Cells[row, 1].Value = "RÉSUMÉ";
                        ws.Cells[row, 1].Style.Font.Bold = true;
                        ws.Cells[row, 1].Style.Font.Size = 12;
                        row++;

                        ws.Cells[row, 1].Value = "Nombre d'écritures :";
                        ws.Cells[row, 2].Value = ecritures.Count;
                        row++;

                        ws.Cells[row, 1].Value = "Total Débit :";
                        ws.Cells[row, 2].Value = ecritures.Sum(ec => ec.Debit);  // fixed
                        ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00 \"DH\"";
                        row++;

                        ws.Cells[row, 1].Value = "Total Crédit :";
                        ws.Cells[row, 2].Value = ecritures.Sum(ec => ec.Credit);  // fixed
                        ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00 \"DH\"";
                        row++;

                        decimal difference = ecritures.Sum(ec => ec.Debit) - ecritures.Sum(ec => ec.Credit);  // fixed
                        ws.Cells[row, 1].Value = "Différence :";
                        ws.Cells[row, 2].Value = Math.Abs(difference);
                        ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00 \"DH\"";
                        ws.Cells[row, 1].Style.Font.Bold = true;
                        ws.Cells[row, 2].Style.Font.Bold = true;

                        // Column widths
                        ws.Column(1).Width = 12;
                        ws.Column(2).Width = 12;
                        ws.Column(3).Width = 30;
                        ws.Column(4).Width = 35;
                        ws.Column(5).Width = 15;
                        ws.Column(6).Width = 15;
                        ws.Column(7).Width = 15;

                        // Add borders to data range
                        var dataRange = ws.Cells[dataStartRow - 1, 1, dataEndRow, 7];
                        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                        // Freeze header row
                        ws.View.FreezePanes(dataStartRow, 1);

                        // Save
                        package.SaveAs(new FileInfo(saveDialog.FileName));
                    }

                    MessageBox.Show($"Journal Général exporté avec succès !\n\n{saveDialog.FileName}",
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
    }
}