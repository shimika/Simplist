using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpList {
	public class FileClass {
		public FileClass() { }

		static string ffSeason = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpListSeason.txt";
		static string ffList = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpList.txt";
		public static string ffFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpList\";
		static string ffBackup = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpListBackup\";

		public static void InitFiles() {
			if (!Directory.Exists(ffFolder)) { Directory.CreateDirectory(ffFolder); }
			if (!Directory.Exists(ffBackup)) { Directory.CreateDirectory(ffBackup); }
			if (!File.Exists(ffList)) { using (StreamWriter sw = new StreamWriter(ffList, true, Encoding.UTF8)) { sw.Write(""); } }
			if (!File.Exists(ffSeason)) { using (StreamWriter sw = new StreamWriter(ffSeason, true, Encoding.UTF8)) { sw.Write(""); } }

			string strBackupSeason = ffBackup + DateTime.Now.ToString("yyyy-MM-dd") + "_Season.txt";
			string strBackupArchive = ffBackup + DateTime.Now.ToString("yyyy-MM-dd") + "_Archive.txt";
			if (!File.Exists(strBackupSeason)) {
				using (StreamReader sr = new StreamReader(ffSeason)) {
					using (StreamWriter sw = new StreamWriter(strBackupSeason, true, Encoding.UTF8)) {
						sw.Write(sr.ReadToEnd());
					}
				}
				using (StreamReader sr = new StreamReader(ffList)) {
					using (StreamWriter sw = new StreamWriter(strBackupArchive, true, Encoding.UTF8)) {
						sw.Write(sr.ReadToEnd());
					}
				}
			}
		}

		public static void GetSeasonData() {
			string[] strSplitSeason = null;
			using (StreamReader sr = new StreamReader(ffSeason)) {
				strSplitSeason = sr.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			}

			for (int i = 0; i < strSplitSeason.Length; i++) {
				string[] strSplit = strSplitSeason[i].Split(new string[] { "	" }, StringSplitOptions.RemoveEmptyEntries);
				int nID = -1;
				if (DataStruct.dictNameTag.ContainsKey(strSplit[2])) { nID = DataStruct.dictNameTag[strSplit[2]]; }

				sctSeason sData = new sctSeason() {
					Title = strSplit[1], Keyword = strSplit[3].Substring(1),
					LinkID = nID,
					Weekday = Convert.ToInt32(strSplit[0][0].ToString()),
					Time = strSplit[0].Substring(2),
					SortTag = strSplit[0],
				};

				if (sData.LinkID > 0) {
					sctArchive sClone = DataStruct.dictArchive[sData.LinkID];
					sClone.isLinked = true;
					DataStruct.dictArchive[sData.LinkID] = sClone;
					DataStruct.dictSeason.Add(sData.LinkID, sData);
				}
			}
		}

		public static void GetListData() {
			string[] strSplitList = null;
			using (StreamReader sr = new StreamReader(ffList)) {
				strSplitList = sr.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			}
			foreach (string str in strSplitList) {
				sctArchive sList = new sctArchive();
				DataStruct.nIDCount++;

				string[] str2 = str.Split(new string[] { " : " }, StringSplitOptions.RemoveEmptyEntries);
				if (str2.Length == 2) {
					sList.Memo = str2[1].Trim();
				} else { sList.Memo = ""; }

				str2 = str2[0].Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
				if (str2.Length == 2) {
					string[] str3 = str2[1].Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
					if (str3.Length == 1) {
						sList.Season = -1;
						sList.Episode = Convert.ToInt32(str3[0].Trim());
					} else {
						sList.Season = Convert.ToInt32(str3[0].Trim());
						sList.Episode = Convert.ToInt32(str3[1].Trim());
					}
				} else {
					sList.Season = sList.Episode = -1;
				}
				sList.Title = str2[0].Trim();
				sList.ID = DataStruct.nIDCount;

				DataStruct.dictArchive.Add(DataStruct.nIDCount, sList);
				DataStruct.dictNameTag.Add(sList.Title, DataStruct.nIDCount);
			}
		}

		public static void SaveData() {
			var listSeason = DataStruct.dictSeason.OrderBy(kvp => kvp.Value.SortTag);

			using (StreamWriter sw = new StreamWriter(ffSeason, false, Encoding.UTF8)) {
				foreach (KeyValuePair<int,sctSeason> kData in listSeason) {
					sctSeason aData = kData.Value;
					sw.WriteLine(aData.SortTag + "\t\t" + aData.Title + "\t\t" + DataStruct.dictArchive[aData.LinkID].Title + "\t\t#" + aData.Keyword);
				}
			}

			// Write list data
			var listArchive = DataStruct.dictArchive.OrderBy(kvp => kvp.Value.Title);

			using (StreamWriter sw = new StreamWriter(ffList, false, Encoding.UTF8)) {
				string strAppend;
				foreach (KeyValuePair<int, sctArchive> kData in listArchive) {
					sctArchive aData = kData.Value;
					strAppend = "";

					if (aData.Episode < 0 && aData.Memo == "") { strAppend = "\t"; }
					strAppend += aData.Title;
					if (aData.Episode >= 0) { strAppend += " - "; }
					if (aData.Season >= 0) { strAppend += aData.Season + "."; }
					if (aData.Episode >= 0) { strAppend += aData.Episode.ToString("00"); }
					if (aData.Memo != "") { strAppend += " : " + aData.Memo; }

					sw.WriteLine(strAppend);
				}
			}
		}
	}
}
