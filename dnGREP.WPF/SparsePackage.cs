using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using Windows.Management.Deployment;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace dnGREP.WPF
{
    internal static class SparsePackage
    {
        public static bool CanRegisterPackage =>
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0);

        public static void RegisterSparsePackage(bool reregister)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0))
            {
                if (reregister && IsRegistered)
                {
                    RemoveSparsePackage();
                }

                if (!IsRegistered)
                {
                    string exePath = AppDomain.CurrentDomain.BaseDirectory;
                    string externalLocation = Path.Combine(exePath, @"");
                    string sparsePkgPath = Path.Combine(exePath, @"dnGrep.msix");

                    // Attempt registration
                    if (RegisterSparsePackage(externalLocation, sparsePkgPath))
                    {
                        Debug.WriteLine("Package Registration succeeded!");
                    }
                    else
                    {
                        Debug.WriteLine("Package Registration failed.");
                    }
                }
            }
        }

        public static bool IsRegistered
        {
            get
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0))
                {
                    PackageManager packageManager = new();
                    return packageManager.FindPackagesForUser(string.Empty, "dnGrep_wbnnev551gwxy").Any();
                }
                return false;
            }
        }

        [SupportedOSPlatform("windows10.0.19041")]
        unsafe private static bool RegisterSparsePackage(string externalLocation, string sparsePkgPath)
        {
            bool registration = false;
            try
            {
                Uri externalUri = new(externalLocation);
                Uri packageUri = new(sparsePkgPath);

                Debug.WriteLine($"exe Location {externalLocation}");
                Debug.WriteLine($"msix Address {sparsePkgPath}");

                Debug.WriteLine($"  exe Uri {externalUri}");
                Debug.WriteLine($"  msix Uri {packageUri}");

                PackageManager packageManager = new();

                // Declare use of an external location
                AddPackageOptions options = new()
                {
                    ExternalLocationUri = externalUri
                };

                Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation =
                    packageManager.AddPackageByUriAsync(packageUri, options);

                // this event will be signaled when the deployment operation has completed.
                ManualResetEvent opCompletedEvent = new(false);

                deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

                Debug.WriteLine($"Installing package {sparsePkgPath}");

                Debug.WriteLine("Waiting for package registration to complete...");

                opCompletedEvent.WaitOne();

                if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Error)
                {
                    DeploymentResult deploymentResult = deploymentOperation.GetResults();
                    Debug.WriteLine($"Installation Error: {deploymentOperation.ErrorCode}");
                    Debug.WriteLine($"Detailed Error Text: {deploymentResult.ErrorText}");

                }
                else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Canceled)
                {
                    Debug.WriteLine("Package Registration Canceled");
                }
                else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Completed)
                {
                    registration = true;
                    Debug.WriteLine("Package Registration succeeded!");

                    // Notify the shell about the change
                    PInvoke.SHChangeNotify(SHCNE_ID.SHCNE_ASSOCCHANGED, SHCNF_FLAGS.SHCNF_IDLIST);
                }
                else
                {
                    Debug.WriteLine("Installation status unknown");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddPackageSample failed, error message: {ex.Message}");
                Debug.WriteLine($"Full StackTrace: {ex}");

                return registration;
            }

            return registration;
        }

        public static void RemoveSparsePackage()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0))
            {
                PackageManager packageManager = new();
                var myPackage = packageManager.FindPackagesForUser(string.Empty, "dnGrep_wbnnev551gwxy").FirstOrDefault();

                if (myPackage != null)
                {
                    Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress>
                        deploymentOperation = packageManager.RemovePackageAsync(myPackage.Id.FullName);
                    // this event will be signaled when the deployment operation has completed.
                    ManualResetEvent opCompletedEvent = new(false);

                    deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

                    Debug.WriteLine("Uninstalling package..");
                    opCompletedEvent.WaitOne();
                    Debug.WriteLine("Uninstall complete!");
                }
                else
                {
                    Debug.WriteLine("Package not found for uninstall");
                }
            }
        }
    }
}
