using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace dnGREP.WPF
{
	public class OptionsFormState : DependencyObject
	{
		/// <summary>
		/// LastCheckedVersion property
		/// </summary>
		public DateTime LastCheckedVersion
		{
			get { return (DateTime)GetValue(LastCheckedVersionProperty); }
			set { SetValue(LastCheckedVersionProperty, value); updateState(); }
		}

		public static DependencyProperty LastCheckedVersionProperty =
			DependencyProperty.Register("LastCheckedVersion", typeof(DateTime), typeof(OptionsFormState),
			new FrameworkPropertyMetadata(DateTime.Now, new PropertyChangedCallback(OnLastCheckedVersionChanged)));

		private static void OnLastCheckedVersionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.LastCheckedVersion = (DateTime)args.NewValue;
			updateState();
		}

		/// <summary>
		/// UpdateCheckInterval property
		/// </summary>
		public string UpdateCheckInterval
		{
			get { return (string)GetValue(UpdateCheckIntervalProperty); }
			set { SetValue(UpdateCheckIntervalProperty, value); updateState(); }
		}

		public static DependencyProperty UpdateCheckIntervalProperty =
			DependencyProperty.Register("UpdateCheckInterval", typeof(string), typeof(OptionsFormState),
			new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnUpdateCheckIntervalChanged)));

		private static void OnUpdateCheckIntervalChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.UpdateCheckInterval = (string)args.NewValue;
			updateState();
		}

		/// <summary>
		/// EnableUpdateChecking property
		/// </summary>
		public bool EnableUpdateChecking
		{
			get { return (bool)GetValue(EnableUpdateCheckingProperty); }
			set { SetValue(EnableUpdateCheckingProperty, value); updateState(); }
		}

		public static DependencyProperty EnableUpdateCheckingProperty =
			DependencyProperty.Register("EnableUpdateChecking", typeof(bool), typeof(OptionsFormState),
			new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnEnableUpdateCheckingChanged)));

		private static void OnEnableUpdateCheckingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			Properties.Settings.Default.EnableUpdateChecking = (bool)args.NewValue;
			updateState();
		}

		private static void updateState()
		{

		}
	}
}
