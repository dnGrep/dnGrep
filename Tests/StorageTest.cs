using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using dnGREP;
using System.IO;
using dnGREP.Common;
using System.Xml.Serialization;
using System.Collections.Specialized;

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
