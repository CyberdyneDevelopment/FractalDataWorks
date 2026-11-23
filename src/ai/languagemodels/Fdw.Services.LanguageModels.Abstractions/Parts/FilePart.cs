
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A part that is a file, such as a document.</summary>
public sealed class FilePart : LanguageModelMessagePart
{
    /// <summary>Initializes a new instance of the <see cref="FilePart"/> class.</summary>
    /// <param name="mediaType">The media type of the content, for example <c>application/pdf</c>.</param>
    /// <param name="data">The content.</param>
    public FilePart(string mediaType, byte[] data)
    {
        MediaType = mediaType;
        Data = data;
    }

    /// <inheritdoc />
    public override IMessagePartKind Kind => MessagePartKinds.ByName("File");

    /// <summary>Gets the media type of the content.</summary>
    public string MediaType { get; }

    /// <summary>Gets the content.</summary>
    public byte[] Data { get; }
}
