using OptiCleanPro.Views;
using System.Reflection;
using System.Windows;

namespace OptiCleanPro
{
    public partial class MainWindow : Window
    {
        // ── Cached page instances — created once, reused on every nav ──
        private readonly DashboardPage   _dashboard   = new();
        private readonly StoragePage     _storage     = new();
        private readonly SecurityPage    _security    = new();
        private readonly PerformancePage _performance = new();

        public MainWindow()
        {
            InitializeComponent();

            // Show version from assembly metadata — no hard-coded string
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            VersionLabel.Text = $"v{ver?.Major}.{ver?.Minor}.{ver?.Build}";

            MainFrame.Navigate(_dashboard);
        }

        private void Nav_Storage(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(_storage);

        private void Nav_Security(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(_security);

        private void Nav_Performance(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(_performance);

        private void Nav_Dashboard(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(_dashboard);
    }
}