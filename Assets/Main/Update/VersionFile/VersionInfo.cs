/// <summary>
/// 版控文件信息
/// </summary>

public struct VersionInfo
{
    /// <summary>
    /// 大版本号
    /// </summary>
    public ushort ID;
    /// <summary>
    /// 小版本号
    /// </summary>
    public ushort Tag;
    /// <summary>
    /// CDN地址1
    /// </summary>
    public string UpdateUrl1;
    /// <summary>
    /// CDN地址2
    /// </summary>
    public string UpdateUrl2;
    /// <summary>
    /// 客户端下载地址
    /// </summary>
    public string ClientUrl;
    public ushort Exclude;
    /// <summary>
    /// 资源清单MD5
    /// </summary>
    public string MD5;
    public uint Time;

    public ushort BaseID;
    public uint NewTag;
}
