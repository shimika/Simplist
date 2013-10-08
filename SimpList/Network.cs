using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Xml;

namespace SimpList {
	public class Network {
		public static async Task<List<SctListItem>> GetTorrentList(string strTag) {
			Task<string> httpTask = GetHTML(@"http://www.nyaa.eu/?page=rss&cats=1_0&term=" + strTag, "UTF-8");
			string strHTML = await httpTask;

			List<SctListItem> listTorrent = new List<SctListItem>();
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(strHTML);

			XmlNodeList xmlnode = xmlDoc.SelectNodes("rss/channel/item");
			foreach (XmlNode node in xmlnode) {
				SctListItem tData = new SctListItem() {
					Caption = node["title"].InnerText, Tag = node["link"].InnerText,
					Memo = "false__",
				};
				if (node["category"].InnerText == "Raw Anime") { tData.Memo = "true__"; }
				try {
					string strFileSize = node["description"].InnerText.Split(new string[] { " - ", "]]" }, StringSplitOptions.RemoveEmptyEntries)[1];
					tData.Memo += strFileSize;
				} catch {  }
				listTorrent.Add(tData);
			}
			return listTorrent;
		}

		public static async Task<List<SctListItem>> GetWeekdayList(string strTag) {
			Task<string> httpTask = GetHTML(@"http://gs.saro.me/api/ab" + strTag, "UTF-8");
			string strHTML = await httpTask;

			List<SctListItem> listWeekday = new List<SctListItem>();
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(strHTML);

			XmlNodeList xmlnode = xmlDoc.SelectNodes("r/n");
			foreach (XmlNode node in xmlnode) {
				SctListItem tData = new SctListItem() {
					Caption = node.Attributes["s"].InnerText,
					Tag = "http://gs.saro.me/api/as?s=" + node.Attributes["s"].InnerText,
				};
				listWeekday.Add(tData);
			}

			return listWeekday;
		}

		public static async Task<List<SctListItem>> GetMakerList(string strTag) {
			Task<string> httpTask = GetHTML(strTag, "UTF-8");
			string strHTML = await httpTask;

			List<SctListItem> listMaker = new List<SctListItem>();

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(strHTML);

			XmlNodeList xmlnode = xmlDoc.SelectNodes("r");
			string[] strList = null;
			foreach (XmlNode node in xmlnode) {
				string content = node.Attributes["v"].InnerText;
				strList = content.Split(new string[] { "|" }, StringSplitOptions.None);
			}

			bool isFirst = true;
			foreach (string str in strList) {
				if (isFirst) { isFirst = false; continue; }

				string[] strSplit = str.Split(',');
				string strName = strSplit[1], strURL = strSplit[2];
				int episode = 0;
				try {
					episode = Convert.ToInt32(strSplit[0].Substring(0, 3));
				} catch { }
				strName = string.Format("({0}화) {1}", episode, strName);

				listMaker.Add(new SctListItem() {
					Caption = strName, Tag = strURL, SortTag = strSplit[0]
				});
			}

			//listMaker.Reverse();
			listMaker.Sort(new mysortByValue());
			return listMaker;
		}

		private class mysortByValue : IComparer<SctListItem> {
			public int Compare(SctListItem arg1, SctListItem arg2) {
				return string.Compare(arg1.SortTag, arg2.SortTag) * -1;
			}
		}

		public static async Task<List<SctListItem>> GetFileList(string strTag) {
			Task<string> httpTask = GetHTML(strTag, "UTF-8");
			string strHTML = await httpTask;

			if (strHTML == "") { return new List<SctListItem>(); }

			Dictionary<SiteType, int> dictCount = new Dictionary<SiteType, int>();
			dictCount[SiteType.Naver] = Regex.Matches(strHTML, "naver", RegexOptions.IgnoreCase).Cast<Match>().Count();
			dictCount[SiteType.Other] = Regex.Matches(strHTML, "tistory", RegexOptions.IgnoreCase).Cast<Match>().Count();
			dictCount[SiteType.Other] += Regex.Matches(strHTML, "egloos", RegexOptions.IgnoreCase).Cast<Match>().Count();

			SiteType sitetype = SiteType.Other;
			if (dictCount[SiteType.Naver] > dictCount[SiteType.Other]) { sitetype = SiteType.Naver; }

			if (sitetype == SiteType.Naver) {
				httpTask = GetHTML(strTag, "EUC-KR");
				strHTML = await httpTask;

				string nURL = "";
				if (strHTML.IndexOf("mainFrame") < 0 && strHTML.IndexOf("aPostFiles") < 0) {
					int nIndex = strHTML.IndexOf("screenFrame");
					for (int i = strHTML.IndexOf("http://blog.naver.com/", nIndex); ; i++) {
						if (strHTML[i] == '\"') { break; }
						nURL += strHTML[i];
					}
					if (nURL != "") {
						strTag = nURL;
						httpTask = GetHTML(strTag, "EUC-KR");
						strHTML = await httpTask;
					}
				}

				if (strHTML.IndexOf("mainFrame") >= 0 && strHTML.IndexOf("aPostFiles") < 0) {
					int nIndex = strHTML.IndexOf("mainFrame");
					nIndex = strHTML.IndexOf("src", nIndex);
					bool flag = false;
					nURL = "";
					for (int i = nIndex; ; i++) {
						if (strHTML[i] == '\"') {
							if (flag) {
								break;
							} else {
								flag = true;
								continue;
							}
						}

						if (flag) { nURL += strHTML[i]; }
					}
					if (nURL[0] == '/') { nURL = "http://blog.naver.com" + nURL; }
					if (nURL != "") { strTag = nURL; }
				}

				httpTask = GetHTML(strTag, "EUC-KR");
				strHTML = await httpTask;
			}

			List<SctListItem> listSmi = null;
			if (sitetype == SiteType.Naver) {
				listSmi = NaverParse(strHTML);
			} else {
				Task<List<SctListItem>> parseTask = TistoryParse(strHTML);
				listSmi = await parseTask;
			}

			return listSmi;
		}

