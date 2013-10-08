using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpList {
	public class OrderProcess {
		private static List<string> listSeasonStatus = new List<string>();
		public static List<string> listArchiveStatus = new List<string>();

		public static int GetSeasonItemIndex(string strSortTag) {
			for (int i = 0; i < listSeasonStatus.Count; i++) {
				if (string.Compare(listSeasonStatus[i], strSortTag) > 0) {
					listSeasonStatus.Insert(i, strSortTag);
					return i + Convert.ToInt32(strSortTag[0].ToString()) + 1;
				}
			}
			listSeasonStatus.Add(strSortTag);
			return listSeasonStatus.Count + Convert.ToInt32(strSortTag[0].ToString());
		}

		public static int GetArchiveItemIndex(string strIDName) {
			for (int i = 0; i < listArchiveStatus.Count; i++) {
				if (string.Compare(listArchiveStatus[i], strIDName) > 0) {
					listArchiveStatus.Insert(i, strIDName);
					return i;
				}
			}
			listArchiveStatus.Add(strIDName);
			return listArchiveStatus.Count - 1;
		}

		public static void DeleteArchive(int nID) {
			listArchiveStatus.Remove(DataStruct.dictArchive[nID].Title);
			DataStruct.dictNameTag.Remove(DataStruct.dictArchive[nID].Title);
			DataStruct.dictUI.Remove(nID * 10);
			DataStruct.dictArchive.Remove(nID);
		}

		public static void DeleteSeason(int nID) {
			for (int i = 0; i < listSeasonStatus.Count; i++) {
				if (listSeasonStatus[i] == DataStruct.dictSeason[nID].SortTag) {
					listSeasonStatus.RemoveAt(i);
					break;
				}
			}

			DataStruct.dictUI.Remove(nID * 10 + 1);
			DataStruct.dictSeason.Remove(nID);
		}

		public static string CheckWeekdayExists() {
			bool[] weekdate = new bool[10];
			for (int i = 0; i < listSeasonStatus.Count; i++) {
				weekdate[Convert.ToInt32(listSeasonStatus[i][0].ToString())] = true;
			}
			string strReturn = "";
			for (int i = 0; i < 7; i++) {
				if (weekdate[i]) {
					strReturn += "1";
				} else {
					strReturn += "0";
				}
			}
			return strReturn;
		}
	}
}
