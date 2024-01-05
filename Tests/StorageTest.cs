using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnGREP.Common;
using Xunit;

namespace Tests
{

    public class StorageTest : TestBase, IDisposable
    {
        private readonly string sourceFolder;
        private readonly string destinationFolder;

        public StorageTest()
        {
            sourceFolder = Path.Combine(GetDllPath(), "Files");
            destinationFolder = Path.Combine(Path.GetTempPath(), "dnGrepTest", Guid.NewGuid().ToString());
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
        }

        public void Dispose()
        {
            Directory.Delete(destinationFolder, true);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void TestSave()
        {
            if (Activator.CreateInstance(typeof(GrepSettings), true) is GrepSettings storage)
            {
                storage.Clear();
                Assert.Equal(0, storage.Count);
                storage.Set("test", "hello");
                storage.Save(destinationFolder + "\\test.xml");
                Assert.True(File.Exists(destinationFolder + "\\test.xml"));
                Assert.True(new FileInfo(destinationFolder + "\\test.xml").Length > 10);
            }
            else
            {
                Assert.Fail("Could not create instance of GrepSettings");
            }
        }

        [Fact]
        public void TestLoad()
        {
            if (Activator.CreateInstance(typeof(GrepSettings), true) is GrepSettings storage)
            {
                storage.Clear();
                Assert.Equal(0, storage.Count);
                storage.Set("test", "hello");
                storage.Save(destinationFolder + "\\test.xml");
                storage.Clear();
                Assert.Equal(0, storage.Count);
                storage.Load(destinationFolder + "\\test.xml");
                Assert.Equal("hello", storage.Get<string>("test"));
            }
            else
            {
                Assert.Fail("Could not create instance of GrepSettings");
            }
        }

        [Fact]
        public void TestDataTypes()
        {
            if (Activator.CreateInstance(typeof(GrepSettings), true) is GrepSettings storage)
            {
                storage.Clear();
                Assert.Equal(0, storage.Count);
                storage.Set("size", 10);
                storage.Set("isTrue", true);
                DateTime? start = null;
                storage.Set("startDate", start);
                DateTime? end = new(2023, 02, 28, 16, 14, 12, DateTimeKind.Local);
                storage.Set("endDate", end);
                bool? indetermnate = null;
                storage.Set("indetermnate", indetermnate);
                long num = 35000L;
                storage.Set("longNum", num);

                storage.Save(destinationFolder + "\\test.xml");
                storage.Clear();
                Assert.Equal(0, storage.Count);
                storage.Load(destinationFolder + "\\test.xml");

                Assert.Equal(10, storage.Get<int>("size"));
                Assert.True(storage.Get<bool>("isTrue"));
                Assert.Null(storage.GetNullable<DateTime?>("startDate"));
                Assert.Equal(end, storage.GetNullable<DateTime?>("endDate"));
                Assert.Null(storage.GetNullable<bool?>("indetermnate"));
                Assert.Equal(num, storage.Get<long>("longNum"));
                Assert.Equal(4000, storage.Get<long>(GrepSettings.Key.PreviewLargeFileLimit));
            }
            else
            {
                Assert.Fail("Could not create instance of GrepSettings");
            }
        }

        [Fact]
        public void TestConvertFromV1toV3()
        {
            if (Activator.CreateInstance(typeof(GrepSettings), true) is GrepSettings storage)
            {
                storage.Clear();

                string file = Path.Combine(sourceFolder, "Settings", "version1", "dnGREP.Settings.dat");
                storage.Load(file);
                // these are called when plugins are loaded
                storage.ConvertExtensionsToV3("Word", ["doc"]);
                storage.ConvertExtensionsToV3("Pdf", ["pdf"]);
                storage.ConvertExtensionsToV3("Openxml", ["docx", "docm", "xls", "xlsx", "xlsm", "pptx", "pptm"]);

                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.CustomEditor));
                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.CustomEditorArgs));
                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.EscapeQuotesInMatchArgument));
                Assert.True(storage.ContainsKey(GrepSettings.Key.CustomEditors));

                List<CustomEditor> list = storage.Get<List<CustomEditor>>(GrepSettings.Key.CustomEditors);

                Assert.NotNull(list);
                Assert.NotEmpty(list);
                Assert.Single(list);

                CustomEditor editor = list[0];
                Assert.NotNull(editor);
                Assert.Equal("Notepad++", editor.Label);
                Assert.Equal(@"C:\Program Files\Notepad++\notepad++.exe", editor.Path);
                Assert.Equal("-n%line -c%column %file", editor.Args);
                Assert.False(editor.EscapeQuotes);
                Assert.True(string.IsNullOrEmpty(editor.Extensions));

                // old binary data stores
                Assert.True(storage.ContainsKey(GrepSettings.Key.LastCheckedVersion));
                Assert.Equal(DateTime.Now, storage.Get<DateTime>(GrepSettings.Key.LastCheckedVersion), TimeSpan.FromSeconds(5));

                Assert.True(storage.ContainsKey(GrepSettings.Key.StartDate));
                Assert.Null(storage.GetNullable<DateTime?>(GrepSettings.Key.StartDate));

                Assert.True(storage.ContainsKey(GrepSettings.Key.EndDate));
                Assert.Null(storage.GetNullable<DateTime?>(GrepSettings.Key.EndDate));

                Assert.True(storage.ContainsKey(GrepSettings.Key.PreviewWindowWrap));
                Assert.False(storage.Get<bool>(GrepSettings.Key.PreviewWindowWrap));

                Assert.True(storage.ContainsKey(GrepSettings.Key.FastSearchBookmarks));
                Assert.Empty(storage.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastSearchBookmarks));

                Assert.True(storage.ContainsKey(GrepSettings.Key.FastReplaceBookmarks));
                Assert.Empty(storage.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastReplaceBookmarks));

                Assert.True(storage.ContainsKey(GrepSettings.Key.FastFileMatchBookmarks));
                Assert.Empty(storage.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastFileMatchBookmarks));

                Assert.True(storage.ContainsKey(GrepSettings.Key.FastFileNotMatchBookmarks));
                Assert.Empty(storage.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastFileNotMatchBookmarks));

                Assert.True(storage.ContainsKey(GrepSettings.Key.FastPathBookmarks));
                Assert.Empty(storage.Get<List<MostRecentlyUsed>>(GrepSettings.Key.FastPathBookmarks));

                var plugins = storage.Get<List<PluginConfiguration>>(GrepSettings.Key.Plugins);
                Assert.NotNull(plugins);
                Assert.Equal(3, plugins.Count);
                var openxml = plugins.FirstOrDefault(r => r.Name.Equals("Openxml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(openxml);
                Assert.True(openxml.Enabled);
                Assert.True(openxml.PreviewText);
                Assert.Equal("docx, docm, xls, xlsx, xlsm", openxml.Extensions);
                Assert.False(storage.ContainsKey("AddOpenxmlExtensions"));
                Assert.False(storage.ContainsKey("RemOpenxmlExtensions"));

                Assert.True(storage.ContainsKey(GrepSettings.Key.ArchiveExtensions));
                Assert.True(storage.ContainsKey(GrepSettings.Key.ArchiveCustomExtensions));
                Assert.NotEmpty(storage.GetExtensionList("Archive"));
            }
            else
            {
                Assert.Fail("Could not create instance of GrepSettings");
            }
        }

        [Fact]
        public void TestConvertFromV2toV3()
        {
            if (Activator.CreateInstance(typeof(GrepSettings), true) is GrepSettings storage)
            {
                storage.Clear();

                string file = Path.Combine(sourceFolder, "Settings", "version2", "dnGREP.Settings.dat");
                storage.Load(file);
                // these are called when plugins are loaded
                storage.ConvertExtensionsToV3("Word", ["doc"]);
                storage.ConvertExtensionsToV3("Pdf", ["pdf"]);
                storage.ConvertExtensionsToV3("Openxml", ["docx", "docm", "xls", "xlsx", "xlsm", "pptx", "pptm"]);

                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.CustomEditor));
                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.CustomEditorArgs));
                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.EscapeQuotesInMatchArgument));
                Assert.True(storage.ContainsKey(GrepSettings.Key.CustomEditors));

                List<CustomEditor> list = storage.Get<List<CustomEditor>>(GrepSettings.Key.CustomEditors);

                Assert.NotNull(list);
                Assert.NotEmpty(list);
                Assert.Single(list);

                CustomEditor editor = list[0];
                Assert.NotNull(editor);
                Assert.Equal("Notepad++", editor.Label);
                Assert.Equal(@"C:\Program Files\Notepad++\notepad++.exe", editor.Path);
                Assert.Equal("-n%line -c%column %file", editor.Args);
                Assert.True(editor.EscapeQuotes);
                Assert.True(string.IsNullOrEmpty(editor.Extensions));

                var plugins = storage.Get<List<PluginConfiguration>>(GrepSettings.Key.Plugins);
                Assert.NotNull(plugins);
                Assert.Equal(3, plugins.Count);
                var openxml = plugins.FirstOrDefault(r => r.Name.Equals("Openxml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(openxml);
                Assert.True(openxml.Enabled);
                Assert.True(openxml.PreviewText);
                Assert.Equal("docx, docm, xls, xlsx, xlsm", openxml.Extensions);
                Assert.False(storage.ContainsKey("AddOpenxmlExtensions"));
                Assert.False(storage.ContainsKey("RemOpenxmlExtensions"));

                Assert.True(storage.ContainsKey(GrepSettings.Key.ArchiveExtensions));
                Assert.True(storage.ContainsKey(GrepSettings.Key.ArchiveCustomExtensions));
                Assert.NotEmpty(storage.GetExtensionList("Archive"));
            }
            else
            {
                Assert.Fail("Could not create instance of GrepSettings");
            }
        }

        [Fact]
        public void TestLoadV3()
        {
            if (Activator.CreateInstance(typeof(GrepSettings), true) is GrepSettings storage)
            {
                storage.Clear();

                string file = Path.Combine(sourceFolder, "Settings", "version3", "dnGREP.Settings.dat");
                storage.Load(file);
                // these are called when plugins are loaded
                storage.ConvertExtensionsToV3("Word", ["doc"]);
                storage.ConvertExtensionsToV3("Pdf", ["pdf"]);
                storage.ConvertExtensionsToV3("Openxml", ["docx", "docm", "xls", "xlsx", "xlsm", "pptx", "pptm"]);

                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.CustomEditor));
                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.CustomEditorArgs));
                Assert.False(storage.ContainsKey(GrepSettings.ObsoleteKey.EscapeQuotesInMatchArgument));
                Assert.True(storage.ContainsKey(GrepSettings.Key.CustomEditors));

                List<CustomEditor> list = storage.Get<List<CustomEditor>>(GrepSettings.Key.CustomEditors);

                Assert.NotNull(list);
                Assert.NotEmpty(list);
                Assert.Equal(3, list.Count);

                CustomEditor editor = list[0];
                Assert.NotNull(editor);
                Assert.Equal("Notepad++", editor.Label);
                Assert.Equal(@"C:\Program Files\Notepad++\notepad++.exe", editor.Path);
                Assert.Equal("-n%line -c%column %file", editor.Args);
                Assert.False(editor.EscapeQuotes);
                Assert.True(string.IsNullOrEmpty(editor.Extensions));

                editor = list[1];
                Assert.NotNull(editor);
                Assert.Equal("VSCode", editor.Label);
                Assert.Equal(@"C:\Users\user\AppData\Local\Programs\Microsoft VS Code\Code.exe", editor.Path);
                Assert.Equal("-r -g %file:%line:%column", editor.Args);
                Assert.False(editor.EscapeQuotes);
                Assert.Equal("txt", editor.Extensions);

                var plugins = storage.Get<List<PluginConfiguration>>(GrepSettings.Key.Plugins);
                Assert.NotNull(plugins);
                Assert.Equal(3, plugins.Count);
                var openxml = plugins.FirstOrDefault(r => r.Name.Equals("Openxml", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(openxml);
                Assert.True(openxml.Enabled);
                Assert.True(openxml.PreviewText);
                Assert.Equal("docx, docm, xls, xlsx, xlsm", openxml.Extensions);
                Assert.False(storage.ContainsKey("AddOpenxmlExtensions"));
                Assert.False(storage.ContainsKey("RemOpenxmlExtensions"));

                Assert.True(storage.ContainsKey(GrepSettings.Key.ArchiveExtensions));
                Assert.True(storage.ContainsKey(GrepSettings.Key.ArchiveCustomExtensions));
                Assert.NotEmpty(storage.GetExtensionList("Archive"));
            }
            else
            {
                Assert.Fail("Could not create instance of GrepSettings");
            }
        }
    }
}
