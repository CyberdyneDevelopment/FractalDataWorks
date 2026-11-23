
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A part that is an image.</summary>
public sealed class ImagePart : LanguageModelMessagePart
{
    /// <summary>Initializes a new instance of the <see cref="ImagePart"/> class.</summary>
    /// <param name="mediaType">The media type of the content, for example <c>application/pdf</c>.</param>
    /// <param name="data">The content.</param>
    public ImagePart(string mediaType, byte[] data)
    {
        MediaType = mediaType;
        Data = data;
    }

    /// <inheritdoc />
    public override IMessagePartKind Kind => MessagePartKinds.ByName("Image");

    /// <summary>Gets the media type of the content.</summary>
    public string MediaType { get; }

    /// <summary>Gets the content.</summary>
    public byte[] Data { get; }
}
