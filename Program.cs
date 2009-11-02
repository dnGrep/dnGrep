using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NLog;
using dnGREP.Common;

namespace dnGREP
{
	static class Program
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			AppDomain.CurrentDomain.AppendPrivatePath(Utils.GetCurrentPath() + "\\Plugins");
			
//#if DEBUG
//            System.Diagnostics.Debugger.Break();
//#endif
			if (args != null && args.Length > 0)
			{
				Properties.Settings.Default.SearchFolder = args[0];
			}
			try
			{
				Application.Run(new MainForm());
			}
			catch (Exception ex)
			{
				logger.LogException(LogLevel.Error, ex.Message, ex);
			}
		}
	}
}