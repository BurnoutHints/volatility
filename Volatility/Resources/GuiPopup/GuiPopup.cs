using System.Text;

namespace Volatility.Resources;

public class GuiPopup : Resource
{
    public List<Popup> Popups { get; } = new();

    const int PopupStructSize = 0xC0;

    public override ResourceType GetResourceType() => ResourceType.GuiPopup;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness)
    {
        ushort size = (ushort)(Popups.Count * PopupStructSize);
        base.WriteToStream(writer, endianness);
        long start = writer.BaseStream.Position;
        writer.Write((uint)0x8);
        writer.Write((short)Popups.Count);
        writer.Write((short)PopupStructSize);
        foreach (var p in Popups)
            WriteOne(writer, p);
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);
        Popups.Clear();
        long start = reader.BaseStream.Position;
        uint dataPtr = reader.ReadUInt32();
        short count = reader.ReadInt16();
        short elemSize = reader.ReadInt16();
        long ret = reader.BaseStream.Position;
        reader.BaseStream.Position = start + dataPtr;
        for (int i = 0; i < count; i++)
            Popups.Add(ReadOne(reader));
        reader.BaseStream.Position = ret;
    }

    static Popup ReadOne(ResourceBinaryReader r)
    {
        Popup p = new Popup();
        p.NameId = r.ReadUInt64();
        p.Name = ReadFixedString(r, 13);
        r.BaseStream.Position += 3;
        p.Style = (PopupStyle)r.ReadInt32();
        p.Icon = (PopupIcons)r.ReadInt32();
        p.TitleId = ReadFixedString(r, 32);
        p.MessageId = ReadFixedString(r, 32);
        p.MessageParam0 = (PopupParamTypes)r.ReadInt32();
        p.MessageParam1 = (PopupParamTypes)r.ReadInt32();
        p.MessageParamsUsed = r.ReadInt32();
        p.Button1Id = ReadFixedString(r, 32);
        p.Button1Param = (PopupParamTypes)r.ReadInt32();
        p.Button1ParamUsed = r.ReadByte() != 0;
        p.Button2Id = ReadFixedString(r, 32);
        r.BaseStream.Position += 3;
        p.Button2Param = (PopupParamTypes)r.ReadInt32();
        p.Button2ParamUsed = r.ReadByte() != 0;
        r.BaseStream.Position += 7;
        return p;
    }

    static void WriteOne(EndianAwareBinaryWriter w, Popup p)
    {
        w.Write(p.NameId);
        WriteFixedString(w, p.Name, 13);
        w.Write(new byte[3]);
        w.Write((int)p.Style);
        w.Write((int)p.Icon);
        WriteFixedString(w, p.TitleId, 32);
        WriteFixedString(w, p.MessageId, 32);
        w.Write((int)p.MessageParam0);
        w.Write((int)p.MessageParam1);
        w.Write(p.MessageParamsUsed);
        WriteFixedString(w, p.Button1Id, 32);
        w.Write((int)p.Button1Param);
        w.Write((byte)(p.Button1ParamUsed ? 1 : 0));
        WriteFixedString(w, p.Button2Id, 32);
        w.Write(new byte[3]);
        w.Write((int)p.Button2Param);
        w.Write((byte)(p.Button2ParamUsed ? 1 : 0));
        w.Write(new byte[7]);
    }

    static string ReadFixedString(ResourceBinaryReader r, int len)
    {
        var bytes = r.ReadBytes(len);
        int n = Array.IndexOf<byte>(bytes, 0);
        if (n >= 0) return Encoding.ASCII.GetString(bytes, 0, n);
        return Encoding.ASCII.GetString(bytes);
    }

    static void WriteFixedString(EndianAwareBinaryWriter w, string? s, int len)
    {
        var bytes = Encoding.ASCII.GetBytes(s ?? string.Empty);
        if (bytes.Length > len) w.Write(bytes, 0, len);
        else
        {
            w.Write(bytes);
            if (bytes.Length < len) w.Write(new byte[len - bytes.Length]);
        }
    }

    public GuiPopup() : base() { }
    public GuiPopup(string path, Endian endianness) : base(path, endianness) { }

    public enum PopupStyle : int
    {
        E_POPUPSTYLE_CRASHNAV_WAIT = 0,
        E_POPUPSTYLE_CRASHNAV_OK = 1,
        E_POPUPSTYLE_CRASHNAV_OKCANCEL = 2,
        E_POPUPSTYLE_CRASHNAV_ONLINE_WAIT = 3,
        E_POPUPSTYLE_CRASHNAV_ONLINE_OK = 4,
        E_POPUPSTYLE_CRASHNAV_ONLINE_OKCANCEL = 5,
        E_POPUPSTYLE_INGAME_WAIT = 6,
        E_POPUPSTYLE_INGAME_OK = 7,
        E_POPUPSTYLE_INGAME_OKCANCEL = 8,
        E_POPUPSTYLE_INGAME_ONLINE_WAIT = 9,
        E_POPUPSTYLE_INGAME_ONLINE_OK = 10,
        E_POPUPSTYLE_INGAME_ONLINE_OKCANCEL = 11,
        E_POPUPSTYLE_INGAME_ONLINE_ENTER_FREEBURN = 12,
        E_POPUPSTYLE_CUSTOM = 13,
        E_POPUPSTYLE_ISLAND_ENTER = 14,
        E_POPUPSTYLE_ISLAND_BUY = 15,
        E_POPUPSTYLE_COUNT = 16
    }

    public enum PopupIcons : int
    {
        E_POPUPICONS_INVISIBLE = 0,
        E_POPUPICONS_WARNING = 1,
        E_POPUPICONS_COUNT = 2
    }

    public enum PopupParamTypes : int
    {
        E_POPUPPARAMTYPES_UNUSED = 0,
        E_POPUPPARAMTYPES_STRING = 1,
        E_POPUPPARAMTYPES_STRING_ID = 2,
        E_POPUPPARAMTYPES_COUNT = 3
    }

    public struct Popup
    {
        public CgsID NameId;
        public string Name;
        public PopupStyle Style;
        public PopupIcons Icon;
        public string TitleId;
        public string MessageId;
        public PopupParamTypes MessageParam0;
        public PopupParamTypes MessageParam1;
        public int MessageParamsUsed;
        public string Button1Id;
        public PopupParamTypes Button1Param;
        public bool Button1ParamUsed;
        public string Button2Id;
        public PopupParamTypes Button2Param;
        public bool Button2ParamUsed;
    }
}