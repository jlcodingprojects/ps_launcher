using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace Launcher
{
    public partial class LauncherWindow : Window
    {
        private readonly string scriptsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Launcher");

        private readonly EventHandler deactivatedHandler;
        public bool isClosing = false;

        public LauncherWindow()
        {
            InitializeComponent();

            this.UseLayoutRounding = true;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

            // Create scripts directory if it doesn't exist
            Directory.CreateDirectory(scriptsPath);

            // Load scripts and create UI asynchronously
            _ = RefreshScriptsListAsync();

            this.Left = 0;
            this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 40;

            deactivatedHandler = (s, e) =>
            {
                if (!isClosing)
                {
                    isClosing = true;
                    this.Close();
                }
            };
            this.Deactivated += deactivatedHandler;
        }

        private async Task RefreshScriptsListAsync()
        {
            var loadingIndicator = new TextBlock
            {
                Text = "Loading scripts...",
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            MainStackPanel.Children.Clear();
            MainStackPanel.Children.Add(loadingIndicator);

            try
            {
                // Create modern button style
                var buttonStyle = new Style(typeof(Button));
                buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 45, 48))));
                buttonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
                buttonStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
                buttonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(12, 6, 12, 6)));
                buttonStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(0, 0, 0, 0)));
                buttonStyle.Setters.Add(new Setter(Button.FontSizeProperty, 13.0));
                buttonStyle.Setters.Add(new Setter(Button.FontWeightProperty, FontWeights.Normal));
                buttonStyle.Setters.Add(new Setter(Button.MinWidthProperty, 80.0));
                buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, CreateButtonTemplate()));

                // Create a container for scripts
                var scriptsContainer = new StackPanel
                {
                    Margin = new Thickness(10),
                };

                // Load files asynchronously
                string[] scripts = await Task.Run(() => Directory.GetFiles(scriptsPath, "*.ps1"));

                foreach (string script in scripts)
                {
                    var scriptPanel = new Border
                    {
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(20, 14, 20, 14),
                        Background = new LinearGradientBrush
                        {
                            StartPoint = new Point(0, 0),
                            EndPoint = new Point(1, 1),
                            GradientStops = new GradientStopCollection
                            {
                                new GradientStop(Color.FromRgb(40, 40, 43), 0.0),
                                new GradientStop(Color.FromRgb(35, 35, 38), 1.0)
                            }
                        },
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            BlurRadius = 10,
                            ShadowDepth = 2,
                            Opacity = 0.2,
                            Color = Colors.Black
                        },
                        Cursor = Cursors.Hand
                    };

                    // Add mouse enter/leave events for hover effect
                    scriptPanel.MouseEnter += (s, e) =>
                    {
                        if (scriptPanel.Background is LinearGradientBrush brush)
                        {
                            brush.GradientStops[0].Color = Color.FromRgb(45, 45, 48);
                            brush.GradientStops[1].Color = Color.FromRgb(40, 40, 43);
                        }
                        if (scriptPanel.Effect is System.Windows.Media.Effects.DropShadowEffect shadow)
                        {
                            shadow.BlurRadius = 15;
                            shadow.Opacity = 0.3;
                        }
                    };

                    scriptPanel.MouseLeave += (s, e) =>
                    {
                        if (scriptPanel.Background is LinearGradientBrush brush)
                        {
                            brush.GradientStops[0].Color = Color.FromRgb(40, 40, 43);
                            brush.GradientStops[1].Color = Color.FromRgb(35, 35, 38);
                        }
                        if (scriptPanel.Effect is System.Windows.Media.Effects.DropShadowEffect shadow)
                        {
                            shadow.BlurRadius = 10;
                            shadow.Opacity = 0.2;
                        }
                    };

                    var innerPanel = new Grid();
                    innerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    innerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // Add mouse click event to run the script
                    scriptPanel.MouseLeftButtonDown += (s, e) => RunScript(script);

                    var scriptName = new TextBlock
                    {
                        Text = Path.GetFileName(script),
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.White,
                        FontSize = 14,
                        FontWeight = FontWeights.Medium
                    };
                    Grid.SetColumn(scriptName, 0);

                    var editButton = new Button
                    {
                        Content = "Edit",
                        Style = buttonStyle,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };
                    Grid.SetColumn(editButton, 1);
                    editButton.Click += (s, e) => EditScript(script);

                    innerPanel.Children.Add(scriptName);
                    innerPanel.Children.Add(editButton);
                    scriptPanel.Child = innerPanel;
                    scriptsContainer.Children.Add(scriptPanel);
                }

                MainStackPanel.Children.Add(scriptsContainer);

                // Update "Add New Script" button
                var addNewButton = new Button
                {
                    Content = "Add New Script",
                    Style = buttonStyle,
                    Margin = new Thickness(10),
                    Padding = new Thickness(20, 14, 20, 14),
                };

                addNewButton.Click += (s, e) => CreateNewScriptAsync();

                var openFolderButton = new Button
                {
                    Content = "Open Scripts Folder",
                    Style = buttonStyle,

                    Margin = new Thickness(10),
                    Padding = new Thickness(20, 14, 20, 14),
                };

                openFolderButton.Click += (s, e) => OpenScriptsFolder();

                // Add mouse enter/leave events for hover effect
                addNewButton.MouseEnter += (s, e) =>
                {
                    if (addNewButton.Background is LinearGradientBrush brush)
                    {
                        brush.GradientStops[0].Color = Color.FromRgb(92, 42, 125);
                        brush.GradientStops[1].Color = Color.FromRgb(76, 35, 98);
                    }
                };

                addNewButton.MouseLeave += (s, e) =>
                {
                    if (addNewButton.Background is LinearGradientBrush brush)
                    {
                        brush.GradientStops[0].Color = Color.FromRgb(82, 37, 110);
                        brush.GradientStops[1].Color = Color.FromRgb(66, 30, 88);
                    }
                };

                MainStackPanel.Children.Add(addNewButton);
                MainStackPanel.Children.Add(openFolderButton);
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error refreshing scripts list: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                MainStackPanel.Children.Remove(loadingIndicator);
            }
        }

        private void RunScript(string scriptPath)
        {
            try
            {
                if (!isClosing)
                {
                    isClosing = true;
                    Process.Start("powershell.exe", $"-ExecutionPolicy Bypass -File \"{scriptPath}\"");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                isClosing = false;  // Reset the flag if there's an error
                MessageBox.Show($"Error running script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditScript(string scriptPath)
        {
            try
            {
                if (!isClosing)
                {
                    var path = scriptPath.Replace("\\", "/");
                    isClosing = true;
                    Process.Start("C:\\Users\\User\\AppData\\Local\\Programs\\cursor\\cursor.exe", $"\"{scriptPath}\"");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                isClosing = false;  // Reset the flag if there's an error
                MessageBox.Show($"Error editing script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenScriptsFolder()
        {
            Process.Start("explorer.exe", scriptsPath);
        }

        private async Task CreateNewScriptAsync()
        {
            try
            {
                // Temporarily remove the Deactivated event handler
                this.Deactivated -= deactivatedHandler;

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    InitialDirectory = scriptsPath,
                    Filter = "PowerShell Scripts (*.ps1)|*.ps1",
                    DefaultExt = ".ps1",
                    FileName = "NewScript"
                };

                bool? result = dialog.ShowDialog(this);

                if (result == true)
                {
                    try
                    {
                        File.WriteAllText(dialog.FileName, "# New PowerShell Script");
                        await RefreshScriptsListAsync();
                        EditScript(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating script file: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Re-add the Deactivated event handler
                this.Deactivated += deactivatedHandler;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening save dialog: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(this);

            // Calculate angle from center to mouse position
            double centerX = this.ActualWidth / 2;
            double centerY = this.ActualHeight / 2;
            double offsetX = mousePosition.X / this.ActualWidth;
            double offsetY = mousePosition.Y / this.ActualHeight;
            double angle = Math.Atan2(offsetY - 0.5, offsetX - 0.5);

            // Calculate four equidistant points around the circle
            double radius = 0.5; // Controls how far the gradient points move from center
            double[] angles = {
                angle,              // Mouse angle
                angle + Math.PI/2,  // 90 degrees clockwise
                angle + Math.PI,    // 180 degrees (opposite)
                angle + 3*Math.PI/2 // 270 degrees clockwise
            };

            if (sender is Border border && border.Background is LinearGradientBrush gradientBrush)
            {
                gradientBrush.GradientStops.Clear();
                for (int i = 0; i < angles.Length; i++)
                {
                    double x = 0.5 + Math.Cos(angles[i]) * radius;
                    double y = 0.5 + Math.Sin(angles[i]) * radius;
                    gradientBrush.GradientStops.Add(new GradientStop(
                        Color.FromRgb((byte)(35 + (i * 5)), (byte)(35 + (i * 5)), (byte)(35 + (i * 5))),
                        i / (angles.Length - 1.0)
                    ));
                }

                // Use first and third points (opposite points) for start/end
                gradientBrush.StartPoint = new Point(
                    0.5 + Math.Cos(angles[0]) * radius,
                    0.5 + Math.Sin(angles[0]) * radius
                );
                gradientBrush.EndPoint = new Point(
                    0.5 + Math.Cos(angles[2]) * radius,
                    0.5 + Math.Sin(angles[2]) * radius
                );
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PositionWindowAtBottom();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionWindowAtBottom();
        }

        private void PositionWindowAtBottom()
        {
            double screenHeight = SystemParameters.WorkArea.Bottom;
            this.Top = screenHeight - this.ActualHeight;
        }

        // Add this new method to create a modern button template
        private ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.MarginProperty, new Thickness(2));

            border.AppendChild(contentPresenter);
            template.VisualTree = border;

            // Add triggers for hover effect
            var triggerCollection = new ControlTemplate();
            //var triggerCollection = new ControlTemplate.TriggerCollection();

            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(55, 55, 58))));
            trigger.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));

            template.Triggers.Add(trigger);

            return template;
        }
    }
}
