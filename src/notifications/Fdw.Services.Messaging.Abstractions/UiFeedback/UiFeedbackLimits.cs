namespace Fdw.Services.Messaging;

/// <summary>
/// The limits a UI comment is held to (FDW-2143): how long a note may be, how much a picture of the screen may weigh, and how many comments a list returns.
/// </summary>
/// <remarks>
/// Why a class and not constants on <see cref="IUiFeedbackService"/>: this package targets netstandard2.0, whose runtime cannot hold members in an interface
/// (CS8701).
/// </remarks>
public static class UiFeedbackLimits
{
    /// <summary>The most characters a note holds.</summary>
    public const int MaxBodyLength = 4000;

    /// <summary>The most a picture of the screen weighs, in bytes, once decoded.</summary>
    public const int MaxPictureBytes = 2 * 1024 * 1024;

    /// <summary>The most components a comment is about (FDW-2343).</summary>
    public const int MaxAnchors = 20;

    /// <summary>The most characters one component's description holds (FDW-2343).</summary>
    public const int MaxAnchorLength = 4000;

    /// <summary>The most comments a list returns at once.</summary>
    public const int MaxPageSize = 200;
}
