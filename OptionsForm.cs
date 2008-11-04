using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace nGREP
{
	public partial class OptionsForm : Form
	{
		private static string SHELL_KEY_NAME = "nGREP";
		private static string SHELL_MENU_TEXT = "nGREP...";
		
		public OptionsForm()
		{
			InitializeComponent();
		}

		private void changeState()
		{
			if (Properties.Settings.Default.UseCustomEditor)
			{
				rbSpecificEditor.Checked = true;
				rbDefaultEditor.Checked = false;
				tbEditorPath.Enabled = true;
			}
			else
			{
				rbSpecificEditor.Checked = false;
				rbDefaultEditor.Checked = true;
				tbEditorPath.Enabled = false;
			}
		}

		private bool isShellRegistered()
		{
			string regPath = string.Format(@"Directory\shell\{0}",
									   SHELL_KEY_NAME);

			return Registry.ClassesRoot.OpenSubKey(regPath) != null;
		}

		private void shellRegister()
		{
			string regPath = string.Format(@"Directory\shell\{0}", SHELL_KEY_NAME);

			// add context menu to the registry

			using (RegistryKey key =
				   Registry.ClassesRoot.CreateSubKey(regPath))
			{
				key.SetValue(null, SHELL_MENU_TEXT);
			}

			// add command that is invoked to the registry
			string menuCommand = string.Format("\"{0}\" \"%1\"",
								   Application.ExecutablePath);
			using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(
				string.Format(@"{0}\command", regPath)))
			{
				key.SetValue(null, menuCommand);
			}
		}

		private void shellUnregister()
		{
			string regPath = string.Format(@"Directory\shell\{0}", SHELL_KEY_NAME);

			Registry.ClassesRoot.DeleteSubKeyTree(regPath);
		}

		private void OptionsForm_Load(object sender, EventArgs e)
		{
			cbRegisterShell.Checked = isShellRegistered();
			changeState();
		}
		
		private void cbRegisterShell_CheckedChanged(object sender, EventArgs e)
		{
			if (cbRegisterShell.Checked && !isShellRegistered())
			{
				shellRegister();
			}
			else if (!cbRegisterShell.Checked && isShellRegistered())
			{
				shellUnregister();
			}
		}

		private void rbEditorCheckedChanged(object sender, EventArgs e)
		{
			if (rbDefaultEditor.Checked)
				Properties.Settings.Default.UseCustomEditor = false;
			else
				Properties.Settings.Default.UseCustomEditor = true;

			changeState();
		}

		private void OptionsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Properties.Settings.Default.Save();
		}

		public static string GetEditorPath(string file, int line)
		{
			if (!Properties.Settings.Default.UseCustomEditor)
			{
				return file;
			}
			else
			{
				if (!string.IsNullOrEmpty(Properties.Settings.Default.CustomEditor))
				{
					string path = Properties.Settings.Default.CustomEditor.Replace("%file", "\"" + file + "\"").Replace("%line", line.ToString());
					return path;
				}
				else
				{
					return file;
				}
			}
		}
	}
}