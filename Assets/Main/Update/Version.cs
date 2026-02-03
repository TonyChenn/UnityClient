using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class Version
{
    private const string VERSIONFILENAME = "version.data";
    private const string MANIFESTFILENAME = "manifest.csv";
    public struct VersionInfo
    {
        public int SmallVersion;
        public int BigVersion;
        public string AppleExamVersion;
        public string MD5;
        public string Cdn1;
        public string Cdn2;
        public long Time;
    }


    private static VersionInfo localVersionInfo;
    private static VersionInfo remoteVersionInfo;
    
    public static void StartCheckUpdate()
    {
        
    }

    public static async Task GetLocalVersionInfo()
    {
        string dst = Path.Combine(Application.persistentDataPath, VERSIONFILENAME);
        
        if (!File.Exists(dst)) { await CopyToPersistentAsync(VERSIONFILENAME); }

        string info = await File.ReadAllTextAsync(dst);
        localVersionInfo = JsonUtility.FromJson<VersionInfo>(info);
    }
    
    private static async Task RequestVersionFile()
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

        using UnityWebRequest req =  UnityWebRequest.Get(src);
        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            throw new IOException($"Read StreamingAssets failed: {src}\n{req.error}");
        }
        await File.WriteAllBytesAsync(dst, req.downloadHandler.data);
        return dst;
    }
}
