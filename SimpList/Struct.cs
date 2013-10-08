using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SimpList {
	public struct sctSeason {
		public string Title, Keyword, Time, SortTag;
		public int Weekday, LinkID;
	}
	public struct sctArchive {
		public string Title, Memo;
		public int Season, Episode, ID;
		public bool isLinked;
	}
	public struct SctListItem {
		public string Caption, Tag, Memo, SortTag;
	}

	public struct UIComponent {
		public TextBlock txtTitle, txtEpisode;
		public Grid gEpisode, gBase, gOption;
		public Button bReturn;
	}

	public class DataStruct {
		public static SortedDictionary<int, sctArchive> dictArchive = new SortedDictionary<int,sctArchive>();
		public static SortedDictionary<int, sctSeason> dictSeason = new SortedDictionary<int, sctSeason>();
		public static Dictionary<string, int> dictNameTag = new Dictionary<string, int>();
		public static Dictionary<int, UIComponent> dictUI = new Dictionary<int, UIComponent>();
		public static int nIDCount = 0;
	}

	public class Functions {
		public static BitmapImage rtSource(string uriSource) {
			uriSource = "pack://application:,,,/SimpList;component/Resources/" + uriSource;
			BitmapImage source = new BitmapImage(new Uri(uriSource));
			return source;
		}

		public static string GetMD5Hash(string md5input) {
			md5input = md5input.ToLower();
			MD5CryptoServiceProvider md5x = new MD5CryptoServiceProvider();
			byte[] md5bs = Encoding.UTF8.GetBytes(md5input);
			md5bs = md5x.ComputeHash(md5bs);
			StringBuilder md5s = new StringBuilder();
			foreach (byte md5b in md5bs) { md5s.Append(md5b.ToString("x2").ToLower()); }
			return md5s.ToString();
		}
	}

	public class ComboBoxPairs {
		public string _Key { get; set; }
		public int _Value { get; set; }

		public ComboBoxPairs(string _key, int _value) {
			_Key = _key;
			_Value = _value;
		}
	}
}
