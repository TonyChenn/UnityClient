using System.IO;

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