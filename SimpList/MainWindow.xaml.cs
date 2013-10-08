using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SimpList {
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
			buttonClose.Click += (o, e) => { isShutdown = true; AnimateWindow(0, 300); };
			gridTitlebar.MouseDown += (o, e) => DragMove();
			gridPopup.MouseDown += (o, e) => {
				strAnswer = "cancel";
				HideMessageBox();
			};
			buttonAdd.Click += (o, e) => {
				if (nowView == "Season") {
					ShowMessageBox("formaddseason", "");
				} else {
					ShowMessageBox("formaddarchive", "");
				}
				textboxTitle.Focus();
			};

			bool isShowAll = false;
			buttonToggle.Click += (o, e) => {
				isShowAll = !isShowAll;

				if (isShowAll) {
					((Image)buttonToggle.Content).Source = Functions.rtSource("favsoff.png");
					foreach (Grid grid in stackArchive.Children) {
						grid.Visibility = Visibility.Visible;
					}
				} else {
					((Image)buttonToggle.Content).Source = Functions.rtSource("favs.png");
					int nLinkID;
					foreach (Grid grid in stackArchive.Children) {
						nLinkID = (int)grid.Tag;
						if (DataStruct.dictArchive[nLinkID].Episode < 0) {
							grid.Visibility = Visibility.Collapsed;
						}
					}
				}
				scrollArchive.ScrollToTop();
			};

			gridMain.PreviewKeyDown += (o, e) => e.Handled = true;
			this.Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right - 400;
			this.Top = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height / 2 - 300;

			gridList.RenderTransformOrigin = new Point(0.5, 0.5);
			gridList.RenderTransform = new ScaleTransform(1, 1);

			gridMessage.RenderTransformOrigin = new Point(0.5, 0.5);
			gridMessage.RenderTransform = new ScaleTransform(1, 1);

			CustomControl.mColor = FindResource("sColor") as SolidColorBrush;
		}

		// Global variables
		string strWeek = "일월화수목금토"; string nowView = "Season";
		Button[] arrayWeekButton = new Button[7];

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			for (int i = 0; i < 7; i++) { arrayWeekButton[i] = (Button)stackSeason.Children[i]; }

			// If not exists, make file and folder.
			FileClass.InitFiles();

			// Parse season and list data from file
			FileClass.GetListData();
			FileClass.GetSeasonData();

			foreach (KeyValuePair<int, sctArchive> kData in DataStruct.dictArchive) {
				sctArchive sData = kData.Value;
				int nIndex = OrderProcess.GetArchiveItemIndex(sData.Title);

				Grid grid = GetSeasonListItem(sData.Title, sData.ID, false);
				stackArchive.Children.Insert(nIndex, grid);

				if (sData.Episode < 0) {
					grid.Visibility = Visibility.Collapsed;
				}
			}
			foreach (KeyValuePair<int, sctSeason> kData in DataStruct.dictSeason) {
				sctSeason sData = kData.Value;

				int nIndex = OrderProcess.GetSeasonItemIndex(sData.SortTag);
				Grid grid = GetSeasonListItem(sData.Title, sData.LinkID, true);
				stackSeason.Children.Insert(nIndex, grid);
			}

			RefreshWeekdayData();
			RefreshComboboxSource();

			gridRoot.RenderTransformOrigin = new Point(1, 0.5);
			gridRoot.RenderTransform = new ScaleTransform(0, 0);
			AnimateWindow(0, 0);

			AnimateWindow(1, 300);
		}

		private void RefreshWeekdayData() {
			string strWeekData = OrderProcess.CheckWeekdayExists();
			for (int i = 0; i < 7; i++) {
				if (strWeekData[i] == '1') {
					arrayWeekButton[i].Visibility = Visibility.Visible;
				} else {
					arrayWeekButton[i].Visibility = Visibility.Collapsed;
				}
			}
		}

		private void ButtonPopup_Click(object sender, RoutedEventArgs e) {
			Button btn = (Button)sender;

			string[] strSplit = ((string)btn.Tag).Split(new string[] { "-=-" }, StringSplitOptions.RemoveEmptyEntries);

			string strType = strSplit[0];
			string strTag = strSplit[1];

			if (strType == "web") {
				Process pro = new Process() {
					StartInfo = new ProcessStartInfo() {
						FileName = new UriBuilder(strTag).Uri.ToString(),
					}
				};
				pro.Start();
				return;
			}
			string strCaption = strSplit[2];

			if (strType == "download") {
				string downLink = strTag;
				string downName = strCaption;
				string nowTime = DateTime.Now.ToString().Replace(':', '_');
				string downPath = FileClass.ffFolder + nowTime + "_" + downName;

				WebClient webClient = new WebClient() { Proxy = null };
				webClient.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.76 Safari/537.36");
				Uri uri = new Uri(downLink);
				webClient.DownloadFileCompleted += webClient_DownloadFileCompleted;
				webClient.DownloadFileAsync(uri, downPath, downPath);

				return;
			}

			
			RunCommandPopup(strType, strTag, strCaption);
		}

		private void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e) {
			if (e.Error != null) {
				MessageBox.Show(e.Error.Message);
				return;
			}
			string strPath = (string)e.UserState;
			string[] strExt = strPath.Split('.');

			if (strExt[strExt.Length - 1].ToLower() == "smi") {
				strExt = strPath.Split('\\');

				SaveFileDialog saveDialog = new SaveFileDialog();
				saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				saveDialog.FileName = strExt[strExt.Length - 1];
				saveDialog.Title = "Save As...";

				if (saveDialog.ShowDialog() == true) {
					File.Copy(strPath, saveDialog.FileName, true);
				}

			} else {
				Process pro = new Process() {
					StartInfo = new ProcessStartInfo() {
						FileName = strPath
					}
				};
				pro.Start();
			}
		}

		private void btnBase_MouseDown(object sender, MouseButtonEventArgs e) {
			string[] strSplt = ((string)((Button)sender).Tag).Split('_');

			string strName = strSplt[0];
			int nID = Convert.ToInt32(strSplt[1]);
			int nType = -1;
			if (strSplt.Length == 3) { nType = Convert.ToInt32(strSplt[2]); }

			if (e.MiddleButton == MouseButtonState.Pressed) {
				RunCommandMain("Clipboard", nID, nType);
			} else if (e.RightButton == MouseButtonState.Pressed) {
				RunCommandMain("Folder", nID, nType);
			}
		}

		private Grid GetSeasonListItem(string strName, int nLinkID, bool isSeason) {
			Grid gridBase = new Grid() { Tag = nLinkID, Width = 350, Height = 40, Background = Brushes.Transparent, Margin = new Thickness(0, 0, 0, 0) };
			string strIsSeason = isSeason ? "1" : "0";

			// Option contents

			Grid gridOption = new Grid() {
				Width = 80, Height = 40, Margin = new Thickness(350, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left,
				Opacity = 0,
			};
			Button btnModify = new Button() {
				Width = 40, Height = 40, Margin = new Thickness(0, 0, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Style = FindResource("FlatButton") as Style,
				Content = new Image() { Width = 40, Height = 40, Source = Functions.rtSource("settings.png") },
				Background = Brushes.Transparent,
				Tag = "Modify_" + nLinkID + "_" + strIsSeason,
			};
			Button btnRemove = new Button() {
				Width = 40, Height = 40, Margin = new Thickness(40, 0, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Style = FindResource("FlatButton") as Style,
				Content = new Image() { Width = 40, Height = 40, Source = Functions.rtSource("delete.png") }, Background = Brushes.Transparent,
				Tag = "Remove_" + nLinkID + "_" + strIsSeason,
			};
			gridOption.Children.Add(btnModify);
			gridOption.Children.Add(btnRemove);

			// Option Indicator

			Button btnIndicator = new Button() {
				Width = 15, Height = 40, Margin = new Thickness(0, 0, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Style = FindResource("FlatButton") as Style,
				Content = new Image() { Width = 15, Height = 21, Source = Functions.rtSource("indicator.png") }, Background = Brushes.Transparent,
				Tag = nLinkID,
				Visibility = Visibility.Collapsed,
			};

			// Titlebar

			TextBlock txtName = new TextBlock() {
				Text = strName, IsHitTestVisible = false,
				FontSize = 13.33, Width = 240,
				VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(20, 0, 10, 0), TextTrimming = TextTrimming.CharacterEllipsis,

				Foreground = DataStruct.dictArchive[nLinkID].Episode < 0 ? Brushes.LightGray : Brushes.Black
			};

			// Underbar & Invisible buttons

			Rectangle rectUnderbar = new Rectangle() {
				Width = 310, Height = 1, Fill = FindResource("sColor") as Brush, 
				HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
				Opacity = 0.5,
			};
			Button btnBase = new Button() {
				Width = 350, Height = 40, HorizontalContentAlignment = HorizontalAlignment.Left,
				Style = FindResource("FlatButton") as Style, Background = Brushes.Transparent,
				Tag = "Select_" + nLinkID + "_" + strIsSeason,
			};
			Button btnReturn = new Button() {
				Width = 350, Height = 40, HorizontalContentAlignment = HorizontalAlignment.Left,
				Style = FindResource("FlatButton") as Style, Background = Brushes.Transparent,
				Tag = nLinkID, Visibility = Visibility.Collapsed,
			};

			// Episode area

			Grid gridEpisode = new Grid() { Margin = new Thickness(255, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
			Button btnLeft = new Button() {
				Width = 25, Height = 30, Margin = new Thickness(0, 0, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Style = FindResource("FlatButton") as Style, Background = Brushes.Transparent,
				Content = new Image() { Width = 11, Height = 17, Source = Functions.rtSource("arrowleft.png") },
				Tag = "CountDown_" + nLinkID,
			};

			TextBlock txtEpisode = new TextBlock() {
				Text = DataStruct.dictArchive[nLinkID].Episode >= 0 ? DataStruct.dictArchive[nLinkID].Episode + "화" : "-",
				FontSize = 12, Width = 30,
				VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(25, 10, 0, 10), Background = Brushes.Transparent,
			};

			Button btnRight = new Button() {
				Width = 25, Height = 30, Margin = new Thickness(55, 0, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Transparent,
				Style = FindResource("FlatButton") as Style,
				Content = new Image() { Width = 11, Height = 17, Source = Functions.rtSource("arrowright.png") },
				Tag = "CountUp_" + nLinkID,
			};
			gridEpisode.Children.Add(btnLeft);
			gridEpisode.Children.Add(txtEpisode);
			gridEpisode.Children.Add(btnRight);
			if (DataStruct.dictArchive[nLinkID].Episode < 0) { gridEpisode.Visibility = Visibility.Collapsed; }

			// Event handlers 

			gridBase.MouseEnter += (o, e) => btnIndicator.Visibility = Visibility.Visible;
			gridBase.MouseLeave += (o, e) => btnIndicator.Visibility = Visibility.Collapsed;

			btnLeft.Click += Button_Click;
			btnRight.Click += Button_Click;
			btnBase.Click += Button_Click;
			btnBase.MouseDown += btnBase_MouseDown;
			btnModify.Click += Button_Click;
			btnRemove.Click += Button_Click;

			DispatcherTimer dptHold = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(500) };
			btnLeft.PreviewMouseDown += (o, e) => dptHold.Start();
			btnLeft.MouseUp += (o, e) => dptHold.Stop();
			btnLeft.MouseLeave += (o, e) => dptHold.Stop();
			dptHold.Tick += (o, e) => {
				dptHold.Stop();
				strClickmode = "Hold";
				RunCommandMain("Hold", nLinkID, 0);
			};

			// Indicator Events

			btnIndicator.Click += (o, e) => {
				gridEpisode.Visibility = Visibility.Collapsed;
				btnReturn.Visibility = Visibility.Visible;
				gridOption.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(255, 0, 0, 0), TimeSpan.FromMilliseconds(200)) {
					EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut },
					BeginTime = TimeSpan.FromMilliseconds(100)
				});
				gridOption.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
			};
			btnReturn.Click += (o, e) => {
				int nIndex = (int)((Button)o).Tag;

				if (DataStruct.dictArchive[nIndex].Episode >= 0) { gridEpisode.Visibility = Visibility.Visible; }
				btnReturn.Visibility = Visibility.Collapsed;

				gridOption.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(330, 0, 0, 0), TimeSpan.FromMilliseconds(200)) {
					EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut },
				});
				gridOption.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)) {
				});
			};

			gridBase.Children.Add(btnBase);
			gridBase.Children.Add(btnIndicator);

			gridBase.Children.Add(txtName);

			gridBase.Children.Add(gridEpisode);

			gridBase.Children.Add(rectUnderbar);
			gridBase.Children.Add(btnReturn);
			gridBase.Children.Add(gridOption);

			int nUIIndex = nLinkID * 10;
			if (isSeason) { nUIIndex += 1; }

			UIComponent uicomp = new UIComponent() {
				txtTitle = txtName, txtEpisode = txtEpisode,
				gEpisode = gridEpisode, gBase = gridBase, gOption = gridOption,
				bReturn = btnReturn
			};
			DataStruct.dictUI.Add(nUIIndex, uicomp);

			return gridBase;
		}

		private void buttonView_Click(object sender, RoutedEventArgs e) {
			if (((Button)sender).Name == "buttonLeftView") {
				buttonLeftView.Visibility = Visibility.Collapsed;
				buttonRightView.Visibility = Visibility.Visible;
				buttonToggle.Visibility = Visibility.Collapsed;
				nowView = "Season";

				textSeasonView.BeginAnimation(TextBlock.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
				textArchiveView.BeginAnimation(TextBlock.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));

				gridSeason.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(0), TimeSpan.FromMilliseconds(600)) {
					EasingFunction = new ExponentialEase() { Exponent = 15, EasingMode = EasingMode.EaseOut },
					BeginTime = TimeSpan.FromMilliseconds(200)
				});
				gridArchive.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(350, 0, 0, 0), TimeSpan.FromMilliseconds(600)) {
					EasingFunction = new ExponentialEase() { Exponent = 15, EasingMode = EasingMode.EaseOut },
					BeginTime = TimeSpan.FromMilliseconds(200)
				});
			} else {
				buttonLeftView.Visibility = Visibility.Visible;
				buttonRightView.Visibility = Visibility.Collapsed;
				buttonToggle.Visibility = Visibility.Visible;
				nowView = "Archive";

				textSeasonView.BeginAnimation(TextBlock.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
				textArchiveView.BeginAnimation(TextBlock.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));

				gridSeason.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(100, 0, 0, 0), TimeSpan.FromMilliseconds(600)) {
					EasingFunction = new ExponentialEase() { Exponent = 15, EasingMode = EasingMode.EaseOut },
					BeginTime = TimeSpan.FromMilliseconds(200)
				});
				gridArchive.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(0), TimeSpan.FromMilliseconds(600)) {
					EasingFunction = new ExponentialEase() { Exponent = 10, EasingMode = EasingMode.EaseOut },
					BeginTime = TimeSpan.FromMilliseconds(200)
				});
			}
		}

		string strClickmode = "";
		private void Button_Click(object sender, RoutedEventArgs e) {
			string[] strSplt = ((string)((Button)sender).Tag).Split('_');
			if (strClickmode == "Hold") { return; }

			string strName = strSplt[0];
			int nID = Convert.ToInt32(strSplt[1]);
			int nType = -1;
			if (strSplt.Length == 3) { nType = Convert.ToInt32(strSplt[2]); }

			RunCommandMain(strName, nID, nType);
		}

		private async void RunCommandMain(string strCmd, int nArg, int nType) {
			int nSeasonIndex = nArg * 10, nArchiveIndex = nArg * 10, nCommonIndex = nArg * 10;
			if (DataStruct.dictUI.ContainsKey(nSeasonIndex + 1)) { nSeasonIndex = nSeasonIndex + 1; }
			if (nType == 1) { nCommonIndex = nCommonIndex + 1; }

			sctArchive sData = DataStruct.dictArchive[nArg];
			Task<bool> taskMessage;
			bool isOK;

			switch (strCmd) {
				case "Select":
					if (DataStruct.dictArchive[nArg].Episode < 0) {

						strAnswer = "";
						ShowMessageBox("alert", "Do you want to enable this item?");
						taskMessage = GetAnswer();
						isOK = await taskMessage;

						if (!isOK) { return; }

						DataStruct.dictUI[nSeasonIndex].gEpisode.Visibility = Visibility.Visible;
						DataStruct.dictUI[nArchiveIndex].gEpisode.Visibility = Visibility.Visible;

						DataStruct.dictUI[nSeasonIndex].txtTitle.Foreground = Brushes.Black;
						DataStruct.dictUI[nArchiveIndex].txtTitle.Foreground = Brushes.Black;

						sData.Season = 1; sData.Episode = 0;
						DataStruct.dictUI[nSeasonIndex].txtEpisode.Text = sData.Episode + "화";
						DataStruct.dictUI[nArchiveIndex].txtEpisode.Text = sData.Episode + "화";
					} else {
						if (nType == 1) {
							RunCommandPopup("torrent", DataStruct.dictSeason[nArg].Keyword, string.Format("({0}화) {1}", DataStruct.dictArchive[nArg].Episode, DataStruct.dictSeason[nArg].Title));
						}
					}
					break;
				case "CountDown":
					if (DataStruct.dictArchive[nArg].Episode == 0) {

						strAnswer = "";
						ShowMessageBox("alert", "Do you want to disable this item?");
						taskMessage = GetAnswer();
						isOK = await taskMessage;

						if (!isOK) { return; }

						DataStruct.dictUI[nSeasonIndex].gEpisode.Visibility = Visibility.Collapsed;
						DataStruct.dictUI[nArchiveIndex].gEpisode.Visibility = Visibility.Collapsed;

						DataStruct.dictUI[nSeasonIndex].txtTitle.Foreground = Brushes.LightGray;
						DataStruct.dictUI[nArchiveIndex].txtTitle.Foreground = Brushes.LightGray;

						sData.Season = sData.Episode = -1;

					} else {
						sData.Episode--;

						DataStruct.dictUI[nSeasonIndex].txtEpisode.Text = sData.Episode + "화";
						DataStruct.dictUI[nArchiveIndex].txtEpisode.Text = sData.Episode + "화";
					}
					break;
				case "Hold":
					strAnswer = "";
					ShowMessageBox("alert", "Do you want to disable this item?");
					taskMessage = GetAnswer();
					isOK = await taskMessage;

					strClickmode = "";
					if (!isOK) { return; }

					DataStruct.dictUI[nSeasonIndex].gEpisode.Visibility = Visibility.Collapsed;
					DataStruct.dictUI[nArchiveIndex].gEpisode.Visibility = Visibility.Collapsed;

					DataStruct.dictUI[nSeasonIndex].txtTitle.Foreground = Brushes.LightGray;
					DataStruct.dictUI[nArchiveIndex].txtTitle.Foreground = Brushes.LightGray;

					sData.Season = sData.Episode = -1;
					break;
				case "CountUp":
					sData.Episode++;

					DataStruct.dictUI[nSeasonIndex].txtEpisode.Text = sData.Episode + "화";
					DataStruct.dictUI[nArchiveIndex].txtEpisode.Text = sData.Episode + "화";
					break;
				case "Modify":

					if (nType == 0) {
						textFormType.Text = "Modify archive data";
						textboxTitle.Text = DataStruct.dictArchive[nArg].Title;
						textboxTitle.Tag = DataStruct.dictArchive[nArg].Title;

						stackSeasonForm.Visibility = Visibility.Collapsed;

						nNowEdit = nArchiveIndex;
					} else {
						textFormType.Text = "Modify season data";
						textboxTitle.Text = DataStruct.dictSeason[nArg].Title;
						textboxTitle.Tag = DataStruct.dictSeason[nArg].Title;

						stackSeasonForm.Visibility = Visibility.Visible;

						textShowTime.Text = "Time : " + DataStruct.dictSeason[nArg].SortTag.Substring(2);
						comboboxWeekday.Visibility = Visibility.Collapsed;
						textboxHour.Visibility = Visibility.Collapsed;
						textboxMinute.Visibility = Visibility.Collapsed;

						textLinked.Text = "Linked : " + DataStruct.dictArchive[nArg].Title;
						comboboxLink.Visibility = Visibility.Collapsed;

						textboxSearchTag.Text = DataStruct.dictSeason[nArg].Keyword;
						textboxSearchTag.Tag = DataStruct.dictSeason[nArg].Keyword;

						nNowEdit = nSeasonIndex;
					}

					ShowMessageBox("formmodify", "");
					break;
				case "Remove":

					bool isLinked = false;

					if (nType == 0) {
						if (DataStruct.dictUI.ContainsKey(nArchiveIndex + 1)) {
							isLinked = true;

							strAnswer = "";
							ShowMessageBox("alert", "It was linked with season data.\nAre you okay?");
							taskMessage = GetAnswer();
							isOK = await taskMessage;

							if (!isOK) { return; }
							//if (MessageBox.Show("It have linked component in season data.\nAre you okay?", "Question", MessageBoxButton.OKCancel) != MessageBoxResult.OK) { return; }
						}

						stackArchive.Children.Remove(DataStruct.dictUI[nArchiveIndex].gBase);
						OrderProcess.DeleteArchive(nArg);

						RefreshWeekdayData();
						if (!isLinked) { return; }
					}

					if (DataStruct.dictArchive.ContainsKey(nArg)) {
						sctArchive replaceData = DataStruct.dictArchive[nArg];
						replaceData.isLinked = false;
						DataStruct.dictArchive[nArg] = replaceData;
					}
					stackSeason.Children.Remove(DataStruct.dictUI[nSeasonIndex].gBase);
					OrderProcess.DeleteSeason(nArg);

					RefreshWeekdayData();
					RefreshComboboxSource();
					break;
				case "Clipboard":

					string strAdd = "";
					if (DataStruct.dictArchive[nArg].Episode >= 0) { strAdd = " - " + DataStruct.dictArchive[nArg].Episode.ToString("00"); }
					Clipboard.SetDataObject(DataStruct.dictUI[nCommonIndex].txtTitle.Text + strAdd);
					DataStruct.dictUI[nCommonIndex].txtTitle.BeginAnimation(TextBlock.OpacityProperty, new DoubleAnimation(0.1, 1, TimeSpan.FromMilliseconds(500)));

					break;
				case "Folder":
					// My customization

					
					string path = @"X:\Anime\" + DataStruct.dictArchive[nArg].Title;
					string subPath = "";
					if (DataStruct.dictArchive[nArg].Season > 1) {
						subPath = @"\" + DataStruct.dictArchive[nArg].Title + "기";
					}

					double dbRepeat = 1;
					if (!Directory.Exists(path + subPath) && !Directory.Exists(path)) { dbRepeat = 2; }

					DataStruct.dictUI[nCommonIndex].txtTitle.BeginAnimation(TextBlock.OpacityProperty,
							new DoubleAnimation(0.1, 1, TimeSpan.FromMilliseconds(500 / dbRepeat)) {
								RepeatBehavior = new RepeatBehavior(dbRepeat)
							});

					if (dbRepeat == 2) { return; }
					if (Directory.Exists(path + subPath)) { path += subPath; }

					Process pro = new Process() { StartInfo = new ProcessStartInfo() { FileName = path } };
					pro.Start();

					/*
					// Common function (open option)
					DataStruct.dictUI[nCommonIndex].gEpisode.Visibility = Visibility.Collapsed;
					DataStruct.dictUI[nCommonIndex].bReturn.Visibility = Visibility.Visible;
					DataStruct.dictUI[nCommonIndex].gOption.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(255, 0, 0, 0), TimeSpan.FromMilliseconds(200)) {
						EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut }
					});
					DataStruct.dictUI[nCommonIndex].gOption.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
					*/

					break;
			}

			DataStruct.dictArchive[nArg] = sData;

			FileClass.SaveData();
		}

		string strLoading = "", strNowPopupView = "";
		Dictionary<string, string> dictPopupTitle = new Dictionary<string, string>();

		private async void RunCommandPopup(string strType, string strTag, string strCaption) {
			//MessageBox.Show(strType + "\n" + strTag);
			string strNowLoading = strType + strTag + strCaption;
			strLoading = strNowLoading;
			strNowPopupView = strType;

			dictPopupTitle[strNowPopupView] = strCaption;

			gridList.Visibility = Visibility.Visible;
			gridListWeekday.Visibility = Visibility.Collapsed;
			gridListMaker.Visibility = Visibility.Collapsed;
			gridListFiles.Visibility = Visibility.Collapsed;

			gridMessage.Visibility = Visibility.Collapsed;
			textListMessage.Text = strCaption;

			switch (strType) {
				case "weekday":
					stackListWeekday.Children.Clear();
					break;
				case "torrent":
					stackListTorrent.Children.Clear();
					break;
				case "maker":
					stackListMaker.Children.Clear();
					break;
				case "filelist":
					stackListFiles.Children.Clear();
					break;
			}

			if (strType == "weekday" || strType == "torrent") {
				AnimateBox(gridList, 1.5, 1, 0.7, 1);
				gridPopup.IsHitTestVisible = true;
				gridPopup.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)) {
					BeginTime = TimeSpan.FromMilliseconds(100)
				});
			} else {
			}

			Task<List<SctListItem>> httpTask;
			List<SctListItem> listGetData;

			switch (strType) {
				case "torrent":
					gridTorrentArea.Visibility = Visibility.Visible;
					gridSmiArea.Visibility = Visibility.Collapsed;
					buttonReturnList.Visibility = Visibility.Collapsed;
					imgLoadIndicator.Visibility = Visibility.Visible;

					httpTask = Network.GetTorrentList(strTag);
					listGetData = await httpTask;

					if (strNowLoading != strLoading) { return; }
					imgLoadIndicator.Visibility = Visibility.Collapsed;

					foreach (SctListItem sItem in listGetData) {
						Button buttonItem = new Button() { Width = 310, Height = 40, Background = Brushes.Transparent, Style = FindResource("FlatButton") as Style, };
						CustomControl.GetPopupListItem(buttonItem, sItem.Caption, "download-=-" + sItem.Tag + "-=-" + Functions.GetMD5Hash(sItem.Tag) + ".torrent", sItem.Memo);

						buttonItem.ToolTip = sItem.Caption;
						buttonItem.Click += ButtonPopup_Click;

						stackListTorrent.Children.Add(buttonItem);
					}

					break;

				case "weekday":
					gridTorrentArea.Visibility = Visibility.Collapsed;

					gridSmiArea.Visibility = Visibility.Visible;
					gridListWeekday.Visibility = Visibility.Visible;
					gridListMaker.Visibility = Visibility.Collapsed;
					gridListFiles.Visibility = Visibility.Collapsed;
					
					buttonReturnList.Visibility = Visibility.Collapsed;
					imgLoadIndicator.Visibility = Visibility.Visible;

					gridListWeekday.IsHitTestVisible = true;
					gridListMaker.IsHitTestVisible = false;
					gridListFiles.IsHitTestVisible = false;

					httpTask = Network.GetWeekdayList(strTag);
					listGetData = await httpTask;

					if (strNowLoading != strLoading) { return; }
					imgLoadIndicator.Visibility = Visibility.Collapsed;

					//MessageBox.Show(listGetData.Count.ToString() + "\n" + listGetData[0].Caption + "\n" + listGetData[0].Tag);
					foreach (SctListItem sItem in listGetData) {
						Button buttonItem = new Button() { Width = 310, Height = 40, Background = Brushes.Transparent, Style = FindResource("FlatButton") as Style, };
						CustomControl.GetPopupListItem(buttonItem, sItem.Caption, string.Format("maker-=-{0}-=-{1}", sItem.Tag, sItem.Caption), "");
						buttonItem.Click += ButtonPopup_Click;

						stackListWeekday.Children.Add(buttonItem);
					}

					break;

				case "maker":
					gridTorrentArea.Visibility = Visibility.Collapsed;

					gridListMaker.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(310, 0, 0, 0), TimeSpan.FromMilliseconds(0)));

					gridSmiArea.Visibility = Visibility.Visible;
					gridListWeekday.Visibility = Visibility.Visible;
					gridListMaker.Visibility = Visibility.Visible;
					gridListFiles.Visibility = Visibility.Collapsed;

					buttonReturnList.Visibility = Visibility.Visible;
					imgLoadIndicator.Visibility = Visibility.Visible;

					gridListWeekday.IsHitTestVisible = false;
					gridListMaker.IsHitTestVisible = true;
					gridListFiles.IsHitTestVisible = false;

					httpTask = Network.GetMakerList(strTag);
					listGetData = await httpTask;

					if (strNowLoading != strLoading) { return; }
					imgLoadIndicator.Visibility = Visibility.Collapsed;

					foreach (SctListItem sItem in listGetData) {
						Button buttonItem = new Button() { Width = 310, Height = 40, Background = Brushes.Transparent, Style = FindResource("FlatButton") as Style, };
						CustomControl.GetPopupListItem(buttonItem, sItem.Caption, string.Format("filelist-=-{0}-=-{1}", sItem.Tag, sItem.Caption), "");
						buttonItem.Click += ButtonPopup_Click;

						stackListMaker.Children.Add(buttonItem);
					}


					gridListMaker.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(310, 0, 0, 0), new Thickness(0), TimeSpan.FromMilliseconds(350)) {
						BeginTime = TimeSpan.FromMilliseconds(100),
						EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut }
					});

					break;
				case "filelist":
					gridTorrentArea.Visibility = Visibility.Collapsed;

					gridListFiles.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(310, 0, 0, 0), TimeSpan.FromMilliseconds(0)));

					gridSmiArea.Visibility = Visibility.Visible;
					gridListWeekday.Visibility = Visibility.Visible;
					gridListMaker.Visibility = Visibility.Visible;
					gridListFiles.Visibility = Visibility.Visible;

					buttonReturnList.Visibility = Visibility.Visible;
					imgLoadIndicator.Visibility = Visibility.Visible;

					gridListWeekday.IsHitTestVisible = false;
					gridListMaker.IsHitTestVisible = false;
					gridListFiles.IsHitTestVisible = true;

					httpTask = Network.GetFileList(strTag);
					listGetData = await httpTask;

					if (strNowLoading != strLoading) { return; }

					imgLoadIndicator.Visibility = Visibility.Collapsed;

					foreach (SctListItem sItem in listGetData) {
						Button buttonItem = new Button() { Width = 310, Height = 40, Background = Brushes.Transparent, Style = FindResource("FlatButton") as Style, };
						CustomControl.GetPopupListItem(buttonItem, sItem.Caption, string.Format("download-=-{0}-=-{1}", sItem.Tag, sItem.Caption), "");
						buttonItem.Click += ButtonPopup_Click;

						stackListFiles.Children.Add(buttonItem);
					}
					Button buttonBlog = new Button() { Width = 310, Height = 40, Background = Brushes.Transparent, Style = FindResource("FlatButton") as Style, };
					CustomControl.GetPopupListItem(buttonBlog, "블로그로 이동", "web-=-" + strTag, "");
					buttonBlog.Click += ButtonPopup_Click;

					stackListFiles.Children.Add(buttonBlog);

					gridListFiles.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(310, 0, 0, 0), new Thickness(0), TimeSpan.FromMilliseconds(350)) {
						BeginTime = TimeSpan.FromMilliseconds(100),
						EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut }
					});

					break;
			}
		}

		private void buttonReturnList_Click(object sender, RoutedEventArgs e) {
			if (strNowPopupView == "maker") {
				buttonReturnList.Visibility = Visibility.Collapsed;
				strNowPopupView = "weekday";
				gridListWeekday.IsHitTestVisible = true;
				gridListMaker.IsHitTestVisible = false;
				textListMessage.Text = dictPopupTitle[strNowPopupView];

				gridListMaker.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(310, 0, 0, 0), TimeSpan.FromMilliseconds(350)) {
					BeginTime = TimeSpan.FromMilliseconds(100),
					EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut }
				});
			} else {
				strNowPopupView = "maker";
				gridListMaker.IsHitTestVisible = true;
				gridListFiles.IsHitTestVisible = false;
				textListMessage.Text = dictPopupTitle[strNowPopupView];

				gridListFiles.BeginAnimation(Grid.MarginProperty, new ThicknessAnimation(new Thickness(310, 0, 0, 0), TimeSpan.FromMilliseconds(350)) {
					BeginTime = TimeSpan.FromMilliseconds(100),
					EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut }
				});
			}
		}

		private void RefreshComboboxSource() {
			List<ComboBoxPairs> listContext = new List<ComboBoxPairs>();
			listContext.Add(new ComboBoxPairs("* Add new item in archive", 0));

			var listArchive = DataStruct.dictArchive.OrderBy(kvp => kvp.Value.Title);
			foreach (KeyValuePair<int, sctArchive> kData in listArchive) {
				if (!kData.Value.isLinked) {
					listContext.Add(new ComboBoxPairs(kData.Value.Title, kData.Value.ID));
				}
			}
			comboboxLink.DisplayMemberPath = "_Key";
			comboboxLink.SelectedValuePath = "_Value";
			comboboxLink.ItemsSource = listContext;
		}

		#region Custom Message Style

		string strMessageType = "", strAnswer = ""; int nNowEdit = -1;
		private Task<bool> GetAnswer() {
			return Task.Run(() => {
				for (; ; ) {
					if (strAnswer != "") { break; }
					System.Threading.Thread.Sleep(100);
				}
				if (strAnswer == "ok") {
					return true;
				}
				return false;
			});
		}

		private void ShowMessageBox(string strType, string strMessage) {
			strMessageType = strType;

			switch (strType) {
				case "alert":
					gridList.Visibility = Visibility.Collapsed;
					gridMessage.Visibility = Visibility.Visible;

					stackForm.Visibility = Visibility.Collapsed;
					gridAlert.Visibility = Visibility.Visible;

					textAlert.Text = strMessage;
					break;
				case "formmodify":
					gridList.Visibility = Visibility.Collapsed;
					gridMessage.Visibility = Visibility.Visible;

					stackForm.Visibility = Visibility.Visible;
					gridAlert.Visibility = Visibility.Collapsed;

					comboboxLink.Visibility = Visibility.Collapsed;
					break;
				case "formaddseason":
					textFormType.Text = "Add season data";
					textboxTitle.Text = "";
					textboxTitle.Tag = "Enter the title";

					stackSeasonForm.Visibility = Visibility.Visible;

					textShowTime.Text = "Time";
					textboxHour.Text = textboxMinute.Text = "";
					comboboxWeekday.Visibility = Visibility.Visible;
					textboxHour.Visibility = Visibility.Visible;
					textboxMinute.Visibility = Visibility.Visible;

					textLinked.Text = "Link to";
					comboboxLink.Visibility = Visibility.Visible;
					comboboxLink.SelectedIndex = 0;

					textboxSearchTag.Text = "";
					textboxSearchTag.Tag = "This is for seaching";


					gridList.Visibility = Visibility.Collapsed;
					gridMessage.Visibility = Visibility.Visible;

					stackForm.Visibility = Visibility.Visible;
					gridAlert.Visibility = Visibility.Collapsed;

					comboboxLink.Visibility = Visibility.Visible;
					break;
				case "formaddarchive":
					textFormType.Text = "Add to archive";
					textboxTitle.Text = "";
					textboxTitle.Tag = "Enter the title";

					stackSeasonForm.Visibility = Visibility.Collapsed;
					
					gridList.Visibility = Visibility.Collapsed;
					gridMessage.Visibility = Visibility.Visible;

					stackForm.Visibility = Visibility.Visible;
					gridAlert.Visibility = Visibility.Collapsed;

					comboboxLink.Visibility = Visibility.Collapsed;
					break;
			}

			AnimateBox(gridMessage, 1.5, 1, 0.7, 1);
			gridPopup.IsHitTestVisible = true;
			gridPopup.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)) {
				BeginTime = TimeSpan.FromMilliseconds(100)
			});
		}

		private void HideMessageBox() {
			strLoading = "";
			AnimateBox(gridMessage, 1, 0.9, 1, 0);
			AnimateBox(gridList, 1, 0.9, 1, 0);
			gridPopup.IsHitTestVisible = false;
			gridPopup.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)) {
				BeginTime = TimeSpan.FromMilliseconds(100)
			});
		}

		private void AnimateBox(UIElement uie, double dbFrom, double dbTarget, double dbFrom2, double dbTarget2) {
			Storyboard sb = new Storyboard();

			DoubleAnimation da1 = new DoubleAnimation(dbFrom, dbTarget, TimeSpan.FromMilliseconds(200));
			DoubleAnimation da2 = new DoubleAnimation(dbFrom, dbTarget, TimeSpan.FromMilliseconds(200));
			DoubleAnimation da3 = new DoubleAnimation(dbFrom2, dbTarget2, TimeSpan.FromMilliseconds(200));
			DoubleAnimation da4 = new DoubleAnimation(dbFrom2, dbTarget2, TimeSpan.FromMilliseconds(200));

			da1.EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseInOut };
			da2.EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseInOut };

			Storyboard.SetTarget(da1, uie); Storyboard.SetTarget(da2, uie);
			Storyboard.SetTarget(da3, uie); Storyboard.SetTarget(da4, uie);
			Storyboard.SetTargetProperty(da1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
			Storyboard.SetTargetProperty(da2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
			Storyboard.SetTargetProperty(da3, new PropertyPath(Grid.OpacityProperty));
			Storyboard.SetTargetProperty(da4, new PropertyPath(Grid.OpacityProperty));

			sb.Children.Add(da1); sb.Children.Add(da2);
			sb.Children.Add(da3); sb.Children.Add(da4);
			sb.Begin(this);
		}

		private void buttonPopupOK_Click(object sender, RoutedEventArgs e) {
			strAnswer = "ok";
			if (strMessageType == "alert") { HideMessageBox(); }

			if (textboxTitle.Text == "") {
				ShowTopMessage("Title can't be empty");
				textboxTitle.Focus(); return;
			}

			if (strMessageType == "formmodify") {
				if (nNowEdit % 10 == 0) {
					sctArchive sData = DataStruct.dictArchive[nNowEdit / 10];

					int nIndex = OrderProcess.listArchiveStatus.IndexOf(DataStruct.dictArchive[nNowEdit / 10].Title);
					OrderProcess.listArchiveStatus.Remove(DataStruct.dictArchive[nNowEdit / 10].Title);

					sData.Title = textboxTitle.Text;
					DataStruct.dictArchive[nNowEdit / 10] = sData;

					DataStruct.dictUI[nNowEdit].txtTitle.Text = sData.Title;

					stackArchive.Children.RemoveAt(nIndex);

					nIndex = OrderProcess.GetArchiveItemIndex(sData.Title);
					stackArchive.Children.Insert(nIndex, DataStruct.dictUI[nNowEdit].gBase);
				} else {
					sctSeason sData = DataStruct.dictSeason[nNowEdit / 10];
					sData.Title = textboxTitle.Text;
					sData.Keyword = textboxSearchTag.Text;
					if (sData.Keyword == "") { sData.Keyword = " "; }
					DataStruct.dictSeason[nNowEdit / 10] = sData;

					DataStruct.dictUI[nNowEdit].txtTitle.Text = sData.Title;
				}
				
				DataStruct.dictUI[nNowEdit].bReturn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
			}

			if (strMessageType == "formaddarchive") {
				if (DataStruct.dictNameTag.ContainsKey(textboxTitle.Text)) {
					ShowTopMessage("This name is already exists in archive");
					return;
				}

				DataStruct.nIDCount++;
				sctArchive sData = new sctArchive() {
					Title = textboxTitle.Text, Episode = 0, Season = 1,
					isLinked = false, ID = DataStruct.nIDCount, Memo = ""
				};

				DataStruct.dictArchive.Add(DataStruct.nIDCount, sData);
				DataStruct.dictNameTag.Add(sData.Title, DataStruct.nIDCount);

				Grid grid = GetSeasonListItem(sData.Title, sData.ID, false);
				int nIndex = OrderProcess.GetArchiveItemIndex(sData.Title);
				stackArchive.Children.Insert(nIndex, grid);
			}

			if (strMessageType == "formaddseason") {
				int nTemp = -1;
				try {
					if (textboxHour.Text.Length > 2 || textboxHour.Text.Length == 0) {
						throw new Exception();
					}
					nTemp = Convert.ToInt32(textboxHour.Text);
				} catch {
					ShowTopMessage("Please enter only 1~2 digit numbers");
					textboxHour.Focus(); return;
				}
				try {
					if (textboxMinute.Text.Length > 2 || textboxMinute.Text.Length == 0) {
						throw new Exception();
					}
					nTemp = Convert.ToInt32(textboxMinute.Text);
				} catch {
					ShowTopMessage("Please enter only 1~2 digit numbers");
					textboxMinute.Focus(); return;
				}

				int nLinkIndex = -1;
				if (comboboxLink.SelectedIndex == 0) {
					if (DataStruct.dictNameTag.ContainsKey(textboxTitle.Text)) {
						ShowTopMessage("This name is already exists in archive");  return;
					}

					DataStruct.nIDCount++;
					nLinkIndex = DataStruct.nIDCount;

					sctArchive sData = new sctArchive() {
						Title = textboxTitle.Text, Episode = 0, Season = 1,
						isLinked = true, ID = DataStruct.nIDCount, Memo = ""
					};

					DataStruct.dictArchive.Add(DataStruct.nIDCount, sData);
					DataStruct.dictNameTag.Add(sData.Title, DataStruct.nIDCount);

					Grid grid = GetSeasonListItem(sData.Title, sData.ID, false);
					int nIndex = OrderProcess.GetArchiveItemIndex(sData.Title);
					stackArchive.Children.Insert(nIndex, grid);
				} else {
					nLinkIndex = (int)comboboxLink.SelectedValue;
				}

				string strTime = "";
				if (textboxHour.Text.Length == 1) {
					strTime += "0" + textboxHour.Text + ":";
				} else { strTime += textboxHour.Text + ":"; }

				if (textboxMinute.Text.Length == 1) {
					strTime += "0" + textboxMinute.Text;
				} else { strTime += textboxMinute.Text; }
				if (textboxSearchTag.Text == "") { textboxSearchTag.Text = " "; }

				sctSeason addData = new sctSeason() {
					Title = textboxTitle.Text, Keyword = textboxSearchTag.Text,
					LinkID = nLinkIndex,
					Weekday = comboboxWeekday.SelectedIndex,
					Time = strTime,
					SortTag = comboboxWeekday.SelectedIndex + ":" + strTime,
				};

				if (addData.LinkID > 0) {
					sctArchive sClone = DataStruct.dictArchive[addData.LinkID];
					sClone.isLinked = true;
					DataStruct.dictArchive[addData.LinkID] = sClone;
					DataStruct.dictSeason.Add(addData.LinkID, addData);

					int nIndex = OrderProcess.GetSeasonItemIndex(addData.SortTag);
					Grid grid = GetSeasonListItem(addData.Title, addData.LinkID, true);
					stackSeason.Children.Insert(nIndex, grid);
				}
			}

			HideMessageBox();

			RefreshWeekdayData();
			RefreshComboboxSource();
			FileClass.SaveData();
		}

		private void ShowTopMessage(string strMessage) {
			textTopMessage.Text = strMessage;

			Storyboard sb = new Storyboard();
			DoubleAnimation da1 = new DoubleAnimation(1, 1, TimeSpan.FromMilliseconds(2500));
			Storyboard.SetTarget(da1, textTopMessage);
			Storyboard.SetTargetProperty(da1, new PropertyPath(TextBlock.OpacityProperty));

			DoubleAnimation da2 = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
			Storyboard.SetTarget(da2, textTopMessage);
			Storyboard.SetTargetProperty(da2, new PropertyPath(TextBlock.OpacityProperty));
			da2.BeginTime = TimeSpan.FromMilliseconds(1500);

			sb.Children.Add(da1);
			sb.Children.Add(da2);
			sb.Begin(this);
		}

		private void buttonPopupCancel_Click(object sender, RoutedEventArgs e) {
			strAnswer = "cancel";
			HideMessageBox();
		}

		#endregion

		private void gridMessage_MouseDown(object sender, MouseButtonEventArgs e) { e.Handled = true; }

		bool isShutdown = false;
		public void AnimateWindow(double isShowing, double duration) {
			Storyboard sb = new Storyboard();

			DoubleAnimation da1 = new DoubleAnimation(1 - isShowing, isShowing, TimeSpan.FromMilliseconds(duration)) { BeginTime = TimeSpan.FromMilliseconds(isShowing * 1000) };
			DoubleAnimation da2 = new DoubleAnimation(1 - isShowing, isShowing, TimeSpan.FromMilliseconds(duration)) { BeginTime = TimeSpan.FromMilliseconds(isShowing * 1000) };

			da1.EasingFunction = new BackEase() { Amplitude = 0.3, EasingMode = EasingMode.EaseInOut };
			da2.EasingFunction = new BackEase() { Amplitude = 0.3, EasingMode = EasingMode.EaseInOut };

			Storyboard.SetTarget(da1, gridRoot); Storyboard.SetTarget(da2, gridRoot);
			Storyboard.SetTargetProperty(da1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
			Storyboard.SetTargetProperty(da2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

			sb.Children.Add(da1); sb.Children.Add(da2);
			sb.Completed += delegate(object sender, EventArgs e) {
				if (isShowing > 0) {
					this.Activate();
				} else {
					if (isShutdown) { Application.Current.Shutdown(); }
				}
			};

			sb.Begin(this);
		}
	}
}
