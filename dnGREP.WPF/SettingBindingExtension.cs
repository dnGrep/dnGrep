using System.Windows.Data;


namespace dnGREP.WPF
{
	public class AppSettingBindingExtension : Binding
	{
		public AppSettingBindingExtension()
		{
			Initialize();
		}

		public AppSettingBindingExtension(string path)
			: base(path)
		{
			Initialize();
		}

		private void Initialize()
		{
			this.Source = Properties.Settings.Default;
			this.Mode = BindingMode.TwoWay;
		}
	}

}
