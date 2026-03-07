using System;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using Xunit;

namespace Tests
{
    public class IgnoreIfNotAdministratorTheory : TheoryAttribute
    {
        public IgnoreIfNotAdministratorTheory(
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = 0)
            : base(sourceFilePath, sourceLineNumber)
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
                WindowsPrincipal principal = new(user);
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
                user?.Dispose();
            }
            return isAdmin;
        }
    }
}