		private static List<SctListItem> NaverParse(string html) {
			List<SctListItem> listData = new List<SctListItem>();
			int sIndex = 0, eIndex = 0;
			string attachString;

			string msg = "";
			for (; ; ) {
				sIndex = html.IndexOf("aPostFiles[", sIndex);
				if (sIndex < 0) { break; }
				sIndex = html.IndexOf("[{", sIndex);
				if (sIndex < 0) { break; }
				eIndex = html.IndexOf("}];", sIndex);
				if (eIndex < 0) { break; }

				attachString = html.Substring(sIndex + 2, eIndex - sIndex).Replace("\"", "\'");

				int flag = 0;
				string sKey = "", sValue = "";
				string fileName = "", fileURL = "";

				for (int i = 0; i < attachString.Length; i++) {
					if (attachString[i] == '\\' && i + 1 != attachString.Length) {
						if (attachString[i + 1] == '\'') {
							if (flag == 1) {
								sKey += '\'';
							} else if (flag == 3) {
								sValue += '\'';
							}
							i++; continue;
						}
					}
					if (attachString[i] == '\'') {
						flag++; continue;
					}

					switch (flag) {
						case 1: sKey += attachString[i]; break;
						case 3: sValue += attachString[i]; break;
						case 4:
							sKey = sKey.Trim(); sValue = sValue.Trim();
							if (sKey == "encodedAttachFileName") { fileName = sValue; }
							if (sKey == "encodedAttachFileUrl") { fileURL = sValue; }

							msg += sKey + " = " + sValue + "\n";
							sKey = sValue = "";
							flag = 0;

							break;
					}
					if (attachString[i] == '}') {
						if (fileName != "" && fileURL != "") {
							listData.Add(new SctListItem() { Caption = fileName, Tag = fileURL });
							fileName = fileURL = "";
						}
					}
				}

				msg += "\n";
			}
			return listData;
		}

		private static async Task<List<SctListItem>> TistoryParse(string html) {
			List<SctListItem> listData = new List<SctListItem>();
			int nIndex = 0, lastIndex = 0;
			string[] ext = new string[] { "zip", "rar", "7z", "egg", "smi" };
			List<int> lst = new List<int>();
			string fileName, fileURL;

			for (; ; ) {
				lst.Clear();
				foreach (string str in ext) {
					nIndex = html.IndexOf(string.Format(".{0}\"", str), lastIndex, StringComparison.OrdinalIgnoreCase);
					if (nIndex < 0) { nIndex = 999999999; }
					lst.Add(nIndex);
				}
				lst.Sort();
				if (lst[0] == 999999999) { break; }
				lastIndex = html.IndexOf("\"", lst[0]);
				fileURL = "";

				for (int i = lastIndex - 1; i >= 0; i--) {
					if (html[i] == '\"') { break; }
					fileURL = html[i] + fileURL;
				}

				Task<string> httpTask = GetFilenameFromURL(fileURL);
				fileName = await httpTask;

				if (fileName != "" && fileURL != "") {
					listData.Add(new SctListItem() { Caption = fileName, Tag = fileURL });
				}
			}
			return listData;
		}

		public static Task<string> GetHTML(string url, string encoding) {
			return Task.Run(() => {
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(new UriBuilder(url).Uri);
				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "http://www.google.com/";
				httpWebRequest.UserAgent =
					"Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; WOW64; " +
					"Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; " +
					".NET CLR 3.5.21022; .NET CLR 3.5.30729; .NET CLR 3.0.30618; " +
					"InfoPath.2; OfficeLiveConnector.1.3; OfficeLivePatch.0.0)";
				//httpWebRequest.Headers[HttpRequestHeader.AcceptCharset] = "utf-8";

				string rtHTML = "";
				try {
					httpWebRequest.Proxy = null;
					Stream requestStream = httpWebRequest.GetRequestStream();

					requestStream.Close();
					
					HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding(encoding));
					rtHTML = streamReader.ReadToEnd();
				} catch {
				}
				return rtHTML;
			});
		}

		private static Task<string> GetFilenameFromURL(string url) {
			return Task.Run(() => {
				using (WebClient client = new WebClient() { Proxy = null }) {
					using (Stream rawStream = client.OpenRead(url)) {
						string fileName = string.Empty;
						string contentDisposition = client.ResponseHeaders["content-disposition"];
						string realName = "";
						if (!string.IsNullOrEmpty(contentDisposition)) {
							string lookFor = "filename=";
							int index = contentDisposition.IndexOf(lookFor, StringComparison.CurrentCultureIgnoreCase);
							if (index >= 0) {
								fileName = contentDisposition.Substring(index + lookFor.Length);
								realName = HttpUtility.UrlDecode(fileName, Encoding.GetEncoding("UTF-8"));
							}
						} else {
							string[] strSplit = url.Split('/');
							realName = strSplit[strSplit.Length - 1];
						}
						rawStream.Close();
						if (realName[realName.Length - 1] == '\"') {
							realName = realName.Substring(0, realName.Length - 1);
							if (realName[0] == '\"') {
								realName = realName.Substring(1);
							}
						}
						return realName;
					}
				}
			});
		}
	}
	enum SiteType { Naver, Other };
}
