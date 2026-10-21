namespace Fdw.Services.Messaging;

/// <summary>
/// The picture of the screen kept with a UI comment, as it was sent (FDW-2143).
/// </summary>
public sealed class UiFeedbackPicture
{
    /// <summary>Gets or sets the picture's media type, such as <c>image/png</c>.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the picture's bytes.</summary>
    public byte[] Content { get; set; } = [];
}
