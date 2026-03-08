using GestionComerce;
using GestionComerce.Models;
//using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Superete.Main.Comptabilite.Views
{
    // ─────────────────────────────────────────────────────────
    //  DTOs
    // ─────────────────────────────────────────────────────────
    public class BilanLigneCGNC
    {
        public string Libelle { get; set; }
        public decimal? Brut { get; set; }
        public decimal? AmortProv { get; set; }
        public decimal NetN { get; set; }
        public decimal NetN1 { get; set; }
        public bool IsSection { get; set; }
        public bool IsTotal { get; set; }
        public bool IsSubTotal { get; set; }
        public bool IsEmpty { get; set; }
    }

    public class BilanCGNCDTO
    {
        public List<BilanLigneCGNC> Actif { get; set; }
        public List<BilanLigneCGNC> Passif { get; set; }
        public decimal TotalActifBrut { get; set; }
        public decimal TotalActifAmort { get; set; }
        public decimal TotalActifNet { get; set; }
        public decimal TotalPassif { get; set; }

        public BilanCGNCDTO()
        {
            Actif = new List<BilanLigneCGNC>();
            Passif = new List<BilanLigneCGNC>();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  VIEW
    // ─────────────────────────────────────────────────────────
    public partial class BilanView : UserControl
    {
        private readonly User _user;
        private readonly ComptabiliteService _service;
        private BilanCGNCDTO _bilan;

        private const double ColLibelle = 260;
        private const double ColBrut = 80;
        private const double ColAmort = 80;
        private const double ColNetN = 80;
        private const double ColNetN1 = 80;
        private const double ColPassifN = 90;
        private const double ColPassifN1 = 90;

        // ── Brushes ───────────────────────────────────────────
        private static readonly SolidColorBrush BrRed = MakeBrush("#C0392B");
        private static readonly SolidColorBrush BrDarkRed = MakeBrush("#96281B");
        private static readonly SolidColorBrush BrLightRed = MakeBrush("#FADBD8");
        private static readonly SolidColorBrush BrRowAlt = MakeBrush("#FAFAFA");
        private static readonly SolidColorBrush BrWhite = MakeBrush("#FFFFFF");
        private static readonly SolidColorBrush BrTotalBg = MakeBrush("#F0F4FF");
        private static readonly SolidColorBrush BrDark = MakeBrush("#1A202C");
        private static readonly SolidColorBrush BrGray = MakeBrush("#4A5568");
        private static readonly SolidColorBrush BrLightGray = MakeBrush("#718096");
        private static readonly SolidColorBrush BrBlue = MakeBrush("#1A3C8F");
        private static readonly SolidColorBrush BrGreen = MakeBrush("#276749");

        public BilanView(User u)
        {
            InitializeComponent();
            _user = u;
            _service = new ComptabiliteService();
            DateBilan.SelectedDate = DateTime.Now;
            LoadBilan();
        }

        // ─────────────────────────────────────────────────────
        //  LOAD
        // ─────────────────────────────────────────────────────
        private void LoadBilan()
        {
            try
            {
                TxtStatus.Text = "Chargement…";
                DateTime date = DateBilan.SelectedDate ?? DateTime.Now;
                TxtExercice.Text = date.Year.ToString();

                _bilan = BuildBilanCGNC(date);

                ActifPanel.Children.Clear();
                PassifPanel.Children.Clear();

                int actifIdx = 0;
                foreach (BilanLigneCGNC l in _bilan.Actif)
                    ActifPanel.Children.Add(BuildActifRow(l, ref actifIdx));

                int passifIdx = 0;
                foreach (BilanLigneCGNC l in _bilan.Passif)
                    PassifPanel.Children.Add(BuildPassifRow(l, ref passifIdx));

                TxtTotalActif.Text = string.Format("{0:N2} DH", _bilan.TotalActifNet);
                TxtTotalPassif.Text = string.Format("{0:N2} DH", _bilan.TotalPassif);

                decimal diff = Math.Abs(_bilan.TotalActifNet - _bilan.TotalPassif);
                if (diff < 0.01m)
                {
                    TxtEquilibre.Text = "✅  Bilan équilibré — Actif Net = Passif";
                    TxtEquilibre.Foreground = BrGreen;
                }
                else
                {
                    TxtEquilibre.Text = string.Format("⚠️  Déséquilibre : {0:N2} DH", diff);
                    TxtEquilibre.Foreground = BrRed;
                }

                TxtStatus.Text = string.Format("Bilan au {0:dd/MM/yyyy} — chargé le {1:HH:mm:ss}", date, DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur:\n" + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Erreur de chargement.";
            }
        }

        // ─────────────────────────────────────────────────────
        //  BUILD CGNC STRUCTURE
        // ─────────────────────────────────────────────────────
        private BilanCGNCDTO BuildBilanCGNC(DateTime date)
        {
            var raw = _service.GenererBilan(date);

            decimal SumA(params string[] prefixes)
            {
                return raw.Actifs
                    .Where(a => prefixes.Any(p => a.CodeCompte.StartsWith(p)))
                    .Sum(a => a.Montant);
            }
            decimal SumP(params string[] prefixes)
            {
                return raw.Passifs
                    .Where(p => prefixes.Any(pr => p.CodeCompte.StartsWith(pr)))
                    .Sum(p => p.Montant);
            }

            var actif = new List<BilanLigneCGNC>();
            var passif = new List<BilanLigneCGNC>();

            // ══════════════════════════════════════════════════
            //  ACTIF
            // ══════════════════════════════════════════════════

            // ── ACTIF IMMOBILISÉ ──────────────────────────────
            actif.Add(MakeSection("ACTIF IMMOBILISÉ"));

            actif.Add(MakeSubSection("Immobilisations en non-valeurs (A)"));
            actif.Add(MakeRowA("Frais préliminaires", SumA("2111"), SumA("2811")));
            actif.Add(MakeRowA("Charges à répartir sur plusieurs ex.", SumA("2112"), SumA("2812")));
            actif.Add(MakeRowA("Primes de remboursement des obligations", SumA("2113"), SumA("2813")));

            actif.Add(MakeSubSection("Immobilisations incorporelles (B)"));
            actif.Add(MakeRowA("Immobilisation en recherche et développement", SumA("2121"), SumA("2821")));
            actif.Add(MakeRowA("Brevets, marques, droits et valeurs similaires", SumA("2122"), SumA("2822")));
            actif.Add(MakeRowA("Fonds commercial", SumA("2123"), SumA("2823")));
            actif.Add(MakeRowA("Autres immobilisations incorporelles", SumA("2128"), SumA("2828")));

            actif.Add(MakeSubSection("Immobilisations corporelles (C)"));
            actif.Add(MakeRowA("Terrains", SumA("2311"), SumA("2831")));
            actif.Add(MakeRowA("Constructions", SumA("2321"), SumA("2832")));
            actif.Add(MakeRowA("Installations techniques, matériel et outillage", SumA("2332"), SumA("2833")));
            actif.Add(MakeRowA("Matériel de transport", SumA("2340"), SumA("2840")));
            actif.Add(MakeRowA("Mobilier, matériel de bureau et aménag. divers", SumA("2350"), SumA("2850")));
            actif.Add(MakeRowA("Autres immobilisations corporelles", SumA("2380"), SumA("2880")));
            actif.Add(MakeRowA("Immobilisations corporelles en cours", SumA("2390"), 0m));

            actif.Add(MakeSubSection("Immobilisations financières (D)"));
            actif.Add(MakeRowA("Prêts immobilisés", SumA("2410"), 0m));
            actif.Add(MakeRowA("Autres créances financières", SumA("2411"), 0m));
            actif.Add(MakeRowA("Titres de participation", SumA("2510"), SumA("2950")));
            actif.Add(MakeRowA("Autres titres immobilisés", SumA("2520"), SumA("2952")));

            actif.Add(MakeSubSection("Écarts de conversion — Actif (E)"));
            actif.Add(MakeRowA("Diminution des créances immobilisées", SumA("2710"), 0m));
            actif.Add(MakeRowA("Augmentation des dettes de financement", SumA("2720"), 0m));

            decimal t1Brut = actif.Where(r => !r.IsSection && !r.IsSubTotal && !r.IsEmpty && r.Brut.HasValue).Sum(r => r.Brut.Value);
            decimal t1Amort = actif.Where(r => !r.IsSection && !r.IsSubTotal && !r.IsEmpty && r.AmortProv.HasValue).Sum(r => r.AmortProv.Value);
            decimal t1Net = t1Brut - t1Amort;
            actif.Add(MakeTotalA("TOTAL I  (A+B+C+D+E)", t1Brut, t1Amort, t1Net));

            // ── ACTIF CIRCULANT ───────────────────────────────
            actif.Add(MakeSection("ACTIF CIRCULANT (HORS TRÉSORERIE)"));

            actif.Add(MakeSubSection("Stocks (F)"));
            actif.Add(MakeRowA("Marchandises", SumA("3110"), SumA("3910")));
            actif.Add(MakeRowA("Matières et fournitures consommables", SumA("3120"), SumA("3920")));
            actif.Add(MakeRowA("Produits en cours", SumA("3130"), SumA("3930")));
            actif.Add(MakeRowA("Produits intermédiaires et prod. résiduels", SumA("3140"), SumA("3940")));
            actif.Add(MakeRowA("Produits finis", SumA("3150"), SumA("3950")));

            actif.Add(MakeSubSection("Créances de l'actif circulant (G)"));
            actif.Add(MakeRowA("Fournisseurs, débiteurs, avances et acomptes", SumA("3410"), 0m));
            actif.Add(MakeRowA("Clients et comptes rattachés", SumA("3421"), SumA("3942")));
            actif.Add(MakeRowA("Personnel", SumA("3430"), 0m));
            actif.Add(MakeRowA("État", SumA("3450"), 0m));
            actif.Add(MakeRowA("Comptes d'associés", SumA("3460"), 0m));
            actif.Add(MakeRowA("Autres débiteurs", SumA("3480"), SumA("3948")));
            actif.Add(MakeRowA("Comptes de régularisation Actif", SumA("3491"), 0m));

            actif.Add(MakeRowA("Titres et valeurs de placement (H)", SumA("3500"), SumA("3950")));
            actif.Add(MakeRowA("Écarts de conversion — Actif (I) (circulants)", SumA("3710"), 0m));

            decimal t2Brut = SumA("31", "34", "35", "37");
            decimal t2Amort = SumA("391", "394", "395", "398");
            decimal t2Net = t2Brut - t2Amort;
            actif.Add(MakeTotalA("TOTAL II  (F+G+H+I)", t2Brut, t2Amort, t2Net));

            // ── TRÉSORERIE ACTIF ──────────────────────────────
            actif.Add(MakeSection("TRÉSORERIE — ACTIF"));
            actif.Add(MakeRowA("Chèques et valeurs à encaisser", SumA("5111"), 0m));
            actif.Add(MakeRowA("Banques, Trésorerie Générale, Chèques postaux", SumA("5141"), 0m));
            actif.Add(MakeRowA("Caisse, Régies d'avances et accrédités", SumA("5161"), 0m));

            decimal t3Brut = SumA("51", "52", "53", "54", "55", "59");
            actif.Add(MakeTotalA("TOTAL III", t3Brut, 0m, t3Brut));

            decimal grandBrut = t1Brut + t2Brut + t3Brut;
            decimal grandAmort = t1Amort + t2Amort;
            decimal grandNet = grandBrut - grandAmort;
            actif.Add(MakeTotalA("TOTAL GÉNÉRAL  I+II+III", grandBrut, grandAmort, grandNet));

            // ══════════════════════════════════════════════════
            //  PASSIF
            // ══════════════════════════════════════════════════

            passif.Add(MakeSection("FINANCEMENT PERMANENT"));

            passif.Add(MakeSubSection("Capitaux propres"));
            passif.Add(MakeRowP("Capital social ou personnel (1)", SumP("1111", "1112")));
            passif.Add(MakeRowP("moins : capital souscrit non appelé", SumP("1119")));
            passif.Add(MakeRowP("Capital appelé / dont versé", SumP("1113")));
            passif.Add(MakeRowP("Primes d'émission, de fusion, d'apport", SumP("1120")));
            passif.Add(MakeRowP("Écart de réévaluation", SumP("1130")));
            passif.Add(MakeRowP("Réserve légale", SumP("1141")));
            passif.Add(MakeRowP("Autres réserves", SumP("1148")));
            passif.Add(MakeRowP("Report à nouveau (2)", SumP("1150")));
            passif.Add(MakeRowP("Résultat net en instance d'affectation (2)", SumP("1161")));
            passif.Add(MakeRowP("Résultat net de l'exercice (2)", SumP("1191")));
            passif.Add(MakeTotalP("TOTAL DES CAPITAUX PROPRES (A)",
                SumP("111", "112", "113", "114", "115", "116", "119")));

            passif.Add(MakeSubSection("Capitaux propres assimilés (B)"));
            passif.Add(MakeRowP("Subventions d'investissement", SumP("1311")));
            passif.Add(MakeRowP("Provisions réglementées", SumP("1320")));

            passif.Add(MakeSubSection("Dettes de financement (C)"));
            passif.Add(MakeRowP("Emprunts obligataires", SumP("1410")));
            passif.Add(MakeRowP("Autres dettes de financement", SumP("1480")));

            passif.Add(MakeSubSection("Provisions durables pour risques et charges (D)"));
            passif.Add(MakeRowP("Provisions pour risques", SumP("1511")));
            passif.Add(MakeRowP("Provisions pour charges", SumP("1512")));

            passif.Add(MakeSubSection("Écarts de conversion — Passif (E)"));
            passif.Add(MakeRowP("Augmentation des créances immobilisées", SumP("1710")));
            passif.Add(MakeRowP("Diminution des dettes de financement", SumP("1720")));

            decimal tp1 = SumP("11", "12", "13", "14", "15", "17");
            passif.Add(MakeTotalP("TOTAL I  (A+B+C+D+E)", tp1));

            // ── PASSIF CIRCULANT ──────────────────────────────
            passif.Add(MakeSection("PASSIF CIRCULANT (HORS TRÉSORERIE)"));

            passif.Add(MakeSubSection("Dettes du passif circulant (F)"));
            passif.Add(MakeRowP("Fournisseurs et comptes rattachés", SumP("4411")));
            passif.Add(MakeRowP("Clients créditeurs, avances et acomptes", SumP("4421")));
            passif.Add(MakeRowP("Personnel", SumP("4430")));
            passif.Add(MakeRowP("Organismes sociaux", SumP("4441")));
            passif.Add(MakeRowP("État", SumP("4450")));
            passif.Add(MakeRowP("Comptes d'associés", SumP("4460")));
            passif.Add(MakeRowP("Autres créanciers", SumP("4480")));
            passif.Add(MakeRowP("Comptes de régularisation passif", SumP("4491")));

            passif.Add(MakeSubSection("Autres provisions pour risques et charges (G)"));
            passif.Add(MakeRowP("Provisions pour risques et charges", SumP("4510")));

            passif.Add(MakeSubSection("Écarts de conversion — Passif (Éléments circulants) (H)"));
            passif.Add(MakeRowP("Écarts de conversion passif circulant", SumP("4701")));

            decimal tp2 = SumP("44", "45", "47");
            passif.Add(MakeTotalP("TOTAL II  (F+G+H)", tp2));

            // ── TRÉSORERIE PASSIF ─────────────────────────────
            passif.Add(MakeSection("TRÉSORERIE — PASSIF"));
            passif.Add(MakeRowP("Crédits d'escompte", SumP("5510")));
            passif.Add(MakeRowP("Crédits de trésorerie", SumP("5520")));
            passif.Add(MakeRowP("Banques (soldes créditeurs)", SumP("5141")));

            decimal tp3 = SumP("55", "56", "57", "58");
            passif.Add(MakeTotalP("TOTAL III", tp3));

            decimal grandPassif = tp1 + tp2 + tp3;
            passif.Add(MakeTotalP("TOTAL GÉNÉRAL  I+II+III", grandPassif));

            passif.Add(MakeFootNote("(1) capital personnel débiteur (−)"));
            passif.Add(MakeFootNote("(2) bénéficiaire (+) déficitaire (−)"));

            return new BilanCGNCDTO
            {
                Actif = actif,
                Passif = passif,
                TotalActifBrut = grandBrut,
                TotalActifAmort = grandAmort,
                TotalActifNet = grandNet,
                TotalPassif = grandPassif
            };
        }

        // ─────────────────────────────────────────────────────
        //  DTO FACTORY METHODS
        // ─────────────────────────────────────────────────────
        private static BilanLigneCGNC MakeSection(string lib)
        {
            return new BilanLigneCGNC { Libelle = lib, IsSection = true };
        }
        private static BilanLigneCGNC MakeSubSection(string lib)
        {
            return new BilanLigneCGNC { Libelle = lib, IsSubTotal = true };
        }
        private static BilanLigneCGNC MakeRowA(string lib, decimal brut, decimal amort)
        {
            return new BilanLigneCGNC { Libelle = lib, Brut = brut, AmortProv = amort, NetN = brut - amort };
        }
        private static BilanLigneCGNC MakeRowP(string lib, decimal netN)
        {
            return new BilanLigneCGNC { Libelle = lib, NetN = netN };
        }
        private static BilanLigneCGNC MakeTotalA(string lib, decimal brut, decimal amort, decimal net)
        {
            return new BilanLigneCGNC { Libelle = lib, Brut = brut, AmortProv = amort, NetN = net, IsTotal = true };
        }
        private static BilanLigneCGNC MakeTotalP(string lib, decimal net)
        {
            return new BilanLigneCGNC { Libelle = lib, NetN = net, IsTotal = true };
        }
        private static BilanLigneCGNC MakeFootNote(string text)
        {
            return new BilanLigneCGNC { Libelle = text, IsEmpty = true };
        }

        // ─────────────────────────────────────────────────────
        //  UI ROW BUILDERS
        // ─────────────────────────────────────────────────────
        private System.Windows.Controls.Border BuildActifRow(BilanLigneCGNC l, ref int idx)
        {
            if (l.IsSection) return MakeSectionHeader(l.Libelle);
            if (l.IsSubTotal) return MakeSubHeader(l.Libelle);

            bool isAlt = (idx++ % 2 == 0);
            SolidColorBrush bg = l.IsTotal ? BrTotalBg : (isAlt ? BrRowAlt : BrWhite);
            FontWeight fw = l.IsTotal ? FontWeights.Bold : FontWeights.Normal;

            Grid grid = new Grid { Background = bg };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColLibelle) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColBrut) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColAmort) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColNetN) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColNetN1) });

            string brutStr = l.Brut.HasValue ? string.Format("{0:N2}", l.Brut) : "";
            string amortStr = l.AmortProv.HasValue ? string.Format("{0:N2}", l.AmortProv) : "";

            grid.Children.Add(MakeCell(0, l.Libelle, fw, HorizontalAlignment.Left, l.IsTotal ? BrDark : BrGray));
            grid.Children.Add(MakeCell(1, brutStr, fw, HorizontalAlignment.Right, BrDark));
            grid.Children.Add(MakeCell(2, amortStr, fw, HorizontalAlignment.Right, BrDark));
            grid.Children.Add(MakeCell(3, string.Format("{0:N2}", l.NetN), fw, HorizontalAlignment.Right, l.IsTotal ? BrBlue : BrDark));
            grid.Children.Add(MakeCell(4, string.Format("{0:N2}", l.NetN1), FontWeights.Normal, HorizontalAlignment.Right, BrLightGray));

            var border = new System.Windows.Controls.Border
            {
                Child = grid,
                BorderBrush = l.IsTotal ? MakeBrush("#667EEA") : MakeBrush("#E2E8F0"),
                BorderThickness = new Thickness(0, 0, 0, l.IsTotal ? 2 : 1)
            };
            return border;
        }

        private System.Windows.Controls.Border BuildPassifRow(BilanLigneCGNC l, ref int idx)
        {
            if (l.IsSection) return MakeSectionHeader(l.Libelle);

            if (l.IsEmpty)
            {
                var note = new System.Windows.Controls.Border { Background = BrWhite, Padding = new Thickness(8, 3, 4, 3) };
                note.Child = new TextBlock
                {
                    Text = l.Libelle,
                    FontSize = 10,
                    Foreground = BrLightGray,
                    FontStyle = FontStyles.Italic
                };
                return note;
            }

            if (l.IsSubTotal) return MakeSubHeader(l.Libelle);

            bool isAlt = (idx++ % 2 == 0);
            SolidColorBrush bg = l.IsTotal ? MakeBrush("#F0FFF4") : (isAlt ? BrRowAlt : BrWhite);
            FontWeight fw = l.IsTotal ? FontWeights.Bold : FontWeights.Normal;

            Grid grid = new Grid { Background = bg };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColLibelle) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColPassifN) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColPassifN1) });

            grid.Children.Add(MakeCell(0, l.Libelle, fw, HorizontalAlignment.Left, l.IsTotal ? BrDark : BrGray));
            grid.Children.Add(MakeCell(1, string.Format("{0:N2}", l.NetN), fw, HorizontalAlignment.Right, l.IsTotal ? BrGreen : BrDark));
            grid.Children.Add(MakeCell(2, string.Format("{0:N2}", l.NetN1), FontWeights.Normal, HorizontalAlignment.Right, BrLightGray));

            var border = new System.Windows.Controls.Border
            {
                Child = grid,
                BorderBrush = l.IsTotal ? MakeBrush("#48BB78") : MakeBrush("#E2E8F0"),
                BorderThickness = new Thickness(0, 0, 0, l.IsTotal ? 2 : 1)
            };
            return border;
        }

        // ─────────────────────────────────────────────────────
        //  UI HELPERS
        // ─────────────────────────────────────────────────────
        private static System.Windows.Controls.Border MakeSectionHeader(string text)
        {
            return new System.Windows.Controls.Border
            {
                Background = BrRed,
                Padding = new Thickness(8, 6, 4, 6),
                Child = new TextBlock
                {
                    Text = text.ToUpper(),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                }
            };
        }

        private static System.Windows.Controls.Border MakeSubHeader(string text)
        {
            return new System.Windows.Controls.Border
            {
                Background = BrLightRed,
                Padding = new Thickness(8, 4, 4, 4),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrDarkRed
                }
            };
        }

        private static TextBlock MakeCell(int col, string text, FontWeight fw,
            HorizontalAlignment ha, SolidColorBrush fg)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontWeight = fw,
                FontSize = 11,
                HorizontalAlignment = ha,
                Foreground = fg,
                Padding = new Thickness(6, 4, 6, 4),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(tb, col);
            return tb;
        }

        private static SolidColorBrush MakeBrush(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        // ─────────────────────────────────────────────────────
        //  EVENTS
        // ─────────────────────────────────────────────────────
        private void DateBilan_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadBilan();
        }

        // ─────────────────────────────────────────────────────
        //  PRINT
        // ─────────────────────────────────────────────────────
        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog dlg = new PrintDialog();
                if (dlg.ShowDialog() != true) return;

                FlowDocument doc = BuildPrintDocument();
                IDocumentPaginatorSource src = doc;
                dlg.PrintDocument(src.DocumentPaginator, "Bilan CGNC");
                MessageBox.Show("Impression lancée !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur impression :\n" + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument BuildPrintDocument()
        {
            FlowDocument doc = new FlowDocument
            {
                PagePadding = new Thickness(40),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10
            };

            doc.Blocks.Add(new Paragraph(new Run("BILAN COMPTABLE — MODÈLE CGNC"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            });
            doc.Blocks.Add(new Paragraph(new Run("Exercice clos le " + (DateBilan.SelectedDate.HasValue ? DateBilan.SelectedDate.Value.ToString("dd/MM/yyyy") : "")))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            });

            // ACTIF
            doc.Blocks.Add(new Paragraph(new Run("ACTIF"))
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B")),
                Margin = new Thickness(0, 10, 0, 4)
            });
            Table actifTable = CreatePrintTable(new string[] { "Libellé", "Brut", "Amort./Prov.", "Net N", "Net N-1" });
            foreach (BilanLigneCGNC l in _bilan.Actif)
                AddPrintRow(actifTable, l, true);
            doc.Blocks.Add(actifTable);

            // PASSIF
            doc.Blocks.Add(new Paragraph(new Run("PASSIF"))
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B")),
                Margin = new Thickness(0, 16, 0, 4)
            });
            Table passifTable = CreatePrintTable(new string[] { "Libellé", "Exercice N", "N-1" });
            foreach (BilanLigneCGNC l in _bilan.Passif)
                AddPrintRow(passifTable, l, false);
            doc.Blocks.Add(passifTable);

            return doc;
        }

        private Table CreatePrintTable(string[] headers)
        {
            Table t = new Table { CellSpacing = 0 };
            t.Columns.Add(new TableColumn { Width = new GridLength(220) });
            for (int i = 1; i < headers.Length; i++)
                t.Columns.Add(new TableColumn { Width = new GridLength(80) });

            TableRowGroup rg = new TableRowGroup();
            TableRow hr = new TableRow { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B")) };
            foreach (string h in headers)
            {
                TableCell tc = new TableCell(new Paragraph(new Run(h))
                {
                    TextAlignment = TextAlignment.Center
                })
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(4, 2, 4, 2)
                };
                hr.Cells.Add(tc);
            }
            rg.Rows.Add(hr);
            t.RowGroups.Add(rg);
            return t;
        }

        private void AddPrintRow(Table t, BilanLigneCGNC l, bool isActif)
        {
            TableRowGroup rg = t.RowGroups[0];

            if (l.IsEmpty) return;

            TableRow row = new TableRow();

            if (l.IsSection)
            {
                row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0392B"));
                TableCell sc = new TableCell(new Paragraph(new Run(l.Libelle.ToUpper()))
                { FontWeight = FontWeights.Bold })
                {
                    ColumnSpan = isActif ? 5 : 3,
                    Foreground = Brushes.White,
                    Padding = new Thickness(4, 2, 4, 2)
                };
                row.Cells.Add(sc);
                rg.Rows.Add(row);
                return;
            }

            if (l.IsTotal)
                row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EBF8FF"));

            row.Cells.Add(MakePrintCell(l.Libelle, l.IsTotal || l.IsSubTotal, TextAlignment.Left));

            if (isActif)
            {
                row.Cells.Add(MakePrintCell(l.Brut.HasValue ? string.Format("{0:N2}", l.Brut) : "", l.IsTotal, TextAlignment.Right));
                row.Cells.Add(MakePrintCell(l.AmortProv.HasValue ? string.Format("{0:N2}", l.AmortProv) : "", l.IsTotal, TextAlignment.Right));
            }
            row.Cells.Add(MakePrintCell(string.Format("{0:N2}", l.NetN), l.IsTotal, TextAlignment.Right));
            row.Cells.Add(MakePrintCell(string.Format("{0:N2}", l.NetN1), false, TextAlignment.Right));

            rg.Rows.Add(row);
        }

        private TableCell MakePrintCell(string text, bool bold, TextAlignment align)
        {
            return new TableCell(new Paragraph(new Run(text)) { TextAlignment = align })
            {
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4, 2, 4, 2)
            };
        }

        // ─────────────────────────────────────────────────────
        //  EXCEL
        // ─────────────────────────────────────────────────────
        private void BtnExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel|*.xlsx",
                    FileName = "Bilan_CGNC_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx",
                    DefaultExt = ".xlsx"
                };
                if (dlg.ShowDialog() != true) return;

                using (ExcelPackage pkg = new ExcelPackage())
                {
                    BuildActifSheet(pkg.Workbook.Worksheets.Add("Actif"));
                    BuildPassifSheet(pkg.Workbook.Worksheets.Add("Passif"));
                    pkg.SaveAs(new FileInfo(dlg.FileName));
                }

                MessageBox.Show("Exporté :\n" + dlg.FileName, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur Excel :\n" + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static System.Drawing.Color Rgb(byte r, byte g, byte b)
        {
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        private void BuildActifSheet(ExcelWorksheet ws)
        {
            ws.Cells["A1"].Value = "BILAN (ACTIF) — MODÈLE CGNC";
            ws.Cells["A1:E1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int row = 3;
            ws.Cells[row, 1].Value = "Libellé";
            ws.Cells[row, 2].Value = "Brut";
            ws.Cells[row, 3].Value = "Amort./Prov.";
            ws.Cells[row, 4].Value = "Net N";
            ws.Cells[row, 5].Value = "Net N-1";
            for (int c = 1; c <= 5; c++)
            {
                ws.Cells[row, c].Style.Font.Bold = true;
                ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(Rgb(192, 57, 43));
                ws.Cells[row, c].Style.Font.Color.SetColor(Rgb(255, 255, 255));
            }
            row++;

            foreach (BilanLigneCGNC l in _bilan.Actif)
            {
                if (l.IsEmpty) continue;

                ws.Cells[row, 1].Value = l.Libelle;

                if (l.IsSection)
                {
                    for (int c = 1; c <= 5; c++)
                    {
                        ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(Rgb(192, 57, 43));
                        ws.Cells[row, c].Style.Font.Color.SetColor(Rgb(255, 255, 255));
                        ws.Cells[row, c].Style.Font.Bold = true;
                    }
                }
                else if (l.IsSubTotal)
                {
                    ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Rgb(250, 219, 216));
                    ws.Cells[row, 1].Style.Font.Bold = true;
                }
                else if (l.IsTotal)
                {
                    for (int c = 1; c <= 5; c++)
                    {
                        ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(Rgb(176, 196, 222));
                        ws.Cells[row, c].Style.Font.Bold = true;
                    }
                }

                if (!l.IsSection && !l.IsSubTotal)
                {
                    if (l.Brut.HasValue)
                    {
                        ws.Cells[row, 2].Value = l.Brut.Value;
                        ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                    }
                    if (l.AmortProv.HasValue)
                    {
                        ws.Cells[row, 3].Value = l.AmortProv.Value;
                        ws.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                    }
                    ws.Cells[row, 4].Value = l.NetN;
                    ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                    ws.Cells[row, 5].Value = l.NetN1;
                    ws.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                }

                row++;
            }

            ws.Column(1).Width = 45;
            for (int c = 2; c <= 5; c++) ws.Column(c).Width = 16;
        }

        private void BuildPassifSheet(ExcelWorksheet ws)
        {
            ws.Cells["A1"].Value = "BILAN (PASSIF) — MODÈLE CGNC";
            ws.Cells["A1:C1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int row = 3;
            ws.Cells[row, 1].Value = "Libellé";
            ws.Cells[row, 2].Value = "Exercice N";
            ws.Cells[row, 3].Value = "N-1";
            for (int c = 1; c <= 3; c++)
            {
                ws.Cells[row, c].Style.Font.Bold = true;
                ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(Rgb(192, 57, 43));
                ws.Cells[row, c].Style.Font.Color.SetColor(Rgb(255, 255, 255));
            }
            row++;

            foreach (BilanLigneCGNC l in _bilan.Passif)
            {
                if (l.IsEmpty)
                {
                    ws.Cells[row, 1].Value = l.Libelle;
                    ws.Cells[row, 1].Style.Font.Italic = true;
                    row++;
                    continue;
                }

                ws.Cells[row, 1].Value = l.Libelle;

                if (l.IsSection)
                {
                    for (int c = 1; c <= 3; c++)
                    {
                        ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(Rgb(192, 57, 43));
                        ws.Cells[row, c].Style.Font.Color.SetColor(Rgb(255, 255, 255));
                        ws.Cells[row, c].Style.Font.Bold = true;
                    }
                }
                else if (l.IsSubTotal)
                {
                    ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Rgb(250, 219, 216));
                    ws.Cells[row, 1].Style.Font.Bold = true;
                }
                else if (l.IsTotal)
                {
                    for (int c = 1; c <= 3; c++)
                    {
                        ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(Rgb(144, 238, 144));
                        ws.Cells[row, c].Style.Font.Bold = true;
                    }
                }

                if (!l.IsSection && !l.IsSubTotal)
                {
                    ws.Cells[row, 2].Value = l.NetN;
                    ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                    ws.Cells[row, 3].Value = l.NetN1;
                    ws.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                }

                row++;
            }

            ws.Column(1).Width = 45;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 16;
        }
    }
}