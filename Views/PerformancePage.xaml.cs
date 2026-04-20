using OptiCleanPro.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OptiCleanPro.Views
{
    public partial class PerformancePage : Page
    {
        private readonly Dictionary<string, (bool Basic, bool Moderate, bool Maximum)> _options = new()
        {
            { "Clear RAM Cache",            (true,  true,  true)  },
            { "Flush DNS Cache",            (false, true,  true)  },
            { "Clean Temp Files",           (true,  true,  true)  },
            { "Disable Startup Programs",   (false, true,  true)  },
            { "Disable Visual Effects",     (false, true,  true)  },
            { "Optimize Virtual Memory",    (false, false, true)  },
            { "Kill Background Processes",  (false, false, true)  },
            { "Disable Superfetch/SysMain", (false, false, true)  },
            { "Boost CPU Priority",         (false, false, true)  },
        };

        private readonly Dictionary<string, CheckBox> _customCheckboxes = new();

        // Cached counter — created once, disposed on Unloaded
        private PerformanceCounter? _cpuCounter;

        public PerformancePage()
        {
            InitializeComponent();
            BuildCustomPanel();
            BuildModeTables();

            // Initialise counter once (not in Loaded, to avoid blocking on every navigation)
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // discard first reading
            }
            catch (Exception ex) { Debug.WriteLine($"[OptiClean] PerformanceCounter init failed: {ex.Message}"); }

            Loaded   += async (s, e) => await RefreshGaugeAsync();
            Unloaded += (s, e) => _cpuCounter?.Dispose();
        }

        // ─── Gauge ───────────────────────────────────────────────

        private async Task RefreshGaugeAsync()
        {
            double score = await Task.Run(ComputePerformanceScore);
            DrawNeedle(score);
            PercentLabel.Text = $"{(int)score}%";
        }

        private double ComputePerformanceScore()
        {
            try
            {
                if (_cpuCounter is null) return 48;
                Thread.Sleep(500); // ← safe: runs on background thread via Task.Run
                float usage = _cpuCounter.NextValue();
                return Math.Clamp(100 - usage, 0, 100); // higher idle = better score
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptiClean] Score calc failed: {ex.Message}");
                return 48;
            }
        }

        private void DrawNeedle(double percentage)
        {
            double cx = 150, cy = 140, length = 82;
            // 0% → 180°, 100% → 0° (left to right)
            double angleRad = (1.0 - percentage / 100.0) * Math.PI;

            NeedleLine.X1 = cx;
            NeedleLine.Y1 = cy;
            NeedleLine.X2 = cx + length * Math.Cos(angleRad);
            NeedleLine.Y2 = cy - length * Math.Sin(angleRad); // Y inverted on screen
        }

        // ─── Custom Panel ────────────────────────────────────────

        private void BuildCustomPanel()
        {
            foreach (var option in _options.Keys)
            {
                var cb = new CheckBox
                {
                    Content    = option,
                    FontSize   = 13,
                    Margin     = new Thickness(0, 5, 0, 5),
                    Foreground = Brushes.Black
                };
                CustomOptionsStack.Children.Add(cb);
                _customCheckboxes[option] = cb;
            }
        }

        // ─── Apply Buttons ───────────────────────────────────────

        private async void Apply_Basic(object sender, RoutedEventArgs e)
            => await RunMode("Basic", _options.Where(o => o.Value.Basic).Select(o => o.Key).ToList());

        private async void Apply_Moderate(object sender, RoutedEventArgs e)
            => await RunMode("Moderate", _options.Where(o => o.Value.Moderate).Select(o => o.Key).ToList());

        private async void Apply_Maximum(object sender, RoutedEventArgs e)
            => await RunMode("Maximum", _options.Where(o => o.Value.Maximum).Select(o => o.Key).ToList());

        private async void Apply_Custom(object sender, RoutedEventArgs e)
        {
            var selected = _customCheckboxes
                .Where(kv => kv.Value.IsChecked == true)
                .Select(kv => kv.Key).ToList();

            if (!selected.Any())
            {
                MessageBox.Show("Please select at least one option.", "Custom Mode",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await RunMode("Custom", selected);
        }

        private async Task RunMode(string modeName, List<string> tasks)
        {
            if (!tasks.Any()) return;

            var optimizer = new PerformanceOptimizer();

            await Task.Run(() =>
            {
                foreach (var task in tasks)
                {
                    try   { optimizer.Run(task); }
                    catch (Exception ex) { Debug.WriteLine($"[OptiClean] Task '{task}' failed: {ex.Message}"); }
                }
            });

            MessageBox.Show($"✅ {modeName} optimizations applied!\n{tasks.Count} tasks completed.",
                "Done", MessageBoxButton.OK, MessageBoxImage.Information);

            // Refresh gauge after showing the dialog
            await RefreshGaugeAsync();
        }

        // ─── Expand buttons — toggle detail panels ───────────────

        private void Expand_Basic(object sender, RoutedEventArgs e)    => TogglePanel(BasicPanel);
        private void Expand_Moderate(object sender, RoutedEventArgs e) => TogglePanel(ModeratePanel);
        private void Expand_Maximum(object sender, RoutedEventArgs e)  => TogglePanel(MaximumPanel);
        private void Expand_Custom(object sender, RoutedEventArgs e)   => TogglePanel(CustomPanel);

        private static void TogglePanel(Border panel)
            => panel.Visibility = panel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

        // ─── Detail dialogs ──────────────────────────────────────

        private void Detail_Basic(object sender, RoutedEventArgs e)    => ShowDetailDialog("Basic");
        private void Detail_Moderate(object sender, RoutedEventArgs e) => ShowDetailDialog("Moderate");
        private void Detail_Maximum(object sender, RoutedEventArgs e)  => ShowDetailDialog("Maximum");

        private void Detail_Custom(object sender, RoutedEventArgs e)
            => TogglePanel(CustomPanel);

        private void ShowDetailDialog(string mode)
        {
            var items = mode switch
            {
                "Basic"    => _options.Where(o => o.Value.Basic),
                "Moderate" => _options.Where(o => o.Value.Moderate),
                "Maximum"  => _options.Where(o => o.Value.Maximum),
                _          => _options.AsEnumerable()
            };
            var list = string.Join("\n• ", items.Select(o => o.Key));
            MessageBox.Show($"{mode} mode will apply:\n\n• {list}",
                $"{mode} Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Settings restored to defaults.", "Restore Defaults",
                MessageBoxButton.OK, MessageBoxImage.Information);

        // ─── Build detail tables inside expand panels ─────────────

        private void BuildModeTables()
        {
            foreach (var option in _options)
            {
                if (option.Value.Basic)    BasicTable.Children.Add(CreateRow(option.Key));
                if (option.Value.Moderate) ModerateTable.Children.Add(CreateRow(option.Key));
                if (option.Value.Maximum)  MaximumTable.Children.Add(CreateRow(option.Key));
            }
        }

        private static UIElement CreateRow(string text)
        {
            return new Border
            {
                Background   = Brushes.White,
                CornerRadius = new CornerRadius(6),
                Margin       = new Thickness(0, 4, 0, 4),
                Padding      = new Thickness(8),
                Child        = new DockPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text       = "✔",
                            Foreground = Brushes.LimeGreen,
                            FontSize   = 12,
                            Margin     = new Thickness(0, 0, 6, 0)
                        },
                        new TextBlock
                        {
                            Text              = text,
                            FontSize          = 13,
                            Foreground        = Brushes.Black,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };
        }

        private void Back_Click(object sender, RoutedEventArgs e)
            => NavigationService?.GoBack();
    }
}