using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Management.Deployment;

namespace dnGREP.WPF
{
    internal static class SparsePackage
    {
        public static void RegisterSparsePackage()
        {
            if (!IsRegistered())
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string externalLocation = Path.Combine(exePath, @"");
                string sparsePkgPath = Path.Combine(exePath, @"dnGrep.msix");

                //Attempt registration
                if (RegisterSparsePackage(externalLocation, sparsePkgPath))
                {
                    //Registration succeeded, restart the app to run with identity
                    Debug.WriteLine("Package Registration succeeded!");
                }
                else //Registration failed, run without identity
                {
                    Debug.WriteLine("Package Registration failed.");
                }
            }
        }

        public static bool IsRegistered()
        {
            PackageManager packageManager = new();
            return packageManager.FindPackagesForUser(string.Empty, "dnGrep_h91ms92gdsmmt").Any();
        }

        [DllImport("Shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x8000000;
        private const uint SHCNF_IDLIST = 0x0;

        private static bool RegisterSparsePackage(string externalLocation, string sparsePkgPath)
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

                //Declare use of an external location
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
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
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
            PackageManager packageManager = new();
            var myPackage = packageManager.FindPackagesForUser(string.Empty, "dnGrep_h91ms92gdsmmt").FirstOrDefault();

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
