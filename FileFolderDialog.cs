using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Windows.Forms;

namespace dnGREP
{
	public class FileFolderDialog : CommonDialog
	{
		private OpenFileDialog dialog = new OpenFileDialog();

		public OpenFileDialog Dialog
		{
			get { return dialog; }
			set { dialog = value; }
		}

		public new DialogResult ShowDialog()
		{
			return this.ShowDialog(null);
		}

		public new DialogResult ShowDialog(IWin32Window owner)
		{
			dialog.ValidateNames = false;
			
			try
			{
				if (dialog.FileName != null && dialog.FileName != "")
				{
					if (Directory.Exists(dialog.FileName))
						dialog.InitialDirectory = dialog.FileName;
					else
						dialog.InitialDirectory = Path.GetDirectoryName(dialog.FileName);
				}
			}
			catch (Exception ex)
			{
				// Do nothing
			}

			dialog.FileName = "Folder Selection.";

			if (owner == null)
				return dialog.ShowDialog();
			else
				return dialog.ShowDialog(owner);
		}

		public string SelectedPath
		{
			get {
				try
				{
					if (dialog.FileName != null &&
						(dialog.FileName.EndsWith("Folder Selection.") || !File.Exists(dialog.FileName)) && 
						!Directory.Exists(dialog.FileName))
					{
						return Path.GetDirectoryName(dialog.FileName);
					}					
					else
					{
						return dialog.FileName;
					}
				}
				catch (Exception ex)
				{
					return dialog.FileName;
				}
			}
			set
			{
				if (value != null && value != "")
				{
					dialog.FileName = value;
				}
			}
		}

		public string SelectedPaths
		{
			get {
				if (dialog.FileNames != null && dialog.FileNames.Length > 1)
				{
					StringBuilder sb = new StringBuilder();
					foreach (string fileName in dialog.FileNames)
					{
						try
						{
							if (File.Exists(fileName))
								sb.Append(fileName + ";");
						}
						catch (Exception ex)
						{
							// Go to next
						}
					}
					return sb.ToString();
				}
				else
				{
					return null;
				}
			}
		}		

		public override void Reset()
		{
			dialog.Reset();
		}

		protected override bool RunDialog(IntPtr hwndOwner)
		{
			return true;
		}
	}
}
