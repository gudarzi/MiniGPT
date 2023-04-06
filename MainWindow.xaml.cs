using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MiniGPT
{
	public partial class MainWindow : Window
	{
		MainViewModel mvm;

		public MainWindow()
		{
			mvm = new MainViewModel();
			DataContext = mvm;
			InitializeComponent();
			TheWebView.EnsureCoreWebView2Async();
			TheWebView.CoreWebView2InitializationCompleted += TheWebView_CoreWebView2InitializationCompleted;
		}

		private void TheWebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
		{
			TheWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(mvm.hostName, ".", CoreWebView2HostResourceAccessKind.Allow);
		}

		private void RenderButton_Click(object sender, RoutedEventArgs e)
		{
			TheWebView.NavigateToString(mvm.HTML);
		}

		private async void RequestButton_Click(object sender, RoutedEventArgs e)
		{
			var progress = new Progress<string>(report =>
			{
				//Debug.WriteLine(report);
				TheWebView.NavigateToString(report);
			});
			await Task.Run(() => mvm.SendRequest(progress));
		}
	}
}