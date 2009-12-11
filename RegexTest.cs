using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using dnGREP.Common;

namespace dnGREP
{
	public partial class RegexTest : Form
	{
		private string searchRegex = "";
		private string replaceRegex = "";

		public RegexTest(string search, string replace)
		{
			InitializeComponent();
			searchRegex = search;
			replaceRegex = replace;
			tbSearchFor.Text = searchRegex;
			tbReplaceWith.Text = replaceRegex;
		}

		private void btnSearch_Click(object sender, EventArgs e)
		{
			try
			{
				RegexOptions options = RegexOptions.Singleline;
				if (cbMultiline.Checked)
					options = RegexOptions.Multiline;
				if (!cbCaseSensitive.Checked)
					options = options | RegexOptions.IgnoreCase;

				if (cbMultiline.Checked)
				{
					Regex regex = new Regex(tbSearchFor.Text, options);
					StringBuilder sb = new StringBuilder();
					foreach (Match match in regex.Matches(tbInputText.Text))
					{
						sb.AppendLine(match.Value);
						sb.AppendLine("=================================");
					}
					tbOutputText.Text = sb.ToString();
				}
				else
				{
					string[] lines = Utils.CleanLineBreaks(tbInputText.Text).Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
					foreach (string line in lines)
					{
						Regex regex = new Regex(tbSearchFor.Text, options);
						StringBuilder sb = new StringBuilder();
						foreach (Match match in regex.Matches(line))
						{
							sb.AppendLine(match.Value);
							sb.AppendLine("=================================");
						}
						tbOutputText.Text = sb.ToString();
					}
				}
			}
			catch (Exception ex)
			{
				tbOutputText.Text = "Error: " + ex.Message;
			}
		}

		private void formKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}

		private void btnReplace_Click(object sender, EventArgs e)
		{
			try
			{
				RegexOptions options = RegexOptions.Singleline;
				if (cbMultiline.Checked)
					options = RegexOptions.Multiline;
				if (cbCaseSensitive.Checked)
					options = options | RegexOptions.IgnoreCase;
				Regex search = new Regex(tbSearchFor.Text, options);
				string replace = tbReplaceWith.Text;

				tbOutputText.Text = search.Replace(tbInputText.Text, replace);
			}
			catch (Exception ex)
			{
				tbOutputText.Text = "Error: " + ex.Message;
			}
		}

		private void btnDone_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(Utils.GetCurrentPath() + "\\Doc\\regular-expressions-cheat-sheet-v2.pdf");
			}
			catch (Exception ex)
			{
				tbOutputText.Text = "Error. Could not open help file: " + ex.Message;
			}
		}
	}
}