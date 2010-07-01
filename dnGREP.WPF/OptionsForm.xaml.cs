using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;
using dnGREP.Common;
using System.Reflection;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for OptionsForm.xaml
    /// </summary>
    public partial class OptionsForm : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public OptionsForm()
        {
            InitializeComponent();
            oldShellUnregister();
            openFileDialog.Title = "Path to custom editor...";
			DiginesisHelpProvider.HelpNamespace = "Doc\\dnGREP.chm";
			DiginesisHelpProvider.ShowHelp = true;
        }

		public GrepSettings settings
		{
			get { return GrepSettings.Instance; }
		}

        private System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        private static string SHELL_KEY_NAME = "dnGREP";
        private static string OLD_SHELL_KEY_NAME = "nGREP";
        private static string SHELL_MENU_TEXT = "dnGREP...";
        private bool isAdministrator = true;

        public bool IsAdministrator
        {
            get { return isAdministrator; }
			set { isAdministrator = value; UpdateState("IsAdministrator"); }
        }     

        private void UpdateState(string name)
        {
            if (!isAdministrator)
            {
                cbRegisterShell.IsEnabled = false;
                cbRegisterShell.ToolTip = "To set shell integration run dnGREP as Administrator.";
                grShell.ToolTip = "To set shell integration run dnGREP as Administrator.";
            }
            else
            {
                cbRegisterShell.IsEnabled = true;
                cbRegisterShell.ToolTip = "Shell integration enables running an application from shell context menu.";
                grShell.ToolTip = "Shell integration enables running an application from shell context menu.";
            }

            if (settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking))
            {
                tbUpdateInterval.IsEnabled = true;
            }
            else
            {
                tbUpdateInterval.IsEnabled = false;
            }
            if (settings.Get<bool>(GrepSettings.Key.ShowLinesInContext))
            {
                tbLinesAfter.IsEnabled = true;
                tbLinesBefore.IsEnabled = true;
            }
            else
            {
                tbLinesAfter.IsEnabled = false;
                tbLinesBefore.IsEnabled = false;
            }
            if (settings.Get<bool>(GrepSettings.Key.UseCustomEditor))
            {
                rbSpecificEditor.IsChecked = true;
                rbDefaultEditor.IsChecked = false;
                tbEditorPath.IsEnabled = true;
                btnBrowse.IsEnabled = true;
                tbEditorArgs.IsEnabled = true;
            }
            else
            {
                rbSpecificEditor.IsChecked = false;
                rbDefaultEditor.IsChecked = true;
                tbEditorPath.IsEnabled = false;
                btnBrowse.IsEnabled = false;
                tbEditorArgs.IsEnabled = false;
            }
        }

        private bool isShellRegistered(string location)
        {
            if (!isAdministrator)
                return false;

            string regPath = string.Format(@"{0}\shell\{1}",
                                       location, SHELL_KEY_NAME);
            try
            {
                return Registry.ClassesRoot.OpenSubKey(regPath) != null;
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdministrator = false;
                return false;
            }
        }

        private void shellRegister(string location)
        {
            if (!isAdministrator)
                return;

            if (!isShellRegistered(location))
            {
                string regPath = string.Format(@"{0}\shell\{1}", location, SHELL_KEY_NAME);

                // add context menu to the registry

                using (RegistryKey key =
                       Registry.ClassesRoot.CreateSubKey(regPath))
                {
                    key.SetValue(null, SHELL_MENU_TEXT);
                }

                // add command that is invoked to the registry
                string menuCommand = string.Format("\"{0}\" \"%1\"",
                                       Assembly.GetAssembly(typeof(OptionsForm)).Location);
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(
                    string.Format(@"{0}\command", regPath)))
                {
                    key.SetValue(null, menuCommand);
                }
            }
        }

        private void shellUnregister(string location)
        {
            if (!isAdministrator)
                return;

            if (isShellRegistered(location))
            {
                string regPath = string.Format(@"{0}\shell\{1}", location, SHELL_KEY_NAME);
                Registry.ClassesRoot.DeleteSubKeyTree(regPath);
            }
        }

        private void oldShellUnregister()
        {
            if (!isAdministrator)
                return;

            string regPath = string.Format(@"Directory\shell\{0}", OLD_SHELL_KEY_NAME);
            if (Registry.ClassesRoot.OpenSubKey(regPath) != null)
            {
                Registry.ClassesRoot.DeleteSubKeyTree(regPath);
            }
        }

        private void checkIfAdmin()
        {
			try
			{
				WindowsIdentity wi = WindowsIdentity.GetCurrent();
				WindowsPrincipal wp = new WindowsPrincipal(wi);

				if (wp.IsInRole("Administrators"))
				{
					isAdministrator = true;
				}
				else
				{
					isAdministrator = false;
				}
			}
			catch (Exception ex)
			{
				isAdministrator = false;
			}
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            checkIfAdmin();
            cbRegisterShell.IsChecked = isShellRegistered("Directory");
            cbCheckForUpdates.IsChecked = settings.Get<bool>(GrepSettings.Key.EnableUpdateChecking);
			cbShowPath.IsChecked = settings.Get<bool>(GrepSettings.Key.ShowFilePathInResults);
			cbShowContext.IsChecked = settings.Get<bool>(GrepSettings.Key.ShowLinesInContext);
            tbLinesBefore.Text = settings.Get<int>(GrepSettings.Key.ContextLinesBefore).ToString();
			tbLinesAfter.Text = settings.Get<int>(GrepSettings.Key.ContextLinesAfter).ToString();
            cbSearchFileNameOnly.IsChecked = settings.Get<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern);
            tbEditorPath.Text = settings.Get<string>(GrepSettings.Key.CustomEditor);
            tbEditorArgs.Text = settings.Get<string>(GrepSettings.Key.CustomEditorArgs);
			cbPreviewResults.IsChecked = settings.Get<bool>(GrepSettings.Key.PreviewResults);
			tbUpdateInterval.Text = settings.Get<int>(GrepSettings.Key.UpdateCheckInterval).ToString();
			tbFuzzyMatchThreshold.Text = settings.Get<double>(GrepSettings.Key.FuzzyMatchThreshold).ToString();
            UpdateState("Initial");
        }

        private void cbRegisterShell_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cbRegisterShell.IsChecked == true)
            {
                shellRegister("Directory");
                shellRegister("Drive");
                shellRegister("*");
            }
            else if (!cbRegisterShell.IsChecked == true)
            {
                shellUnregister("Directory");
                shellUnregister("Drive");
                shellUnregister("*");
            }
        }

        private void rbEditorCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rbDefaultEditor.IsChecked == true)
                settings.Set<bool>(GrepSettings.Key.UseCustomEditor, false);
            else
				settings.Set<bool>(GrepSettings.Key.UseCustomEditor, true);

            UpdateState("UseCustomEditor");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
			settings.Set<int>(GrepSettings.Key.ContextLinesBefore, Utils.ParseInt(tbLinesBefore.Text, 0));
			settings.Set<int>(GrepSettings.Key.ContextLinesAfter, Utils.ParseInt(tbLinesAfter.Text, 0));
            settings.Set<bool>(GrepSettings.Key.AllowSearchingForFileNamePattern, cbSearchFileNameOnly.IsChecked == true);
            settings.Set<string>(GrepSettings.Key.CustomEditor, tbEditorPath.Text);
            settings.Set<string>(GrepSettings.Key.CustomEditorArgs, tbEditorArgs.Text);
			settings.Set<bool>(GrepSettings.Key.PreviewResults, cbPreviewResults.IsChecked == true);
			settings.Set<int>(GrepSettings.Key.UpdateCheckInterval, Utils.ParseInt(tbUpdateInterval.Text, 1));
			double threshold = Utils.ParseDouble(tbFuzzyMatchThreshold.Text, 0.5);
			if (threshold >= 0 && threshold <= 1.0)
				settings.Set<double>(GrepSettings.Key.FuzzyMatchThreshold, threshold);
			settings.Save();
        }

        public static string GetEditorPath(string file, int line)
        {
            if (!GrepSettings.Instance.Get<bool>(GrepSettings.Key.UseCustomEditor))
            {
                return file;
            }
            else
            {
                if (!string.IsNullOrEmpty(GrepSettings.Instance.Get<string>(GrepSettings.Key.CustomEditor)))
                {
                    string path = GrepSettings.Instance.Get<string>(GrepSettings.Key.CustomEditor).Replace("%file", "\"" + file + "\"").Replace("%line", line.ToString());
                    return path;
                }
                else
                {
                    return file;
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbEditorPath.Text = openFileDialog.FileName;
            }
        }

        private void cbCheckForUpdates_CheckedChanged(object sender, RoutedEventArgs e)
        {
            settings.Set<bool>(GrepSettings.Key.EnableUpdateChecking, cbCheckForUpdates.IsChecked == true);
            if (tbUpdateInterval.Text.Trim() == "")
                tbUpdateInterval.Text = "1";
            UpdateState("EnableUpdateChecking");
        }

        private void cbShowPath_CheckedChanged(object sender, RoutedEventArgs e)
        {
			settings.Set<bool>(GrepSettings.Key.ShowFilePathInResults, cbShowPath.IsChecked == true);
        }

        private void cbShowContext_CheckedChanged(object sender, RoutedEventArgs e)
        {
			settings.Set<bool>(GrepSettings.Key.ShowLinesInContext, cbShowContext.IsChecked == true);
            UpdateState("ShowLinesInContext");
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

		private void tbFuzzyMatchThreshold_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (Utils.ParseDouble(tbFuzzyMatchThreshold.Text) < 0 ||
				Utils.ParseDouble(tbFuzzyMatchThreshold.Text) > 1.0)
			{
				lblFuzzyMatchError.Visibility = Visibility.Visible;
			}
			else
			{
				lblFuzzyMatchError.Visibility = Visibility.Collapsed;
			}
		}

		private void btnClearPreviousSearches_Click(object sender, RoutedEventArgs e)
		{
			settings.Set<List<string>>(GrepSettings.Key.FastFileMatchBookmarks, new List<string>());
			settings.Set<List<string>>(GrepSettings.Key.FastFileNotMatchBookmarks, new List<string>());
			settings.Set<List<string>>(GrepSettings.Key.FastPathBookmarks, new List<string>());
			settings.Set<List<string>>(GrepSettings.Key.FastReplaceBookmarks, new List<string>());
			settings.Set<List<string>>(GrepSettings.Key.FastSearchBookmarks, new List<string>());
		}
    }
}
