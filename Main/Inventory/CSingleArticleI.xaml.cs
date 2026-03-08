using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GestionComerce.Main.Inventory
{
    public partial class CSingleArticleI : UserControl
    {
        List<Article> la;
        List<Famille> lf;
        CMainI main;
        public Article a;
        List<Fournisseur> lfo;
        private bool isCardMode;
        private string iconSize;

        public CSingleArticleI(Article a, List<Article> la, CMainI main, List<Famille> lf, List<Fournisseur> lfo, bool isCardMode = false, string iconSize = "Moyennes")
        {
            InitializeComponent();
            this.a = a;
            this.la = la;
            this.main = main;
            this.lf = lf;
            this.lfo = lfo;
            this.isCardMode = isCardMode;
            this.iconSize = iconSize;

            // Apply layout
            if (isCardMode)
            {
                if (iconSize == "Petites")
                {
                    ApplySmallCardLayout(); // Nouveau layout simplifié
                }
                else
                {
                    ApplyCardLayout(); // Layout normal (Grandes/Moyennes)
                }
            }
            else
            {
                ApplyRowLayout();
            }

            // Load article image
            LoadArticleImage();

            // Set article name
            ArticleName.Text = a.ArticleName;
            if (isCardMode)
            {
                if (iconSize == "Petites")
                {
                    SmallArticleName.Text = a.ArticleName;
                }
                else
                {
                    CardArticleName.Text = a.ArticleName;
                }
            }

            // Set famille name
            foreach (Famille f in lf)
            {
                if (f.FamilleID == a.FamillyID)
                {
                    Famillee.Text = f.FamilleName;
                    if (isCardMode && iconSize != "Petites")
                        CardFamillee.Text = f.FamilleName;
                    break;
                }
            }

            // Set fournisseur name
            foreach (Fournisseur fo in lfo)
            {
                if (fo.FournisseurID == a.FournisseurID)
                {
                    Fournisseur.Text = fo.Nom;
                    if (isCardMode && iconSize != "Petites")
                        CardFournisseur.Text = fo.Nom;
                    break;
                }
            }

            // Set prices
            PrixVente.Text = a.PrixVente.ToString("0.00") + " Dh";
            PrixAchat.Text = a.PrixAchat.ToString("0.00") + " Dh";

            if (isCardMode)
            {
                if (iconSize == "Petites")
                {
                    SmallPrixVente.Text = a.PrixVente.ToString("0.00") + " Dh";
                    SmallPrixAchat.Text = a.PrixAchat.ToString("0.00") + " Dh";
                }
                else
                {
                    CardPrixVente.Text = a.PrixVente.ToString("0.00") + " Dh";
                    CardPrixAchat.Text = a.PrixAchat.ToString("0.00") + " Dh";
                }
            }

            // Set quantity
            Quantite.Text = a.Quantite.ToString();
            if (isCardMode && iconSize != "Petites")
                CardQuantite.Text = a.Quantite.ToString();

            if (a.DateExpiration.HasValue)
            {
                DateExpiration.Text = a.DateExpiration.Value.ToString("dd/MM/yyyy");
                TimeSpan timeUntilExpiration = a.DateExpiration.Value - DateTime.Now;

                if (timeUntilExpiration.TotalDays < 0)
                {
                    DateExpiration.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(220, 38, 38));
                    DateExpiration.FontWeight = FontWeights.Bold;
                }
                else if (timeUntilExpiration.TotalDays <= 30)
                {
                    DateExpiration.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(245, 158, 11));
                    DateExpiration.FontWeight = FontWeights.SemiBold;
                }
            }
            else
            {
                DateExpiration.Text = "N/A";
            }

            // Set permissions
            foreach (Role r in main.main.lr)
            {
                if (main.u.RoleID == r.RoleID)
                {
                    if (r.DeleteArticle == false)
                    {
                        Deletebtn.IsEnabled = false;
                        if (isCardMode && iconSize != "Petites")
                            CardDeletebtn.IsEnabled = false;
                    }
                    if (r.EditArticle == false)
                    {
                        Editbtn.IsEnabled = false;
                        if (isCardMode && iconSize != "Petites")
                            CardEditbtn.IsEnabled = false;
                    }
                    break;
                }
            }
        }
        private void ApplySmallCardLayout()
        {
            this.Width = 150;
            this.Height = 170;
            this.Margin = new Thickness(4);

            RowLayout.Visibility = Visibility.Collapsed;
            CardLayout.Visibility = Visibility.Collapsed;
            SmallCardLayout.Visibility = Visibility.Visible;
        }
        private void ApplyCardLayout()
        {
            // Définir les dimensions selon la taille (seulement Grandes et Moyennes)
            int cardWidth = 0;
            int cardHeight = 0;
            int imageHeight = 0;

            switch (iconSize)
            {
                case "Grandes":
                    cardWidth = 280;
                    cardHeight = 480;
                    imageHeight = 180;
                    break;
                case "Moyennes":
                    cardWidth = 250;
                    cardHeight = 430;
                    imageHeight = 140;
                    break;
                default:
                    cardWidth = 220;
                    cardHeight = 270;
                    imageHeight = 140;
                    break;
            }

            this.Width = cardWidth;
            this.Height = cardHeight; this.Margin = new Thickness(4);

            // Mettre à jour la hauteur de la section image dans le Grid
            if (CardLayout != null && CardLayout.Child is Grid cardGrid)
            {
                if (cardGrid.RowDefinitions.Count > 0)
                {
                    cardGrid.RowDefinitions[0].Height = new GridLength(imageHeight);
                }
            }

            RowLayout.Visibility = Visibility.Collapsed;
            CardLayout.Visibility = Visibility.Visible;
            SmallCardLayout.Visibility = Visibility.Collapsed;
        }

        private void ApplyRowLayout()
        {
            this.Width = double.NaN;
            this.Height = 40;
            this.Margin = new Thickness(0, 0, 0, 0);

            RowLayout.Visibility = Visibility.Visible;
            CardLayout.Visibility = Visibility.Collapsed;
            SmallCardLayout.Visibility = Visibility.Collapsed;
        }

        private void LoadArticleImage()
        {
            try
            {
                if (a.ArticleImage != null && a.ArticleImage.Length > 0)
                {
                    if (a.ArticleImage != null && a.ArticleImage.Length > 0)
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(a.ArticleImage);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        RowArticleImage.Source = bitmap;
                        CardArticleImage.Source = bitmap;
                        SmallArticleImage.Source = bitmap;
                    }
                }
                else
                {
                    // Set placeholder image
                    SetPlaceholderImage();
                }
            }
            catch
            {
                SetPlaceholderImage();
            }
        }

        private void SetPlaceholderImage()
        {
            // Just leave the image empty - no placeholder needed
            if (isCardMode)
            {
                CardArticleImage.Source = null;
            }
            else
            {
                RowArticleImage.Source = null;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            WAddArticle af = new WAddArticle(a, la, lf, lfo, main, 0, null, null);
            af.ShowDialog();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            WDeleteConfirmation df = new WDeleteConfirmation(a, la, null, main);
            df.ShowDialog();
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            WArticleDetails detailsWindow = new WArticleDetails(a, lf, lfo);
            detailsWindow.ShowDialog();
        }
    }
}