namespace Fdw.Services.Messaging;

/// <summary>
/// The limits a UI comment is held to (FDW-2143, FDW-7058): how long a note may be, how much a picture of the screen may weigh, how many components a comment
/// names and how long each may be, and how many comments a list returns at once.
/// </summary>
/// <remarks>
/// These are configuration, never code: the defaults are declared on the messaging service type's option (<c>DefaultMessagingServiceType</c>), a host may
/// change them at registration, and a value stated under <c>Messaging:Default:UiFeedback</c> wins. The registration builds one of these from what applies and
/// the service, its checks and the limits route read it; nothing else states a number.
/// </remarks>
public sealed class UiFeedbackLimits
{
    /// <summary>Initializes a new instance of the <see cref="UiFeedbackLimits"/> class.</summary>
    /// <param name="maxBodyLength">The most characters a note or a comment on it holds.</param>
    /// <param name="maxPictureBytes">The most a picture of the screen weighs, in bytes, once decoded.</param>
    /// <param name="maxAnchors">The most components a comment is about.</param>
    /// <param name="maxAnchorLength">The most characters one component's description holds.</param>
    /// <param name="maxPageSize">The most comments a list returns at once.</param>
    public UiFeedbackLimits(int maxBodyLength, int maxPictureBytes, int maxAnchors, int maxAnchorLength, int maxPageSize)
    {
        MaxBodyLength = maxBodyLength;
        MaxPictureBytes = maxPictureBytes;
        MaxAnchors = maxAnchors;
        MaxAnchorLength = maxAnchorLength;
        MaxPageSize = maxPageSize;
    }

    /// <summary>Gets the most characters a note or a comment on it holds.</summary>
    public int MaxBodyLength { get; }

    /// <summary>Gets the most a picture of the screen weighs, in bytes, once decoded.</summary>
    public int MaxPictureBytes { get; }

    /// <summary>Gets the most components a comment is about (FDW-2343).</summary>
    public int MaxAnchors { get; }

    /// <summary>Gets the most characters one component's description holds (FDW-2343).</summary>
    public int MaxAnchorLength { get; }

    /// <summary>Gets the most comments a list returns at once.</summary>
    public int MaxPageSize { get; }
}
