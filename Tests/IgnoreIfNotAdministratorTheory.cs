using System;
using System.Security.Principal;
using Xunit;

namespace Tests
{
    public class IgnoreIfNotAdministratorTheory : TheoryAttribute
    {
        public IgnoreIfNotAdministratorTheory()
        {
            if (!IsUserAdministrator())
            {
                Skip = "Ignore when not running as Admin";
            }
        }

        private static bool IsUserAdministrator()
        {
            bool isAdmin;
            WindowsIdentity? user = null;
            try
            {
                //get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            finally
            {
                if (user != null)
                    user.Dispose();
            }
            return isAdmin;
        }
    }
}
