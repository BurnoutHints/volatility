using System.Runtime.Serialization;

namespace Volatility;

[Serializable]
class InvalidPlatformException : Exception
{
    public InvalidPlatformException()
    {
    }

    public InvalidPlatformException(string? message) : base(message)
    {
    }

    public InvalidPlatformException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected InvalidPlatformException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public override string Message => "Invalid platform specified.";

    public static InvalidPlatformException SpecifyInvalidPlatform(string platform) 
    {
        return new InvalidPlatformException($"Invalid platform {platform} specified.");
    }
}