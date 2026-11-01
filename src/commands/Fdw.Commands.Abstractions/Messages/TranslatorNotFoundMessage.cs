using Fdw.Messages;

namespace Fdw.Commands.Abstractions.Messages;

/// <summary>
/// Message for when a translator cannot be found for the specified formats.
/// </summary>
public sealed class TranslatorNotFoundMessage : CommandMessage
{
    /// <summary>
    /// Gets the source format name.
    /// </summary>
    public string SourceFormat { get; }

    /// <summary>
    /// Gets the target format name.
    /// </summary>
    public string TargetFormat { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorNotFoundMessage"/> class.
    /// </summary>
    /// <param name="sourceFormat">The source format name.</param>
    /// <param name="targetFormat">The target format name.</param>
    public TranslatorNotFoundMessage(string sourceFormat, string targetFormat)
        : base(1004, "TranslatorNotFound", MessageSeverity.Error,
               $"No translator found for converting '{sourceFormat}' to '{targetFormat}'", "CMD_TRANS_404")
    {
        SourceFormat = sourceFormat;
        TargetFormat = targetFormat;
    }
}