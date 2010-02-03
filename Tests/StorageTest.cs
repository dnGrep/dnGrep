using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using dnGREP;
using System.IO;
using dnGREP.Common;
using System.Xml.Serialization;
using System.Collections.Specialized;

namespace Tests
{
	[TestFixture]
	public class StorageTest : TestBase
	{
		string sourceFolder;

		[FixtureSetUp]
		public void Initialize()
		{
			sourceFolder = Path.GetTempPath() + "TestFiles";
		}

		[FixtureTearDown]
		public void Cleanup()
		{
			Directory.Delete(sourceFolder, true);
		}

		[Test]
		public void TestSave()
		{
			GrepSettings storage = GrepSettings.Instance;
			storage.Clear();
			Assert.IsEmpty(storage);
			storage["test"] = "hello";
			storage.Save(sourceFolder + "\\test.xml");
			Assert.IsTrue(File.Exists(sourceFolder + "\\test.xml"));
			Assert.GreaterThan<long>(new FileInfo(sourceFolder + "\\test.xml").Length, 10);
		}

		[Test]
		public void TestLoad()
		{
			GrepSettings storage = GrepSettings.Instance;
			storage.Clear();
			Assert.IsEmpty(storage);
			storage["test"] = "hello";
			storage.Save(sourceFolder + "\\test.xml");
			storage.Clear();
			Assert.IsEmpty(storage);			
			storage.Load(sourceFolder + "\\test.xml");
			Assert.IsTrue(storage["test"] == "hello");
		}

		[Test]
		public void TestDataTypes()
		{
			GrepSettings storage = GrepSettings.Instance;
			storage.Clear();
			Assert.IsEmpty(storage);
			storage.Set<int>("size", 10);
			storage.Set<bool>("isTrue", true);
			storage.Save(sourceFolder + "\\test.xml");
			storage.Clear();
			Assert.IsEmpty(storage);
			storage.Load(sourceFolder + "\\test.xml");
			Assert.AreEqual<int>(storage.Get<int>("size"), 10);
			Assert.AreEqual<bool>(storage.Get<bool>("isTrue"), true);
		}
	}
}
