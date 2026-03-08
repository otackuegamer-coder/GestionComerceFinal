using Microsoft.Win32;
using GestionComerce.Main;
using GestionComerce.Main.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace GestionComerce.Settings
{
    public partial class PaymentMethodSettings : UserControl
    {
        private List<PaymentMethod> paymentMethods;
        public bool CanEditPaymentMethod { get; set; } = false;
        public bool CanDeletePaymentMethod { get; set; } = false;

        public PaymentMethodSettings(SettingsPage sp)
        {
            InitializeComponent();
            foreach (Role r in sp.lr)
            {
                if (sp.u.RoleID == r.RoleID)
                {
                    if (r.AddPaymentMethod == false)
                    {
                        btnAdd.IsEnabled = false;
                    }
                    CanEditPaymentMethod = r.ModifyPaymentMethod;
                    CanDeletePaymentMethod = r.DeletePaymentMethod;
                    if (r.AddPaymentMethod == true && r.ModifyPaymentMethod == true && r.DeletePaymentMethod == true)
                    {
                        LoadPaymentMethods();
                    }
                    break;
                }
            }
            this.DataContext = this;
        }

        private async void LoadPaymentMethods()
        {
            try
            {
                PaymentMethod pm = new PaymentMethod();
                paymentMethods = await pm.GetPaymentMethodsAsync();

                dgPaymentMethods.ItemsSource = null;
                dgPaymentMethods.ItemsSource = paymentMethods;

                // Show/hide empty state
                if (paymentMethods.Count == 0)
                {
                    dgPaymentMethods.Visibility = Visibility.Collapsed;
                    emptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    dgPaymentMethods.Visibility = Visibility.Visible;
                    emptyState.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des méthodes de paiement: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBrowseNewImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Sélectionner l'image de la méthode de paiement"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtNewImagePath.Text = openFileDialog.FileName;
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewPaymentMethod.Text))
            {
                MessageBox.Show("Veuillez entrer un nom pour la méthode de paiement.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPaymentMethod.Focus();
                return;
            }

            // Check for duplicates
            if (paymentMethods.Any(pm => pm.PaymentMethodName.Equals(txtNewPaymentMethod.Text.Trim(),
                StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Cette méthode de paiement existe déjà.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PaymentMethod newMethod = new PaymentMethod
                {
                    PaymentMethodName = txtNewPaymentMethod.Text.Trim(),
                    ImagePath = txtNewImagePath.Text.Trim()
                };

                int result = await newMethod.InsertPaymentMethodAsync();

                if (result > 0)
                {
                    WCongratulations wCongratulations = new WCongratulations("Ajout succes", "l'Ajout a ete effectue avec succes", 1);
                    wCongratulations.ShowDialog();

                    txtNewPaymentMethod.Clear();
                    txtNewImagePath.Clear();
                    LoadPaymentMethods();
                }
                else
                {
                    WCongratulations wCongratulations = new WCongratulations("Ajout échoué", "l'Ajout n'a pas ete effectue ", 0);
                    wCongratulations.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtNewPaymentMethod_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnAdd_Click(sender, e);
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.Tag is PaymentMethod method)
            {
                // Create edit dialog
                var editDialog = new Window
                {
                    Title = "Modifier la Méthode de Paiement",
                    Width = 520,
                    Height = 380,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true
                };

                // Main border with corner radius and border
                var outerBorder = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219)),
                    Margin = new Thickness(10)
                };

                var mainBorder = new Border
                {
                    Padding = new Thickness(30)
                };

                var stack = new StackPanel();

                // Title
                var titleBlock = new TextBlock
                {
                    Text = "Modifier la Méthode de Paiement",
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                    Margin = new Thickness(0, 0, 0, 24)
                };

                // Name field
                var nameLabel = new TextBlock
                {
                    Text = "Nom de la méthode:",
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var nameTextBox = new TextBox
                {
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 14,
                    Padding = new Thickness(12),
                    Height = 44,
                    Margin = new Thickness(0, 0, 0, 16),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                // Set the text AFTER creating the textbox and BEFORE applying template
                nameTextBox.Text = method.PaymentMethodName;

                // Apply simple rounded corner style without complex template
                nameTextBox.BorderThickness = new Thickness(1);
                nameTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));
                nameTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251));

                // Image field
                var imageLabel = new TextBlock
                {
                    Text = "Image/Icône:",
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var imageGrid = new Grid { Margin = new Thickness(0, 0, 0, 24) };
                imageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                imageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var imageTextBox = new TextBox
                {
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 14,
                    Padding = new Thickness(12),
                    Height = 44,
                    IsReadOnly = true,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                // Set the text AFTER creating the textbox
                imageTextBox.Text = method.ImagePath ?? "";

                Grid.SetColumn(imageTextBox, 0);

                var browsButton = new Button
                {
                    Content = "Parcourir",
                    Width = 100,
                    Height = 44,
                    Margin = new Thickness(12, 0, 0, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Apply simple button style
                browsButton.BorderThickness = new Thickness(0);
                browsButton.Cursor = Cursors.Hand;

                Grid.SetColumn(browsButton, 1);

                browsButton.Click += (s, ev) =>
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                        Title = "Sélectionner l'image"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        imageTextBox.Text = openFileDialog.FileName;
                    }
                };

                imageGrid.Children.Add(imageTextBox);
                imageGrid.Children.Add(browsButton);

                // Buttons
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                var btnCancel = new Button
                {
                    Content = "Annuler",
                    Width = 100,
                    Height = 44,
                    Margin = new Thickness(0, 0, 12, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                btnCancel.Click += (s, ev) => editDialog.DialogResult = false;

                var btnSave = new Button
                {
                    Content = "Enregistrer",
                    Width = 120,
                    Height = 44,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                btnSave.Click += (s, ev) => editDialog.DialogResult = true;

                buttonPanel.Children.Add(btnCancel);
                buttonPanel.Children.Add(btnSave);

                stack.Children.Add(titleBlock);
                stack.Children.Add(nameLabel);
                stack.Children.Add(nameTextBox);
                stack.Children.Add(imageLabel);
                stack.Children.Add(imageGrid);
                stack.Children.Add(buttonPanel);

                mainBorder.Child = stack;
                outerBorder.Child = mainBorder;
                editDialog.Content = outerBorder;

                // Set focus after dialog loads
                editDialog.Loaded += (s, ev) =>
                {
                    nameTextBox.Focus();
                    nameTextBox.SelectAll();
                };

                if (editDialog.ShowDialog() == true)
                {
                    string newName = nameTextBox.Text.Trim();
                    string newImagePath = imageTextBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("Le nom ne peut pas être vide.",
                            "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (paymentMethods.Any(pm => pm.PaymentMethodID != method.PaymentMethodID &&
                        pm.PaymentMethodName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Cette méthode de paiement existe déjà.",
                            "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        method.PaymentMethodName = newName;
                        method.ImagePath = newImagePath;
                        int result = await method.UpdatePaymentMethodAsync();

                        if (result > 0)
                        {
                            WCongratulations wCongratulations = new WCongratulations("Modification avec succes", "Modification a ete effectue avec succes", 1);
                            wCongratulations.ShowDialog();
                            LoadPaymentMethods();
                        }
                        else
                        {
                            WCongratulations wCongratulations = new WCongratulations("Modification échoué", "Modification n'a pas ete effectue ", 0);
                            wCongratulations.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur: {ex.Message}",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ApplyRoundedTextBoxTemplate(TextBox textBox)
        {
            var template = new ControlTemplate(typeof(TextBox));
            var factory = new FrameworkElementFactory(typeof(Border));

            factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            factory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            factory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.NameProperty, "PART_ContentHost");
            scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(12, 0, 12, 0));
            scrollViewer.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);

            factory.AppendChild(scrollViewer);
            template.VisualTree = factory;

            textBox.Template = template;
        }

        private void ApplyButtonTemplateEdit(Button button)
        {
            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));

            factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            factory.AppendChild(presenter);
            template.VisualTree = factory;

            button.Template = template;
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.Tag is PaymentMethod method)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer la méthode '{method.PaymentMethodName}'?\n\n" +
                    "Cette action est irréversible et peut affecter les transactions existantes.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        int deleteResult = await method.DeletePaymentMethodAsync();

                        if (deleteResult > 0)
                        {
                            WCongratulations wCongratulations = new WCongratulations("Suppression avec succes", "Suppression a ete effectue avec succes", 1);
                            wCongratulations.ShowDialog();
                            LoadPaymentMethods();
                        }
                        else
                        {
                            WCongratulations wCongratulations = new WCongratulations("Suppression échoué", "Suppression n'a pas ete effectue ", 0);
                            wCongratulations.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur: {ex.Message}\n\n" +
                            "Cette méthode de paiement est peut-être utilisée dans d'autres enregistrements.",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DgPaymentMethods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle selection if needed
        }
    }
}