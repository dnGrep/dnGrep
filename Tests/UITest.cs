using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Xunit;

namespace Tests
{

    public class UITest : TestBase
    {
        [Fact]
        public void TestDefaultDictionariesMatch()
        {
            string solutionPath = Directory.GetParent(GetDllPath())
                ?.Parent?.Parent?.Parent?.Parent?.FullName ?? string.Empty;

            var path1 = Path.Combine(solutionPath, @"dnGREP.WPF\Themes\LightBrushes.xaml");
            var path2 = Path.Combine(solutionPath, @"dnGREP.WPF\Themes\DarkBrushes.xaml");
            Assert.True(File.Exists(path1));
            Assert.True(File.Exists(path2));

            ResourceDictionary? dict1 = LoadXaml(path1);
            ResourceDictionary? dict2 = LoadXaml(path2);

            Assert.NotNull(dict1);
            Assert.NotNull(dict2);

            Assert.Equal(dict1.Count, dict2.Count);

            foreach (var key in dict1.Keys)
            {
                Assert.True(dict2.Contains(key));

                object value1 = dict1[key];
                object value2 = dict2[key];

                if (value1 is Brush)
                    Assert.True(value2 is Brush);
                else if (value1 is DropShadowEffect)
                    Assert.True(value2 is DropShadowEffect);
                else if (value1 is bool)
                    Assert.True(value2 is bool);
                else if (value1 is BitmapImage)
                    Assert.True(value2 is BitmapImage);
            }
        }

        private ResourceDictionary? LoadXaml(string path)
        {
            using (FileStream s = new FileStream(path, FileMode.Open))
            {
                object obj = XamlReader.Load(s);
                if (obj is ResourceDictionary dict)
                {
                    return dict;
                }
            }
            return null;
        }
    }
}
