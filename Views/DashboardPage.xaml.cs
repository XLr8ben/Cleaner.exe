using OptiCleanPro.Views;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OptiCleanPro.Views
{
    public partial class DashboardPage : Page
    {
        private readonly DispatcherTimer _timer;
        private readonly PerformanceCounter _cpuCounter;

        // P/Invoke for real memory %
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint  dwLength;
            public uint  dwMemoryLoad;   // directly gives memory usage %
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public DashboardPage()
        {
            InitializeComponent();

            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // First call always returns 0 — discard it

            // Refresh metrics every 2 seconds
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (s, e) => RefreshMetrics();
            _timer.Start();

            // Clean up native resources when this page is navigated away from
            Unloaded += (s, e) =>
            {
                _timer.Stop();
                _cpuCounter.Dispose();
            };
        }

        private void RefreshMetrics()
        {
            // CPU
            float cpu = _cpuCounter.NextValue();
            CpuBar.Value   = cpu;
            CpuLabel.Text  = $"{(int)cpu}%";

            // Memory via Win32
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
            if (GlobalMemoryStatusEx(ref mem))
            {
                MemBar.Value  = mem.dwMemoryLoad;
                MemLabel.Text = $"{mem.dwMemoryLoad}%";
                MemBar.Foreground = mem.dwMemoryLoad > 70
                    ? System.Windows.Media.Brushes.OrangeRed
                    : System.Windows.Media.Brushes.LimeGreen;
            }

            // Storage (C: drive)
            try
            {
                var drive   = new DriveInfo("C");
                double used = (1.0 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100;
                StorageBar.Value  = used;
                StorageLabel.Text = $"{(int)used}%";
            }
            catch (Exception ex) { Debug.WriteLine($"[OptiClean] Storage metric failed: {ex.Message}"); }
        }

        private void Nav_Performance(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new PerformancePage());

        private void Nav_Security(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new SecurityPage());
    }
}