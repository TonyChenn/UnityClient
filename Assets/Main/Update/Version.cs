using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class Version
{
    private const string VERSIONFILENAME = "version.data";
    private const string REMOTEVERSIONNAME = "remote_version.data";
    private const string REMOTEMANIFESTNAME = "remote_manifest.csv";
    private const string MANIFESTFILENAME = "manifest.csv";

    private const string ServerUrl = "https://mp-djlw.youzu.com";
    private const string VersionURL = "https://patch-djlw.youzu.com/version";
    private const string gameid = "171";
    private const string opid = "2106";
    private const string opgameId = "2297";


    private static VersionInfo localVersionInfo;
    private static VersionInfo remoteVersionInfo;

    // 从 /ver 协议获取的信息
    private static string remoteVersion;
    private static string remoteAuditVersion = "0";

    // 资源清单
    private static Dictionary<string, ResUnit> localManifest;
    private static Dictionary<string, ResUnit> remoteManifest;


    public static async Task StartCheckUpdate()
    {
        // 加载本地版本文件
        await LoadLocalVersionInfoAsync();
        // 获取远程版本号
        bool success = await RequestRemoteVersionAsync();
        if (!success)
        {
            Debug.Log("[update] 获取远程版本号 失败");
            return;
        }
#if UNITY_IOS || UNITY_ANDROID
        if(remoteAuditVersion == Application.version)
        {
            Debug.Log($"[update] 当前是{Application.platform}提审版本，无需热更：{remoteAuditVersion}");
            enterGame();
            return;
        }
#endif

        // 获取远程版本文件 & 加载
        success = await RequestRemoteVersionFileAsync();
        if (!success) { return; }

        if (localVersionInfo.ID < remoteVersionInfo.ID)
        {
            Debug.Log("[update] 客户端需要更新");
            return;
        }
        if (localVersionInfo.Tag == remoteVersionInfo.Tag)
        {
            Debug.Log($"[update] 无热更新：{localVersionInfo.ID}.{localVersionInfo.Tag}");
            enterGame();
            return;
        }
        Debug.Log($"[update] 有更新：local:{localVersionInfo.Tag} remote:{remoteVersionInfo.Tag}");

        // 解析本地资源清单
        await LoadLocalResManifestAsync();
        // 请求远程资源清单
        success = await RequestRemoteResManifestAsync();
        if (!success) { return; }

        // 对比清单文件
        await DiffManifestAsync();
    }

    private static void enterGame()
    {
        Debug.Log("[update] 进入游戏");
        // TODO: 实现进入游戏的逻辑
    }

    /// <summary>
    /// 加载解析本地版本文件
    /// </summary>
    /// <returns></returns>
    private static async Task LoadLocalVersionInfoAsync()
    {
        Debug.Log("[update] 开始解析本地版本号");
        string dst = Path.Combine(Application.persistentDataPath, VERSIONFILENAME);
        if (!File.Exists(dst)) 
        { 
            try
            {
                await CopyToPersistentAsync(VERSIONFILENAME); 
            }
            catch (IOException ex)
            {
                Debug.LogError($"[update] 复制版本文件失败: {ex.Message}");
                return;
            }
        }

        localVersionInfo = VersionFile.ReadVersionFile(dst);
    }

    /// <summary>
    /// 请求远程版本号
    /// </summary>
    /// <returns></returns>
    private static async Task<bool> RequestRemoteVersionAsync()
    {
        Debug.Log("[update] 获取远程版本号");
        WWWForm form = new WWWForm();
        form.AddField("op_id", opid);
        form.AddField("opgame_id", opgameId);

        UnityWebRequest request = UnityWebRequest.Post($"{ServerUrl}/ver", form);
        Debug.Log($"[C->S] {request.uri} [opid={opid},opgame_id={opgameId}]");
        await request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + request.error);
            return false;
        }

        string response = request.downloadHandler.text;
        Debug.Log($"[S->C] {response}");
        VerResponse jobject = JsonUtility.FromJson<VerResponse>(response);
        if (jobject.status != 0)
        {
            return false;
        }

        remoteVersion = jobject.ver;
