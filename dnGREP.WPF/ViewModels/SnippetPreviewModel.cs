using dnGREP.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace dnGREP.WPF.ViewModels
{
    public class SnippetPreviewModel : WorkspaceViewModel
    {
        private InlineCollection text;
        public InlineCollection Text
        {
            get { return text; }
            set
            {
                if (value == text)
                    return;

                text = value;

                base.OnPropertyChanged(() => Text);
            }
        }

        private string lines;
        public string Lines
        {
            get { return lines; }
            set
            {
                if (value == lines)
                    return;

                lines = value;

                base.OnPropertyChanged(() => Lines);
            }
        }

        private string snippetInfo;
        public string SnippetInfo
        {
            get { return snippetInfo; }
            set
            {
                if (value == snippetInfo)
                    return;

                snippetInfo = value;

                base.OnPropertyChanged(() => SnippetInfo);
            }
        }

        private InlineCollection formatLine(GrepSearchResult.GrepLine line)
        {
            Paragraph paragraph = new Paragraph();
            var font = new FontFamily("Consolas");

            if (line.Matches.Count == 0)
            {
                Run mainRun = new Run(line.LineText);
                paragraph.Inlines.Add(mainRun);
            }
            else
            {
                int counter = 0;
                string fullLine = line.LineText;
                GrepSearchResult.GrepMatch[] lineMatches = new GrepSearchResult.GrepMatch[line.Matches.Count];
                line.Matches.CopyTo(lineMatches);
                foreach (GrepSearchResult.GrepMatch m in lineMatches)
                {
                    try
                    {
                        string regLine = null;
                        string fmtLine = null;
                        if (fullLine.Length < m.StartLocation + m.Length)
                        {
                            regLine = fullLine;
                        }
                        else
                        {
                            regLine = fullLine.Substring(counter, m.StartLocation - counter);
                            fmtLine = fullLine.Substring(m.StartLocation, m.Length);
                        }

                        Run regularRun = new Run(regLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);

                        if (fmtLine != null)
                        {
                            Run highlightedRun = new Run(fmtLine);
                            highlightedRun.FontFamily = font;
                            highlightedRun.Background = Brushes.Yellow;
                            paragraph.Inlines.Add(highlightedRun);
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch
                    {
                        Run regularRun = new Run(fullLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);
                    }
                    finally
                    {
                        counter = m.StartLocation + m.Length;
                    }
                }
                if (counter < fullLine.Length)
                {
                    try
                    {
                        string regLine = fullLine.Substring(counter);
                        Run regularRun = new Run(regLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);
                    }
                    catch
                    {
                        Run regularRun = new Run(fullLine);
                        regularRun.FontFamily = font;
                        paragraph.Inlines.Add(regularRun);
                    }
                }
            }
            return paragraph.Inlines;
        }
    }
}
