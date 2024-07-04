using static Volatility.Utilities.CgsIDUtilities;

namespace Volatility.Resource;

public abstract class Resource 
{
    public string CgsID = "";
    public string AssetName = "invalid";
    public string? ImportPath;
    virtual public Endian ResourceEndian { get; }
    virtual public string ResourceType => "Null"; // May become an enum in the future

    public Resource() {}

    public Resource(string path)
    {
        ImportPath = path;

        // Don't parse a directory
        if (new DirectoryInfo(path).Exists)
            return;

        string? name = Path.GetFileNameWithoutExtension(ImportPath);

        if (!string.IsNullOrEmpty(name))
        {
            // If the filename is a CgsID, we scan the users' ResourceDB (if available)
            // to find a matching asset name for the provided CgsID. If none is found,
            // the CgsID is used in place of a real asset name. If the filename is not a
            // CgsID, we simply use the file name as the asset name, and calculate a new CgsID.
            if (ValidateCgsID(name))
            {
                name = name.Replace("_", "");

                // We store CgsIDs in BE to be consistent with the original console releases.
                // This makes it easy to cross reference assets between all platforms.
                CgsID = ResourceEndian == Endian.LE ? FlipCgsIDEndian(name) : name;


                string newName = GetNameByCgsID(CgsID, ResourceType);
                AssetName = !string.IsNullOrEmpty(newName) ? newName : CgsID;
            }
            else
            {
                AssetName = name;
                CgsID = CalculateCgsID(name);
            }
        }

        using BinaryReader reader = new BinaryReader(new FileStream($"{path}", FileMode.Open));
        ParseFromStream(reader);
    }

    public abstract void WriteToStream(BinaryWriter writer);
    public virtual void ParseFromStream(BinaryReader reader) { }

    public enum Endian
    {
        LE,
        BE
    }
}