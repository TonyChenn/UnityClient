using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 打包AssetBundle后生成的依赖清单
/// </summary>
public static class DependManifest
{
	// Bundle的依赖词典
	private static Dictionary<string, string[]> dependManifestDict;


	static DependManifest()
	{
		dependManifestDict = new Dictionary<string, string[]>(32);
		LoadManifest();
	}

	#region API
	public static void Restart()
	{
		AssetBundle.UnloadAllAssetBundles(true);
		dependManifestDict.Clear();
		LoadManifest();
	}

	/// <summary>
	/// 获取AssetBundle的所有依赖文件
	/// </summary>
	/// <param name="bundleName">无需加 ".u"</param>
	/// <returns></returns>
	public static string[] GetAllDependencies(string bundleName)
	{
		if (bundleName.EndsWith(".u"))
			bundleName = bundleName.Substring(0, bundleName.LastIndexOf("."));

		if (dependManifestDict.ContainsKey(bundleName))
			return dependManifestDict[bundleName];

		return null;
	}
	#endregion


	/// <summary>
	/// AB包的依赖清单
	/// </summary>
	private static void LoadManifest()
	{
		string path = $"{Application.streamingAssetsPath}/bundle_manifest.u";
		if (Boot.PlayMode == PlayMode.HostMode)
		{
			string persistPath = $"{Application.persistentDataPath}/bundle_manifest.u";
			if (System.IO.File.Exists(persistPath))
				path = persistPath;
		}

		dependManifestDict.Clear();

		var bundle = AssetBundle.LoadFromFile(path);
		var manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
		string[] bundles = manifest.GetAllAssetBundles();
		for (int i = 0, iMax = bundles.Length; i < iMax; i++)
		{
			string bundleName = bundles[i].Replace(".u", "");
			string[] deps = manifest.GetAllDependencies(bundles[i]);
			for (int j = 0, jMax = deps.Length; j < jMax; j++)
				deps[j] = deps[j].Replace(".u", "");

			dependManifestDict[bundleName] = deps;
		}
		bundle.Unload(true);
		Debug.Log($"ResMgr load bundle_manifest");
	}
}
