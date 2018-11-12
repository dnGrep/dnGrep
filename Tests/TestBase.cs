using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Tests
{
    public class TestBase
    {
        public string GetDllPath()
        {
            //Assembly thisAssembly = Assembly.GetAssembly(typeof(TestBase));
            //return Path.GetDirectoryName(thisAssembly.Location);
            //return @"D:\Sandbox\dnGrep\Tests";
            return Directory.GetCurrentDirectory();
        }
    }
}
