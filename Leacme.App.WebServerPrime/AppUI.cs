// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Leacme.Lib.WebServerPrime;

namespace Leacme.App.WebServerPrime {

	public class AppUI {

		private StackPanel rootPan = (StackPanel)Application.Current.MainWindow.Content;
		private Library lib = new Library();

		public AppUI() {

			rootPan.Spacing = 6;

			var saScrollable = App.ScrollableTextBlock;
			var saBox = App.TextBox;
			saBox.Height = 50;
			saScrollable.Content = saBox;

			var logScrollable = App.ScrollableTextBlock;
			saScrollable.Background = logScrollable.Background = Brushes.Transparent;
			saScrollable.Width = logScrollable.Width = 700;
			var logBox = App.TextBox;
			saBox.Width = logBox.Width = logScrollable.Width - App.Margin.Top * 6;
			logBox.Height = 160;
			saBox.IsReadOnly = logBox.IsReadOnly = true;
			logScrollable.Content = logBox;

			List<string> ipsToAddToMenu;

			var blurb1 = App.TextBlock;
			blurb1.Text = "Select the server root directory from which to serve the files to the web:";
			blurb1.TextAlignment = TextAlignment.Center;

			var rootDirField = App.HorizontalFieldWithButton;
			rootDirField.holder.HorizontalAlignment = HorizontalAlignment.Center;
			rootDirField.label.Text = "Server Root Directory:";
			rootDirField.field.IsReadOnly = true;
			rootDirField.field.Text = Directory.GetCurrentDirectory();
			rootDirField.field.Width = 550;
			rootDirField.button.Content = "Open...";
			rootDirField.button.Click += async (x, y) => { rootDirField.field.Text = await OpenFolder(); };

			var blurb2 = App.TextBlock;
			blurb2.Text = "If index.html file is present in your root directory - its html will be displayed, otherwise the root directory's files will be displayed.";
			blurb2.TextAlignment = TextAlignment.Center;

			var blurb3 = App.TextBlock;
			blurb3.Text = "You can connect to the server with the following addresses (depending on which network your web client resides):";
			blurb3.TextAlignment = TextAlignment.Center;

			var startSfield = App.HorizontalFieldWithButton;
			var stopSfield = App.HorizontalFieldWithButton;

			startSfield.holder.HorizontalAlignment = HorizontalAlignment.Center;
			startSfield.label.Text = "Server Port (0-65535) (default is http 80):";
			startSfield.field.Text = "80";
			startSfield.field.GetObservable(TextBox.TextProperty).Subscribe(z => { startSfield.field.Watermark = ""; });
			startSfield.button.Content = "Start Server";
			startSfield.button.Click += ((z, zz) => {
				if (!string.IsNullOrWhiteSpace(startSfield.field.Text) && int.TryParse(startSfield.field.Text, out int num) && num >= 0 && num <= 65535) {
					lib.StartServer(num, rootDirField.field.Text);
					ipsToAddToMenu = lib.GetEnabledLocalInterfaces().Where(zzz => zzz.AddressFamily.Equals(AddressFamily.InterNetwork)).Select(zzz => { if (num.Equals(80)) { return "http://" + zzz + "/"; } else { return "http://" + zzz + ":" + num + "/"; } }).ToList();
					ipsToAddToMenu.AddRange(lib.GetEnabledLocalInterfaces().Where(zzz => zzz.AddressFamily.Equals(AddressFamily.InterNetworkV6)).Select(zzz => { if (num.Equals(80)) { return "http://[" + zzz + "]/"; } else { return "http://[" + zzz + "]" + ":" + num + "/"; } }).ToList());
					saBox.Text = string.Join("\n", ipsToAddToMenu);
					stopSfield.label.Text = "Server is RUNNING";
					stopSfield.field.Background = Brushes.LimeGreen;
					logBox[!TextBlock.TextProperty] = lib.ServerLogObservable.Select(zzz => "[" + zzz.Timestamp + "]" + " " + "[" + zzz.Level + "]" + " " + zzz.RenderMessage() + "\n" + logBox.Text).ToBinding();
				} else {
					startSfield.field.Text = "";
					startSfield.field.Watermark = "Enter valid port";
				}
			});

			stopSfield.holder.HorizontalAlignment = HorizontalAlignment.Center;
			stopSfield.label.Text = "Server is STOPPED";
			stopSfield.field.IsReadOnly = true;
			stopSfield.field.Width = 240;
			stopSfield.field.Background = Brushes.DarkRed;
			stopSfield.button.Content = "Stop Server";
			stopSfield.button.Click += ((z, zz) => { lib.StopServer(); saBox.Text = ""; stopSfield.label.Text = "Server is STOPPED"; stopSfield.field.Background = Brushes.DarkRed; });

			rootPan.Children.AddRange(new List<IControl> { blurb1, rootDirField.holder, blurb2, startSfield.holder, stopSfield.holder, blurb3, saScrollable, logScrollable });

		}

		private async Task<string> OpenFolder() {
			var dialog = new OpenFolderDialog() {
				Title = "Select Server Root Directory...",
				InitialDirectory = Directory.GetCurrentDirectory(),
			};
			var res = await dialog.ShowAsync(Application.Current.MainWindow);
			return res ?? "";
		}
	}
}