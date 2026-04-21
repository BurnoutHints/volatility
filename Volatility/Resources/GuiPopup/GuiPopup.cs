using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.GuiPopup)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class GuiPopup : Resource
{
    private const int PopupStructSize = 0xC0;

    public List<Popup> Popups { get; set; } = [];

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness)
    {
        base.WriteToStream(writer, endianness);

        const int PopupOffsetsStart = 0x40;

        Arch arch = ResourceArch;
        int popupCount = Popups.Count;
        int popupOffsetEntrySize = ResourceUtilities.GetPointerSize(arch);
        long firstPopupOffset = ResourceUtilities.AlignOffset(
            PopupOffsetsStart + ((long)popupCount * popupOffsetEntrySize),
            0x10);
        long totalSize = firstPopupOffset + ((long)popupCount * PopupStructSize);

        if (popupCount > short.MaxValue)
        {
            throw new InvalidDataException($"GuiPopup count {popupCount} exceeds int16_t storage.");
        }

        if (totalSize > short.MaxValue)
        {
            throw new InvalidDataException($"GuiPopup size 0x{totalSize:X} exceeds int16_t storage.");
        }

        writer.WritePointer(PopupOffsetsStart, arch);
        writer.Write((short)popupCount);
        writer.Write((short)totalSize);
        writer.WriteFixedBytes(null, PopupOffsetsStart - (int)writer.BaseStream.Position);

        writer.BaseStream.Position = PopupOffsetsStart;
        for (int i = 0; i < popupCount; i++)
        {
            writer.WritePointer((ulong)(firstPopupOffset + ((long)i * PopupStructSize)), arch);
        }

        writer.WriteFixedBytes(null, (int)(firstPopupOffset - writer.BaseStream.Position));
        writer.WriteSection((ulong)firstPopupOffset, Popups, Popup.Write);
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);

        Popups.Clear();

        Arch arch = ResourceArch;
        int popupOffsetEntrySize = ResourceUtilities.GetPointerSize(arch);

        ulong dataPtr = reader.ReadPointer(arch);
        short countRaw = reader.ReadInt16();
        short totalSize = reader.ReadInt16();
        if (arch == Arch.x64)
        {
            reader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);
        }

        if (countRaw < 0)
        {
            throw new InvalidDataException($"GuiPopup popup count cannot be negative. Found {countRaw}.");
        }

        int count = countRaw;

        if (count > 0 && dataPtr == 0)
        {
            throw new InvalidDataException(
                "GuiPopup pointer table cannot be null when popup count is greater than zero.");
        }

        if (dataPtr != 0 && dataPtr < (ulong)reader.BaseStream.Position)
        {
            throw new InvalidDataException(
                $"GuiPopup data pointer mismatch! Expected >= 0x{reader.BaseStream.Position:X}, found 0x{dataPtr:X}.");
        }

        long expectedMinimumSize = (long)dataPtr + ((long)count * popupOffsetEntrySize);
        if (count > 0 && reader.BaseStream.Length < expectedMinimumSize)
        {
            throw new InvalidDataException(
                $"GuiPopup offset table exceeds file length. Needed 0x{expectedMinimumSize:X}, found 0x{reader.BaseStream.Length:X}.");
        }

        List<ulong> popupOffsets = count > 0
            ? reader.ParseSection(dataPtr, count, r => r.ReadPointer(arch))
            : [];

        for (int i = 0; i < popupOffsets.Count; i++)
        {
            ulong popupOffset = popupOffsets[i];
            if (popupOffset == 0)
            {
                continue;
            }

            if (popupOffset + PopupStructSize > (ulong)reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    $"GuiPopup entry {i} at 0x{popupOffset:X} exceeds file length 0x{reader.BaseStream.Length:X}.");
            }

            reader.ParseSection(popupOffset, Popup.Read, out Popup popup);
            Popups.Add(popup);
        }

        if (totalSize > 0 && totalSize != reader.BaseStream.Length)
        {
            Console.WriteLine($"WARNING: GuiPopup reported size 0x{totalSize:X}, actual size 0x{reader.BaseStream.Length:X}.");
        }
    }

    public GuiPopup() : base() { }

    public GuiPopup(string path, Endian endianness)
        : base(path, endianness) { }

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

        public static Popup Read(ResourceBinaryReader reader)
        {
            Popup popup = new()
            {
                NameId = reader.ReadUInt64(),
                Name = ResourceUtilities.ReadFixedString(reader, 13)
            };

            reader.BaseStream.Seek(0x3, SeekOrigin.Current);

            popup.Style = (PopupStyle)reader.ReadInt32();
            popup.Icon = (PopupIcons)reader.ReadInt32();
            popup.TitleId = ResourceUtilities.ReadFixedString(reader, 32);
            popup.MessageId = ResourceUtilities.ReadFixedString(reader, 32);
            popup.MessageParam0 = (PopupParamTypes)reader.ReadInt32();
            popup.MessageParam1 = (PopupParamTypes)reader.ReadInt32();
            popup.MessageParamsUsed = reader.ReadInt32();
            popup.Button1Id = ResourceUtilities.ReadFixedString(reader, 32);
            popup.Button1Param = (PopupParamTypes)reader.ReadInt32();
            popup.Button1ParamUsed = reader.ReadByte() != 0;
            popup.Button2Id = ResourceUtilities.ReadFixedString(reader, 32);
            reader.BaseStream.Seek(0x3, SeekOrigin.Current);

            popup.Button2Param = (PopupParamTypes)reader.ReadInt32();
            popup.Button2ParamUsed = reader.ReadByte() != 0;
            reader.BaseStream.Seek(0x7, SeekOrigin.Current);

            return popup;
        }

        public static void Write(ResourceBinaryWriter writer, Popup popup)
        {
            writer.Write(popup.NameId);
            ResourceUtilities.WriteFixedString(writer, popup.Name, 13);
            writer.WriteFixedBytes(null, 0x3);
            writer.Write((int)popup.Style);
            writer.Write((int)popup.Icon);
            ResourceUtilities.WriteFixedString(writer, popup.TitleId, 32);
            ResourceUtilities.WriteFixedString(writer, popup.MessageId, 32);
            writer.Write((int)popup.MessageParam0);
            writer.Write((int)popup.MessageParam1);
            writer.Write(popup.MessageParamsUsed);
            ResourceUtilities.WriteFixedString(writer, popup.Button1Id, 32);
            writer.Write((int)popup.Button1Param);
            writer.Write((byte)(popup.Button1ParamUsed ? 1 : 0));
            ResourceUtilities.WriteFixedString(writer, popup.Button2Id, 32);
            writer.WriteFixedBytes(null, 0x3);
            writer.Write((int)popup.Button2Param);
            writer.Write((byte)(popup.Button2ParamUsed ? 1 : 0));
            writer.WriteFixedBytes(null, 0x7);
        }
    }
}
