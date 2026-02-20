using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class Version
{
    private const string VERSIONFILENAME = "version.data";
    private const string REMOVEVERSIONNAME = "remote_version.data";
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
        }
        Debug.Log($"[update] 有更新：local:{localVersionInfo.Tag} remote:{remoteVersionInfo.Tag}");

        // 解析本地资源清单
        await LoadLocalResManifestAsync();
        // 请求远程资源清单
        success = await RequestRemoteResManifestAsync();
        if (!success) { return; }

        // 对比清单文件

    }

    private static void enterGame()
    {

    }

    /// <summary>
    /// 加载解析本地版本文件
    /// </summary>
    /// <returns></returns>
    private static async Task LoadLocalVersionInfoAsync()
    {
        Debug.Log("[update] 开始解析本地版本号");
        string dst = Path.Combine(Application.persistentDataPath, VERSIONFILENAME);
        if (!File.Exists(dst)) { await CopyToPersistentAsync(VERSIONFILENAME); }

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


        //await File.WriteAllBytesAsync(dst, request.downloadHandler.data);
        string response = request.downloadHandler.text;
        Debug.Log($"[S->C] {response}");
        dynamic jobject = JsonUtility.FromJson<dynamic>(response);
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

    /// <summary>
    /// 请求远程版本文件
    /// </summary>
    /// <returns></returns>
    private static async Task<bool> RequestRemoteVersionFileAsync()
    {
        Debug.Log("[update] 获取远程版本文件");
        string dst = Path.Combine(Application.persistentDataPath, REMOVEVERSIONNAME);

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

    private static async Task<bool> RequestRemoteResManifestAsync()
    {
        Debug.Log("[update] 获取远程资源清单");
        string dst = Path.Combine(Application.persistentDataPath, REMOVEVERSIONNAME);

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

public struct VersionInfo
{
    public ushort ID;
    public ushort Tag;
    public string UpdateUrl1;
    public string UpdateUrl2;
    public string ClientUrl;
    public ushort Exclude;
    public string MD5;
    public uint Time;

    public ushort BaseID;
    public uint NewTag;
}

public static class VersionFile
{
    public static VersionInfo ReadVersionFile(string filePath)
    {
        VersionInfo result = new VersionInfo();
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            using BinaryReader reader = new BinaryReader(fs);
            char chr1 = reader.ReadChar();
            char chr2 = reader.ReadChar();
            char chr3 = reader.ReadChar();
            if (chr1 != 'V' || chr2 != 'H' || chr3 != 'F')
            {
                return result;
            }
            result.ID = ReadUShort(reader);
            result.BaseID = ReadUShort(reader);
            result.Time = ReadUInt(reader);
            result.Tag = ReadUShort(reader);

            for (ushort i = result.BaseID; i < result.ID; i++)
            {
                uint size = ReadUInt(reader);
            }

            if (reader.BaseStream.Position < reader.BaseStream.Length) { result.UpdateUrl1 = ReadString(reader); }
            if (reader.BaseStream.Position < reader.BaseStream.Length) { result.UpdateUrl2 = ReadString(reader); }
            if (reader.BaseStream.Position < reader.BaseStream.Length) { result.ClientUrl = ReadString(reader); }

            if (reader.BaseStream.Position < reader.BaseStream.Length) { result.Exclude = ReadUShort(reader); }
            if (reader.BaseStream.Position < reader.BaseStream.Length) { result.MD5 = ReadString(reader); }
            if (reader.BaseStream.Position < reader.BaseStream.Length) { result.NewTag = ReadUInt(reader); }
        }
        return result;
    }

#if UNITY_EDITOR
    public static void WriteVersionFile(string filePath, VersionInfo versionInfo)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // 文件头
                writer.Write('V');
                writer.Write('H');
                writer.Write('F');
                // id
                WriteUShort(writer, versionInfo.ID);
                // baseid
                WriteUShort(writer, versionInfo.BaseID);
                // time
                WriteUInt(writer, versionInfo.Time);
                // tag
                WriteUShort(writer, versionInfo.Tag);

                for (ushort i = versionInfo.BaseID; i < versionInfo.ID; i++) { WriteUInt(writer, 0); }

                WriteString(writer, versionInfo.UpdateUrl1);
                WriteString(writer, versionInfo.UpdateUrl2);
                WriteString(writer, versionInfo.ClientUrl);

                // exclude
                WriteUShort(writer, 0);
                // md5
                WriteString(writer, versionInfo.MD5);
                // newtag
                WriteUInt(writer, versionInfo.NewTag);
            }
        }
    }
#endif

    #region util
    private static ushort ReadUShort(BinaryReader reader)
    {
        ushort result = reader.ReadByte();
        result = (ushort)(result << 8);
        result += reader.ReadByte();

        return result;
    }

    private static uint ReadUInt(BinaryReader reader)
    {
        uint result = reader.ReadByte();
        result = (result << 24) + (uint)(reader.ReadByte() << 16);
        result += (uint)(reader.ReadByte() << 8);
        result += reader.ReadByte();

        return result;
    }

    private static string ReadString(BinaryReader reader)
    {
        ushort len = ReadUShort(reader);
        char[] chrs = reader.ReadChars(len);

        return string.Concat<char>(chrs);
    }

    private static void WriteUShort(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value & 0xFF));
    }

    private static void WriteUInt(BinaryWriter writer, uint value)
    {
        writer.Write((byte)((value >> 24) & 0xFF));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        WriteUShort(writer, (ushort)value.Length);
        writer.Write(value.ToCharArray());
    }
    #endregion
}

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
public static class ResManifest
{
    public static async Task<Dictionary<string, ResUnit>> ParseResManifestAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return null;

        Dictionary<string, ResUnit> dict = new Dictionary<string, ResUnit>(10240);
        string[] lines = await System.IO.File.ReadAllLinesAsync(filePath);
        for (int i = 0, iMax = lines.Length; i < iMax; i++)
        {
            string[] item = lines[i].Split(',');
            string bundleName = item[0];
            dict[bundleName] = new ResUnit(bundleName, item[1], item[2]);
        }
        return dict;
    }
}
