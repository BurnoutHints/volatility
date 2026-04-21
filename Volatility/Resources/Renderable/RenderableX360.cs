namespace Volatility.Resources;

[ResourceRegistration(RegistrationPlatforms.X360)]
public class RenderableX360 : RenderableBase
{
    public override Endian ResourceEndian => Endian.BE;
    public override Platform ResourcePlatform => Platform.X360;

    public RenderableX360(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
