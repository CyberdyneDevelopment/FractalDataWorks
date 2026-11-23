namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// The limits a UI comment is held to, as the host configured them (FDW-7058), so a form checks what it sends against the same numbers the API refuses by.
/// </summary>
public class UiFeedbackLimitsResponse
{
    /// <summary>Gets or sets the most characters a note, or a comment on it, holds.</summary>
    public int MaxBodyLength { get; set; }

    /// <summary>Gets or sets the most a picture of the screen weighs, in bytes once decoded.</summary>
    public int MaxPictureBytes { get; set; }

    /// <summary>Gets or sets the most components a comment is about.</summary>
    public int MaxAnchors { get; set; }

    /// <summary>Gets or sets the most characters one component's description holds.</summary>
    public int MaxAnchorLength { get; set; }

    /// <summary>Gets or sets the most comments a list returns at once.</summary>
    public int MaxPageSize { get; set; }

    /// <summary>Answers the configured limits.</summary>
    /// <param name="limits">The limits the service holds.</param>
    /// <returns>The response.</returns>
    public static UiFeedbackLimitsResponse Of(UiFeedbackLimits limits) => new()
    {
        MaxBodyLength = limits.MaxBodyLength,
        MaxPictureBytes = limits.MaxPictureBytes,
        MaxAnchors = limits.MaxAnchors,
        MaxAnchorLength = limits.MaxAnchorLength,
        MaxPageSize = limits.MaxPageSize,
    };
}
