using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 热更资源清单
/// </summary>
public class ResManifest
{
	public static Dictionary<string, ResUnit> ResManifestDict { get; set; }
	public struct ResUnit
	{
		public string bundleName;
		public string md5;
		public string size;

		public ResUnit(string bundleName, string md5, string size)
		{
			this.bundleName = bundleName;
			this.md5 = md5;
			this.size = size;
		}
	}

	private ResManifest() { }

	public static void ParseResManifest(string filePath, Dictionary<string, ResUnit> dict)
	{
		if (!System.IO.File.Exists(filePath)) return;

		dict.Clear();

		string[] lines = System.IO.File.ReadAllLines(filePath);
		for (int i = 0, iMax = lines.Length; i < iMax; i++)
		{
			string[] item = lines[i].Split(',');
			dict[item[0]] = new ResUnit(item[0], item[1], item[2]);
		}
	}

	public static string GetBundleMD5Name(string bundleName)
	{
		if (ResManifestDict == null) Debug.LogError("Please init first");
		if (!ResManifestDict.ContainsKey(bundleName))
		{
			Debug.LogError($"找不到bundle信息：{bundleName}");
			return null;
		}
		string md5 = ResManifestDict[bundleName].md5;
		string folder = "";
		if (bundleName.Contains('/'))
		{
			folder = bundleName[..bundleName.LastIndexOf('/')];
		}

		return $"{folder}/{md5}.u";
	}
}
