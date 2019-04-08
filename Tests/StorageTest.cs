using System;
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
        private readonly string destinationFolder;

        public StorageTest()
        {
            destinationFolder = Path.Combine(Path.GetTempPath(), "dnGrepTest", "TestFiles");
        }

        public void Dispose()
        {
            Directory.Delete(destinationFolder, true);
        }

        [Fact]
        public void TestSave()
        {
            GrepSettings storage = GrepSettings.Instance;
            storage.Clear();
            Assert.Empty(storage);
            storage["test"] = "hello";
            storage.Save(destinationFolder + "\\test.xml");
            Assert.True(File.Exists(destinationFolder + "\\test.xml"));
            Assert.True(new FileInfo(destinationFolder + "\\test.xml").Length > 10);
        }

        [Fact]
        public void TestLoad()
        {
            GrepSettings storage = GrepSettings.Instance;
            storage.Clear();
            Assert.Empty(storage);
            storage["test"] = "hello";
            storage.Save(destinationFolder + "\\test.xml");
            storage.Clear();
            Assert.Empty(storage);
            storage.Load(destinationFolder + "\\test.xml");
            Assert.True(storage["test"] == "hello");
        }

        [Fact]
        public void TestDataTypes()
        {
            GrepSettings storage = GrepSettings.Instance;
            storage.Clear();
            Assert.Empty(storage);
            storage.Set("size", 10);
            storage.Set("isTrue", true);
            storage.Save(destinationFolder + "\\test.xml");
            storage.Clear();
            Assert.Empty(storage);
            storage.Load(destinationFolder + "\\test.xml");
            Assert.Equal(10, storage.Get<int>("size"));
            Assert.True(storage.Get<bool>("isTrue"));
        }
    }
}
