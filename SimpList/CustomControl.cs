using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SimpList {
	public class CustomControl {
		public static SolidColorBrush mColor;
		public static void GetPopupListItem(Button buttonItem, string strName, string strTag, string strMemo) {
			buttonItem.Tag = strTag;

			Grid gridBase = new Grid() { Width = 310, Height = 40, Background = Brushes.Transparent };
			TextBlock txtName = null;

			if (strMemo != "") {
				string strSize; bool isRaw;
				isRaw = Convert.ToBoolean(strMemo.Split(new string[] { "__" }, StringSplitOptions.RemoveEmptyEntries)[0]);
				strSize = strMemo.Split(new string[] { "__" }, StringSplitOptions.RemoveEmptyEntries)[1];

				txtName = new TextBlock() {
					Text = strName, IsHitTestVisible = false,
					FontSize = 13.33, Width = 280,
					VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left,
					Margin = new Thickness(10, 5, 20, 0), TextTrimming = TextTrimming.CharacterEllipsis,
					Foreground = isRaw ? mColor : Brushes.Black
				};

				TextBlock txtSize = new TextBlock() {
					Text = strSize, Foreground = Brushes.Gray,
					FontSize = 9, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(0, 0, 20, 4),
				};

				gridBase.Children.Add(txtName);
				gridBase.Children.Add(txtSize);
			} else {
				txtName = new TextBlock() {
					Text = strName, IsHitTestVisible = false,
					FontSize = 13.33, Width = 280,
					VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left,
					Margin = new Thickness(20, 0, 10, 0), TextTrimming = TextTrimming.CharacterEllipsis,
				};
				gridBase.Children.Add(txtName);
			}
			Rectangle rectLine = new Rectangle() {
				Width = 280, Height = 1,
				VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Center,
				Fill = Brushes.LightGray,
			};

			gridBase.Children.Add(rectLine);
			buttonItem.Content = gridBase;
		}
	}
}
