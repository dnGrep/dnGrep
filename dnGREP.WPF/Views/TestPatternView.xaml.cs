using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using dnGREP.Common;
using dnGREP.Engines;
using NLog;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for TestPattern.xaml
    /// </summary>
    public partial class TestPattern : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
		private TestPatternState inputData = new TestPatternState();

        public TestPattern()
        {
            InitializeComponent();
            this.DataContext = inputData;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            inputData.UpdateState("");
        }

        private void formKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {            			
            GrepEnginePlainText engine = new GrepEnginePlainText();
            engine.Initialize(new GrepEngineInitParams(GrepSettings.Instance.Get<double>(GrepSettings.Key.FuzzyMatchThreshold)));
            List<GrepSearchResult> results = new List<GrepSearchResult>();
            GrepSearchOption searchOptions = GrepSearchOption.None;
            if (inputData.Multiline)
                searchOptions |= GrepSearchOption.Multiline;
            if (inputData.CaseSensitive)
                searchOptions |= GrepSearchOption.CaseSensitive;
            if (inputData.Singleline)
                searchOptions |= GrepSearchOption.SingleLine;
			if (inputData.WholeWord)
				searchOptions |= GrepSearchOption.WholeWord;
            using (Stream inputStream = new MemoryStream(Encoding.Default.GetBytes(tbTestInput.Text)))
            {
				try
				{
					results = engine.Search(inputStream, "test.txt", inputData.SearchFor, inputData.TypeOfSearch,
						searchOptions, Encoding.Default);
				}
				catch (ArgumentException ex)
				{
					MessageBox.Show("Incorrect pattern: " + ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
            }
            inputData.SearchResults.Clear();
            inputData.SearchResults.AddRange(results);
            tbTestOutput.Text = "";
            if (inputData.SearchResults.Count == 1)
            {
                foreach (FormattedGrepLine line in inputData.SearchResults[0].FormattedLines)
                {
                    // Copy children Inline to a temporary array.
                    Inline[] inlines = new Inline[line.FormattedText.Count];
                    line.FormattedText.CopyTo(inlines, 0);

                    foreach (Inline inline in inlines)
                    {
                        tbTestOutput.Inlines.Add(inline);
                    }
                    tbTestOutput.Inlines.Add(new LineBreak());
                    tbTestOutput.Inlines.Add(new Run("================================="));
                    tbTestOutput.Inlines.Add(new LineBreak());
                }
            }
            else
            {
                tbTestOutput.Text = "No matches found";
            }
        }

        private void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            GrepEnginePlainText engine = new GrepEnginePlainText();
			engine.Initialize(new GrepEngineInitParams(0.5));
            List<GrepSearchResult> results = new List<GrepSearchResult>();

            GrepSearchOption searchOptions = GrepSearchOption.None;
            if (inputData.Multiline)
                searchOptions |= GrepSearchOption.Multiline;
            if (inputData.CaseSensitive)
                searchOptions |= GrepSearchOption.CaseSensitive;
            if (inputData.Singleline)
                searchOptions |= GrepSearchOption.SingleLine;
			if (inputData.WholeWord)
				searchOptions |= GrepSearchOption.WholeWord;

            string replacedString = "";
            using (Stream inputStream = new MemoryStream(Encoding.Default.GetBytes(tbTestInput.Text)))
            using (Stream writeStream = new MemoryStream())
            {
                engine.Replace(inputStream, writeStream, inputData.SearchFor, inputData.ReplaceWith, inputData.TypeOfSearch,
                    searchOptions, Encoding.Default);
                writeStream.Position = 0;
                StreamReader reader = new StreamReader(writeStream);                
                replacedString = reader.ReadToEnd();
            }
            inputData.SearchResults.Clear();
            inputData.SearchResults.AddRange(results);
            tbTestOutput.Text = replacedString;            
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            GrepSettings.Instance.Save();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnCopyFile_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(tbTestOutput.Text);
        }
    }
}
