using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 热更资源清单
/// </summary>

public static class ResManifest
{
    /// <summary>
    /// CSV 格式解析
    /// relative_path, md5, size
    /// </summary>
    /// <returns></returns>
    [Obsolete("this is csv parser, please use jsonParser instead")]
    public static async Task<Dictionary<string, ResUnit>> ParseResManifestAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) 
        {
            return new Dictionary<string, ResUnit>();
        }

        Dictionary<string, ResUnit> dict = new Dictionary<string, ResUnit>(10240);
        string[] lines = await System.IO.File.ReadAllLinesAsync(filePath);
        
        for (int i = 0, iMax = lines.Length; i < iMax; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            
            string[] item = lines[i].Split(',');
            if (item.Length < 3) continue;
            
            string bundleName = item[0];
            if (string.IsNullOrWhiteSpace(bundleName)) continue;
            
            dict[bundleName] = new ResUnit(bundleName, item[1], item[2]);
        }
        return dict;
    }

    private static async Task ParseJsonResManifestAsync(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return ;
        Dictionary<string, ResUnit> dict = new Dictionary<string, ResUnit>(10240);

        byte[] datas = await System.IO.File.ReadAllBytesAsync(filePath);
    }
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