#if UNITY_ANDROID
        remoteAuditVersion = jobject.ver_android_audit;
#elif UNITY_IOS
        remoteAuditVersion = jobject.ver_ios_audit;
#endif
        return true;
    }
    
    // 从 /ver 接口获取的版本信息
    [Serializable]
    private class VerResponse
    {
        public int status;
        public string ver;
        public string ver_android_audit;
        public string ver_ios_audit;
    }

    /// <summary>
    /// 请求远程版本文件
    /// </summary>
    /// <returns></returns>
    private static async Task<bool> RequestRemoteVersionFileAsync()
    {
        Debug.Log("[update] 获取远程版本文件");
        string dst = Path.Combine(Application.persistentDataPath, REMOTEVERSIONNAME);

        string url = $"{VersionURL}/{gameid}_{opgameId}_{opid}_{remoteVersion}.data?{DateTime.Now}";
        Debug.Log($"[C->S] {url}");
        using var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        Debug.Log($"[S->C] {url}");
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log($"[update] 获取远程版本文件 失败: {request.error}");
            return false;
        }

        if (File.Exists(dst)) File.Delete(dst);
        Debug.Log("[update] 加载远程版本文件");
        await File.WriteAllBytesAsync(dst, request.downloadHandler.data);

        remoteVersionInfo = VersionFile.ReadVersionFile(dst);
        return true;
    }

    /// <summary>
    /// 加载本地资源清单
    /// </summary>
    /// <returns></returns>
    private static async Task LoadLocalResManifestAsync()
    {
        Debug.Log("[update] 开始加载本地资源清单");
        string dst = Path.Combine(Application.persistentDataPath, MANIFESTFILENAME);
        if (!File.Exists(dst)) { await CopyToPersistentAsync(MANIFESTFILENAME); }

        localManifest = await ResManifest.ParseResManifestAsync(dst);
    }

    /// <summary>
    /// 请求远程资源清单 & 解析
    /// </summary>
    /// <returns></returns>
    private static async Task<bool> RequestRemoteResManifestAsync()
    {
        Debug.Log("[update] 获取远程资源清单");
        string dst = Path.Combine(Application.persistentDataPath, REMOTEMANIFESTNAME);

        string url = $"{remoteVersionInfo.UpdateUrl1}/version/white/res/{MANIFESTFILENAME}.{remoteVersionInfo.MD5}";
        Debug.Log($"[C->S] {url}");
        using var request = UnityWebRequest.Get(url);
        await request.SendWebRequest();
        Debug.Log($"[S->C] {url}");
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[update] 获取远程资源清单失败: " + request.error);
            return false;
        }

        if (File.Exists(dst)) File.Delete(dst);
        Debug.Log("[update] 加载远程资源清单");
        await File.WriteAllBytesAsync(dst, request.downloadHandler.data);

        remoteManifest = await ResManifest.ParseResManifestAsync(dst);
        return true;
    }

    private static async Task DiffManifestAsync()
    {
        
    }


    /// <summary>
    /// 把文件从Streamming复制到Persistent
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    private static async Task<string> CopyToPersistentAsync(string filePath, bool overwrite = true)
    {
        string src = Path.Combine(Application.streamingAssetsPath, filePath);
        string dst = Path.Combine(Application.persistentDataPath, filePath);
        string dir = Path.GetDirectoryName(dst);
        if (!Directory.Exists(dir) && !string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        if (!overwrite && File.Exists(dst))
            return dst;

        using UnityWebRequest req = UnityWebRequest.Get(src);
        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            throw new IOException($"Read StreamingAssets failed: {src}\n{req.error}");
        }
        await File.WriteAllBytesAsync(dst, req.downloadHandler.data);
        return dst;
    }
}
