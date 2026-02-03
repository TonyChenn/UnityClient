using HybridCLR;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class CSharpLoader
{
	public static void LoadDLL()
	{
		Debug.Log("----------------------------------------------------------");
		Debug.Log("-> 开始加载C# DLL");
		Assembly assembly = null;
#if UNITY_EDITOR
		Debug.Log("--> 编辑器模式，无需加载DLL");
		assembly = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
#else
		Debug.Log("--> 真机模式，需要加载DLL");
		LoadAOTDlls();
		LoadHotfixDlls(ref assembly);
#endif
		Debug.Log("-> C# DLL 加载完毕");
		Debug.Log("----------------------------------------------------------");
		System.Type gameMgr = assembly.GetType("GameMgr");
		GameObject.Find("[Main]").AddComponent(gameMgr);
	}

	/// <summary>
	/// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
	/// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
	/// 
	/// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
	/// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
	/// </summary>
	private static void LoadAOTDlls()
	{
		Debug.Log("---> 开始补充元数据");
		var aotDlls = Resources.LoadAll<TextAsset>("AotDll");
		foreach (var dll in aotDlls)
		{
			LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dll.bytes, HomologousImageMode.SuperSet);
			Debug.Log($"---->补充元数据：{dll.name}, ret: {err == LoadImageErrorCode.OK}");
		}
		Debug.Log("---> 补充元数据完毕");
	}

	private static void LoadHotfixDlls(ref Assembly assembly)
	{
		Debug.Log("---> 开始加载热更程序集");
		string hotfixDll = Application.persistentDataPath + "/code/code.u";
		if (!File.Exists(hotfixDll) /**|| GameConfig.PlayMode == PlayMode.OfflineMode*/)
		{
			hotfixDll = Application.streamingAssetsPath + "/code/code.u";
		}

		AssetBundle bundle = AssetBundle.LoadFromFile(hotfixDll);
		TextAsset dll = bundle.LoadAsset<TextAsset>("Assembly-CSharp.bytes");
		assembly = Assembly.Load(dll.bytes);
		Debug.Log($"----> 加载热更程序集记 Assembly-CSharp.bytes 完毕: {dll != null}");
		bundle.Unload(true);
		Debug.Log("---> 热更程序集加载完毕");
	}
}
