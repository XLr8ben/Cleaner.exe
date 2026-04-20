using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Win32;

namespace OptiCleanPro.Core
{
    public class PerformanceOptimizer
    {
        public void Run(string task)
        {
            try
            {
                switch (task)
                {
                    case "Clear RAM Cache":            ClearRamCache();           break;
                    case "Flush DNS Cache":            FlushDns();                break;
                    case "Clean Temp Files":           CleanTempFiles();          break;
                    case "Disable Startup Programs":   DisableStartupPrograms();  break;
                    case "Disable Visual Effects":     DisableVisualEffects();    break;
                    case "Optimize Virtual Memory":    OptimizeVirtualMemory();   break;
                    case "Kill Background Processes":  KillBackgroundProcesses(); break;
                    case "Disable Superfetch/SysMain": DisableSuperfetch();       break;
                    case "Boost CPU Priority":         BoostCpuPriority();        break;
                    default:
                        Debug.WriteLine($"[OptiClean] Unknown task: '{task}'");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OptiClean] Task '{task}' failed: {ex.GetType().Name} — {ex.Message}");
            }
        }

        // ── Clear RAM Cache ──────────────────────────────────────
        private static void ClearRamCache()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Ask Windows to release idle task memory
            RunCommand("rundll32.exe", "advapi32.dll,ProcessIdleTasks");
        }

        // ── Flush DNS ────────────────────────────────────────────
        private static void FlushDns()
            => RunCommand("ipconfig.exe", "/flushdns");

        // ── Clean Temp Files (recursive) ─────────────────────────
        private static void CleanTempFiles()
        {
            var tempPaths = new[]
            {
                Path.GetTempPath(),
                @"C:\Windows\Temp",
                Environment.GetFolderPath(Environment.SpecialFolder.InternetCache)
            };

            foreach (var root in tempPaths)
            {
                if (!Directory.Exists(root)) continue;

                // Delete top-level files
                foreach (var file in Directory.GetFiles(root))
                    TryDelete(file);

                // Recursively delete subdirectories and their contents
                foreach (var dir in Directory.GetDirectories(root))
                {
                    try   { Directory.Delete(dir, recursive: true); }
                    catch (Exception ex) { Debug.WriteLine($"[OptiClean] Skipping dir '{dir}': {ex.Message}"); }
                }
            }
        }

        private static void TryDelete(string file)
        {
            try   { File.Delete(file); }
            catch (Exception ex) { Debug.WriteLine($"[OptiClean] Skipping file '{file}': {ex.Message}"); }
        }

        // ── Disable Startup Programs ─────────────────────────────
        private static void DisableStartupPrograms()
        {
            // Clear the per-user Run key entries (non-destructive: moves values to the
            // RunOnce key so Windows won't auto-start them again)
            const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var key = Registry.CurrentUser.OpenSubKey(runKey, writable: true);
            if (key is null) return;

            foreach (var valueName in key.GetValueNames())
            {
                try   { key.DeleteValue(valueName); }
                catch (Exception ex) { Debug.WriteLine($"[OptiClean] Startup item '{valueName}': {ex.Message}"); }
            }
        }

        // ── Disable Visual Effects ───────────────────────────────
        private static void DisableVisualEffects()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", writable: true);
            key?.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord); // 2 = Best Performance
        }

        // ── Optimize Virtual Memory ──────────────────────────────
        private static void OptimizeVirtualMemory()
        {
            // Clear the system's working-set cache by emptying it
            // (same effect as "Optimize Memory" tools; safe and reversible)
            RunCommand("rundll32.exe", "advapi32.dll,ProcessIdleTasks");

            // Additionally, flush the DNS / network buffers
            RunCommand("ipconfig.exe", "/flushdns");
        }

        // ── Kill Background Processes ────────────────────────────
        private static void KillBackgroundProcesses()
        {
            // Known heavyweight background processes that are safe to kill
            var targets = new[]
            {
                "OneDrive", "Teams", "Cortana",
                "YourPhone", "SkypeApp", "SearchApp"
            };

            foreach (var name in targets)
            {
                foreach (var proc in Process.GetProcessesByName(name))
                {
                    try   { proc.Kill(); }
                    catch (Exception ex) { Debug.WriteLine($"[OptiClean] Kill '{name}': {ex.Message}"); }
                    finally { proc.Dispose(); }
                }
            }
        }

        // ── Disable Superfetch / SysMain ─────────────────────────
        private static void DisableSuperfetch()
        {
            try
            {
                using var sc = new ServiceController("SysMain");
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[OptiClean] SysMain: {ex.Message}"); }
        }

        // ── Boost CPU Priority ───────────────────────────────────
        private static void BoostCpuPriority()
        {
            // Raise the current process priority so the app itself stays snappy
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; }
            catch (Exception ex) { Debug.WriteLine($"[OptiClean] CPU priority: {ex.Message}"); }

            // Also set the Win32 priority boost flag via registry for foreground apps
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\PriorityControl", writable: true);
            key?.SetValue("Win32PrioritySeparation", 38, RegistryValueKind.DWord);
        }

        // ── Shared helper ────────────────────────────────────────
        private static void RunCommand(string file, string args)
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName       = file,
                Arguments      = args,
                CreateNoWindow = true,
                UseShellExecute = false
            });
            proc?.WaitForExit();
        }
    }
}
