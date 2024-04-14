using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NLog;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace dnGREP.WPF
{
    internal static class SparsePackage
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // Note: Windows 11 is required for Add-AppxPackage with the -ExternalLocation argument

        public static bool CanRegisterPackage =>
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000, 0);

        public static bool IsRegistered
        {
            get
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000, 0))
                {
                    bool result = Powershell("Get-AppxPackage -Name dnGrep | findstr dnGrep_wbnnev551gwxy", true);
                    return result;
                }
                return false;
            }
        }

        public unsafe static void RegisterSparsePackage(bool reregister)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000, 0))
            {
                if (reregister && IsRegistered)
                {
                    RemoveSparsePackage();
                }

                if (!IsRegistered)
                {
                    string exePath = AppDomain.CurrentDomain.BaseDirectory;
                    string sparsePkgPath = Path.Combine(exePath, @"dnGREP.msix");
                    string externalLocation = exePath.TrimEnd(Path.DirectorySeparatorChar);

                    // Attempt registration
                    bool result = Powershell($"Add-AppxPackage -Path \"\"\"{sparsePkgPath}\"\"\" -ExternalLocation \"\"\"{externalLocation}\"\"\"");

                    if (result)
                    {
                        Debug.WriteLine("Add AppxPackage succeeded.");

                        // Notify the shell about the change
                        PInvoke.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_IDLIST);
                    }
                    else
                    {
                        Debug.WriteLine("Add AppxPackage failed.");
                    }
                }
            }
        }

        public unsafe static void RemoveSparsePackage()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000, 0))
            {
                if (Powershell("Get-AppxPackage -Name dnGrep | Remove-AppxPackage"))
                {
                    Debug.WriteLine("Remove AppxPackage complete.");

                    // Notify the shell about the change
                    PInvoke.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_IDLIST);
                }
                else
                {
                    Debug.WriteLine("Remove AppxPackage failed.");
                }
            }
        }

        private static bool Powershell(string arguments, bool successIfStdOutIsNotEmpty = false)
        {
            try
            {
                using Process ps = new();
                ps.StartInfo.FileName = "powershell.exe";
                ps.StartInfo.Arguments = "-Command " + arguments;
                ps.EnableRaisingEvents = true;
                ps.StartInfo.RedirectStandardOutput = true;
                ps.StartInfo.RedirectStandardError = true;
                // Must not set true to execute PowerShell command
                ps.StartInfo.UseShellExecute = false;
                ps.StartInfo.CreateNoWindow = true;
                ps.Start();
                using var o = ps.StandardOutput;
                using var e = ps.StandardError;
                var standardOutput = o.ReadToEnd();
                var standardError = e.ReadToEnd();

                if (!string.IsNullOrEmpty(standardError))
                {
                    logger.Error("Powershell.exe -Command {0} error:{2}{1}", arguments, standardError, Environment.NewLine);
                }

                if (successIfStdOutIsNotEmpty)
                {
                    return !string.IsNullOrEmpty(standardOutput);
                }
                else
                {
                    return ps.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }
}
