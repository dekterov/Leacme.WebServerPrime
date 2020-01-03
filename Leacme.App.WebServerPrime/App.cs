// Licensed under the MIT license. Copyright (c) 2017 Leacme (http://leac.me)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Logging.Serilog;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Default;
using Avalonia.Threading;
using Leacme.Lib.WebServerPrime;

namespace Leacme.App.WebServerPrime {
	public class App : Application {

		public Menu TopMenu { get; set; }
		public ProgressBar LoadingBar { get; set; }
		public Window AboutWindow { get; set; }
		public AppUI AppUI { get; set; }

		public override void Initialize() {
			Styles.Add(new DefaultTheme());
			Styles.Add((IStyle)new AvaloniaXamlLoader().Load(
				new Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default")));
			var dataGridType = typeof(DataGrid); // load DataGrid workaround
			Styles.Add((IStyle)new AvaloniaXamlLoader().Load(
							new Uri("resm:Avalonia.Controls.DataGrid.Themes.Default.xaml?assembly=Avalonia.Controls.DataGrid")));
			Styles.Resources.Add("ScrollBarThickness", 14);

			Window InitAboutWindow() {
				var aboutPanel = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
				var iconImg = new Image();
				var stream = new MemoryStream();
				App.Icon.Save(stream);
				stream.Seek(0, SeekOrigin.Begin);
				iconImg.Source = new Bitmap(stream);

				var ttl = App.TextBlock;
				ttl.Text = Application.Current.MainWindow.Title;

				var tbc = App.TextBlock;
				tbc.Text = "Copyright (c) 2017 Leacme (http://leac.me)";

				var sal = App.ScrollableTextBlock;
				sal.Height = 220;
				sal.Width = 380;
				((TextBlock)sal.Content).Background = Brushes.White;
				((TextBlock)sal.Content).TextAlignment = TextAlignment.Center;
				((TextBlock)sal.Content).Text = "\nUSAGE\n\n";

				var asm = typeof(Library).GetTypeInfo().Assembly;
				using (Stream rs = asm.GetManifestResourceStream(asm.GetManifestResourceNames().ToList().First(z => z.StartsWith("Leacme.Lib.") && z.EndsWith(".README.md")))) {
					using (var sr = new StreamReader(rs)) {
						var rawText = sr.ReadToEnd();
						((TextBlock)sal.Content).Text += "This application features the ability to " + rawText.Split(
							new string[] { "This application features the ability to" }, StringSplitOptions.None)[1].Split(
								"![][image_screenshot]")[0].Trim() + "\n\n";
						((TextBlock)sal.Content).Text += rawText.Split(new string[] { "## Application Usage" }, StringSplitOptions.None)[1].Split(
							"## Library Usage")[0].Trim() + "\n\n";
					}
				}
				((TextBlock)sal.Content).Text += "LICENSES\n\n" + Application.Current.MainWindow.Title + ":\n";
				using (Stream rs = asm.GetManifestResourceStream(asm.GetManifestResourceNames().ToList().First(z => z.StartsWith("Leacme.Lib.") && z.EndsWith(".LICENSE.md")))) {
					using (var sr = new StreamReader(rs)) {
						var rawText = sr.ReadToEnd();
						((TextBlock)sal.Content).Text += rawText;
					}
				}

				aboutPanel.Children.AddRange(new List<Control> { iconImg, ttl, tbc, sal });
				foreach (Control ctl in aboutPanel.Children) {
					ctl.HorizontalAlignment = HorizontalAlignment.Center;
					ctl.Margin = new Thickness(10);
				}

				var aboutWindow = new Window() {
					Title = "About " + Application.Current.MainWindow.Title,
					Height = 400,
					Width = 400,
					Background = App.Background,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					Icon = App.Icon,
					CanResize = false,
					Content = aboutPanel
				};
				return aboutWindow;
			}

			Menu InitTopMenu() {
				var topMenu = new Menu() {
					Background = App.Background,
				};

				var fileItem = new MenuItem() { Header = "File" };
				var exitItem = new MenuItem() { Header = "Exit" };
				exitItem.Click += (x, y) => { Application.Current.Exit(); };
				((AvaloniaList<object>)fileItem.Items).AddRange(new object[] { new Separator(), exitItem });

				var helpItem = new MenuItem() { Header = "Help" };
				var aboutItem = new MenuItem() { Header = "About..." };
				aboutItem.Click += async (x, y) => {
					if (!Application.Current.Windows.Contains(AboutWindow)) {
						AboutWindow = InitAboutWindow();
						await AboutWindow.ShowDialog<Window>(Application.Current.MainWindow);
					}
				};

				((AvaloniaList<object>)helpItem.Items).AddRange(new object[] { new Separator(), aboutItem });
				((AvaloniaList<object>)topMenu.Items).AddRange(new[] { fileItem, helpItem });
				foreach (MenuItem mi in topMenu.Items) {
					mi.VerticalAlignment = VerticalAlignment.Center;
					mi.Padding = new Thickness(5);
				}
				return topMenu;
			}

			TopMenu = InitTopMenu();

			ProgressBar InitProgressBar() {
				var pb = new ProgressBar() { IsIndeterminate = false };
				pb.Styles.Add(new Style(x => x.OfType<ProgressBar>()) {
					Setters = new[] {
						new Setter(TabControl.BackgroundProperty, new SolidColorBrush(Colors.Transparent)),
						new Setter(TabControl.ForegroundProperty, new SolidColorBrush(Colors.Gainsboro)),
						}
				});
				return pb;
			}

			LoadingBar = InitProgressBar();
		}

		public static Thickness Margin { get; set; } = new Thickness(3);

		public static Button Button {
			get {
				return new Button() {
					Content = "Default",
					Background = Background,
					BorderBrush = Brushes.DarkSlateGray,
					Width = 130,
					Margin = Margin,
					Cursor = new Cursor(StandardCursorType.Hand),
				};
			}
		}

		public static IBrush Background {
			get {
				return new LinearGradientBrush {
					StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
					EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
					GradientStops = { new GradientStop { Color = Colors.White, Offset = 0 }, new GradientStop { Color = Colors.Gainsboro, Offset = 1 } }
				};
			}
		}

		public static TextBlock TextBlock {
			get {
				return new TextBlock() { Text = "Default", VerticalAlignment = VerticalAlignment.Center, Margin = Margin, TextWrapping = TextWrapping.Wrap };
			}
		}

		public static TextBox TextBox {
			get {
				return new TextBox() {
					Width = 100, VerticalAlignment = VerticalAlignment.Center, Margin = Margin
				};
			}
		}

		public static StackPanel HorizontalStackPanel {
			get {
				return new StackPanel() {
					Orientation = 0, Margin = Margin
				};
			}
		}

		public static (StackPanel holder, TextBlock label, TextBox field, Button button) HorizontalFieldWithButton {
			get {
				var holder = HorizontalStackPanel;
				var label = App.TextBlock;
				var field = App.TextBox;
				var button = App.Button;
				holder.Children.AddRange(new List<Control>() {
				label, field, button });
				return (holder, label, field, button);
			}
		}

		public static CheckBox CheckBox {
			get {
				return new CheckBox() { Margin = Margin };
			}
		}

		public static Slider Slider {
			get {
				return new Slider() { Margin = Margin, Width = 80, Minimum = 1, Maximum = 10, IsSnapToTickEnabled = true, TickFrequency = 1 };
			}
		}

		public static (StackPanel holder, CheckBox checkBox, TextBlock label) HorizontalCheckBoxEntry {
			get {
				var holder = HorizontalStackPanel;
				var checkBox = App.CheckBox;
				var label = App.TextBlock;
				holder.Children.AddRange(new List<Control>() { checkBox, label });
				return (holder, checkBox, label);
			}
		}

		public static (StackPanel holder, Slider slider, TextBlock value) HorizontalSliderWithValue {
			get {
				var holder = HorizontalStackPanel;
				var slider = App.Slider;
				var value = App.TextBlock;
				value.Background = new SolidColorBrush(Colors.White);
				value.Width = 40;
				value.Text = slider.Value.ToString();
				slider.PropertyChanged += (x, y) => {
					if (y.Property.Equals(Slider.ValueProperty)) {
						value.Text = slider.Value.ToString();
					}
				};
				holder.Children.AddRange(new List<Control>() { slider, value });
				return (holder, slider, value);
			}
		}

		public static DataGrid DataGrid {
			get {
				var dg = new DataGrid() {
					Height = 370, Width = 800,
					AutoGenerateColumns = true,
					Margin = App.Margin,
					BorderBrush = Brushes.LightSlateGray,
					RowBackground = Brushes.White,
					AlternatingRowBackground = Brushes.WhiteSmoke,
					IsReadOnly = true,
					VerticalGridLinesBrush = Brushes.LightGray
				};
				dg.Styles.Add(new Style(x => x.OfType<DataGridRowsPresenter>()) {
					Setters = new[] {
						new Setter(DataGridRowsPresenter.BackgroundProperty, new SolidColorBrush(Colors.White)) }
				});
				return dg;
			}
		}

		public static (StackPanel holder, TextBlock label, ComboBox comboBox) ComboBoxWithLabel {
			get {
				var panel = HorizontalStackPanel;
				var label = TextBlock;
				var comboBox = new ComboBox() { Margin = Margin, Width = 200, MinHeight = 25, Background = Brushes.White };
				panel.Children.AddRange(new List<IControl>() { label, comboBox });
				return (panel, label, comboBox);
			}
		}

		public static (StackPanel holder, TextBlock label, AutoCompleteBox acBox) AutoCompleteWithLabel {
			get {
				var panel = HorizontalStackPanel;
				var label = TextBlock;
				var acBox = new AutoCompleteBox() { Margin = Margin, Width = 200, MinHeight = 25, IsTextCompletionEnabled = true };
				panel.Children.AddRange(new List<IControl>() { label, acBox });
				return (panel, label, acBox);
			}
		}

		public static (StackPanel holder, Carousel carousel, Button left, Button right, DispatcherTimer flipTimer) Carousel {
			get {
				StackPanel holder = App.HorizontalStackPanel;
				Carousel carousel = new Carousel() {
					Margin = App.Margin,
					Height = 300,
					Width = 300,
					Background = Brushes.White,
					BorderThickness = new Thickness(1),
					BorderBrush = Brushes.LightSlateGray,
					PageTransition = new CrossFade(TimeSpan.FromSeconds(0.25)),
				};
				Button left = App.Button;
				left.Content = "<";
				left.Width = left.Height;
				left.Click += (z, zz) => {
					PreviousCyclical();
				};

				void PreviousCyclical() {
					if (carousel.SelectedIndex != 0) {
						carousel.Previous();
					} else {
						carousel.SelectedIndex = carousel.Items.Cast<IControl>().Count() - 1;
					}
				}
				Button right = App.Button;
				right.Content = ">";
				right.Width = right.Height;
				right.Click += (z, zz) => {
					NextCyclical();
				};
				void NextCyclical() {
					if (carousel.SelectedIndex != (carousel.Items.Cast<IControl>().Count() - 1)) {
						carousel.Next();
					} else {
						carousel.SelectedIndex = 0;
					}
				}
				var flipTimer = new DispatcherTimer(new TimeSpan(0, 0, 0, 4, 0), DispatcherPriority.Normal, (z, zz) => { NextCyclical(); });
				holder.Children.AddRange(new List<IControl> { left, carousel, right });
				return (holder, carousel, left, right, flipTimer);
			}
		}

		public static ScrollViewer ScrollViewer {
			get {
				return new ScrollViewer {
					Background = Brushes.White,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				};
			}
		}

		public static ScrollViewer ScrollableTextBlock {
			get {
				var stb = App.ScrollViewer;
				stb.Background = Brushes.Transparent;
				var tb = App.TextBlock;
				tb.Width = stb.Width - App.Margin.Top * 6;
				stb.Content = tb;
				return stb;
			}
		}

		public static TabControl TabControl {
			get {
				Application.Current.Styles.First(z => z.TryGetResource("FontSizeNormal", out var zz)).TryGetResource("FontSizeNormal", out var fontSizeNormal);
				Application.Current.Styles.Add(new Style(x => x.OfType<TabItem>()) {
					Setters = new[] {
						new Setter(TabControl.FontSizeProperty, fontSizeNormal),
						new Setter(TabItem.ForegroundProperty, new SolidColorBrush(Colors.LightSlateGray)),
						new Setter(TabItem.BorderThicknessProperty, new Thickness(0.5,0.5,0.5,0)),
						new Setter(TabItem.BorderBrushProperty, Brushes.LightSlateGray)
					}
				});
				Application.Current.Styles.Add(new Style(x => x.OfType<TabItem>().Class(":selected").Template().OfType<ContentPresenter>()) {
					Setters = new[] {
						new Setter(TabItem.BackgroundProperty, new SolidColorBrush(Colors.White)),
						new Setter(TabItem.ForegroundProperty, new SolidColorBrush(Colors.DarkSlateGray)),
					},
				});

				var tabControl = new TabControl() {
					Height = 300,
					Background = new SolidColorBrush(Colors.White),
					Padding = new Thickness(0),
				};
				tabControl.Styles.Add(new Style(x => x.OfType<TabControl>().Descendant().OfType<WrapPanel>()) {
					Setters = new[] {
						new Setter(TabControl.BackgroundProperty, new SolidColorBrush(Colors.WhiteSmoke))
						}
				});
				((AvaloniaList<object>)tabControl.Items).AddRange(new List<TabItem> {
					new TabItem() { Header = "Default", Content = App.ScrollViewer },
					new TabItem() { Header = "Default", Content = App.ScrollViewer } });
				return tabControl;
			}
		}

		public static WindowIcon Icon { get; set; } = new WindowIcon(
						new MemoryStream(Convert.FromBase64String(@"iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAMAA
				ABg3Am1AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAABhQTFRFEQ0G3t7M+acA+tRxaVUglotnyq9Z////r2Cv
				cgAAAAh0Uk5T/////////wDeg71ZAAABv0lEQVR42pyWi5LDIAhFSQX6/3+8XJSIjzTJMtOpSTgCKiB9XwrtX2v7PQSYqjwC2IRITZg
				wvgPEp2YAsjVDkytSIOoURjwzNM4uWqQ6BcJFzLULwHTa5F3Mogw2+tBcUGh/IG4g/ksm+oilIE52AF5x/Asnr+j03yKk0P/goQ0Ji9
				xtUEyvRbmpJAs+xjLDUAYsOHt36p8xNIDsIyULvv4R7xbAd84A4uoasQ/pGRNmoPA0o0uyKBF3B5I3Wg4TGV20NT9OAPvfv3EAfdnMq
				u14WKC+wwDY1QEkpzgBAqBvwRXgx6BbyED1aAfQFkDQpSyAWWbauYRDYblzlBXYW/B9eAXAK2TnJSDPge//LDwEVF+6pKgPu6PRVmkB
				Stq44fBtgeHw4UO5AWg4rVMCrTFEQZtS9AYwr4+xCNwAOUVrmTnL3idymnuZKUPVwEKhkMWMPBQB32QVHipfXRiicQOD1eKpMNRWlML
				qFafUR4ES8apISzF2r2ioYP4QHWntDzU114bifWvXgZCa0bJOsWdBK7tqWa0pZpla6dRVqx/adFtN/tWna+EWrVLV5ebqMAb97K6hP6
				4n9Pb68yfAADWzIa5MAn9DAAAAAElFTkSuQmCC")));

		public static Window NotificationWindow {
			get {
				var nftPanel = new StackPanel() {
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center
				};

				var aboutWindow = new Window() {
					Title = "Notification ",
					Height = 200,
					Width = 300,
					Background = App.Background,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					Icon = App.Icon,
					CanResize = false,
					Content = nftPanel
				};

				return aboutWindow;
			}
		}

		public static void Main(string[] args) {
			var stop = new CancellationTokenSource();
			var ab = Avalonia.AppBuilder.Configure<App>().UsePlatformDetect().LogToDebug().UseReactiveUI().SetupWithoutStarting();

			ab.Instance.MainWindow = InitMainWindow();
			ab.Instance.MainWindow.Closed += (x, y) => stop.Cancel();
			Console.WriteLine(ab.Instance.MainWindow.Title);
			ab.Instance.MainWindow.Show();
			ab.Instance.MainWindow.Activate();

			Window InitMainWindow() {
				var rp = new StackPanel();
				rp.Children.Add(((App)ab.Instance).TopMenu);
				rp.Children.Add(((App)ab.Instance).LoadingBar);
				Window window = new Window() {
					Title = "Leacme WebServerPrime",
					Height = 540,
					Width = 960,
					Content = rp,
					Background = App.Background,
					WindowStartupLocation = WindowStartupLocation.CenterScreen,
					Icon = App.Icon,
				};
				return window;
			}

			void InitUI() {
				((App)ab.Instance).AppUI = new AppUI();
			}
			InitUI();
			ab.Instance.Run(stop.Token);
		}
	}
}