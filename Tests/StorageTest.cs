using System;
using System.IO;
using dnGREP.Common;
using Xunit;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Tests
{

    public class StorageTest : TestBase, IDisposable
    {
        string sourceFolder;

        public StorageTest()
        {
            sourceFolder = Path.GetTempPath() + "TestFiles";
        }

        public void Dispose()
        {
            Directory.Delete(sourceFolder, true);
        }

        [Fact]
        public void TestSave()
        {
            GrepSettings storage = GrepSettings.Instance;
            storage.Clear();
            Assert.Empty(storage);
            storage["test"] = "hello";
            storage.Save(sourceFolder + "\\test.xml");
            Assert.True(File.Exists(sourceFolder + "\\test.xml"));
            Assert.True(new FileInfo(sourceFolder + "\\test.xml").Length > 10);
        }

        [Fact]
        public void TestLoad()
        {
            GrepSettings storage = GrepSettings.Instance;
            storage.Clear();
            Assert.Empty(storage);
            storage["test"] = "hello";
            storage.Save(sourceFolder + "\\test.xml");
            storage.Clear();
            Assert.Empty(storage);
            storage.Load(sourceFolder + "\\test.xml");
            Assert.True(storage["test"] == "hello");
        }

        [Fact]
        public void TestDataTypes()
        {
            GrepSettings storage = GrepSettings.Instance;
            storage.Clear();
            Assert.Empty(storage);
            storage.Set<int>("size", 10);
            storage.Set<bool>("isTrue", true);
            storage.Save(sourceFolder + "\\test.xml");
            storage.Clear();
            Assert.Empty(storage);
            storage.Load(sourceFolder + "\\test.xml");
            Assert.Equal<int>(storage.Get<int>("size"), 10);
            Assert.Equal<bool>(storage.Get<bool>("isTrue"), true);
        }
    }
}
