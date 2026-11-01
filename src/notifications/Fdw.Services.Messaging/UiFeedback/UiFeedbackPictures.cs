using System;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging;

/// <summary>
/// Turns the picture a tester sent, a PNG data URL, into the picture that is kept (FDW-2143). A picture that is too big or is not a PNG is refused whole:
/// nothing is cut down or converted.
/// </summary>
internal static class UiFeedbackPictures
{
    private const string Prefix = "data:image/png;base64,";
    private const string PngContentType = "image/png";

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Decodes a PNG data URL.
    /// </summary>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="dataUrl">The data URL the tester sent.</param>
    /// <returns>The picture, or the refusal naming what is wrong with it.</returns>
    public static IGenericResult<UiFeedbackPicture> Decode(ILogger logger, string dataUrl)
    {
        if (!dataUrl.StartsWith(Prefix, StringComparison.Ordinal))
            return Refused(logger, "must be a PNG data URL that starts with data:image/png;base64,");

        var encoded = dataUrl.AsSpan(Prefix.Length);

        // Why checked before decoding: a base64 text of n characters holds at most 3n/4 bytes, so an oversized picture is refused without allocating for it.
        if (encoded.Length / 4 * 3 > UiFeedbackLimits.MaxPictureBytes + 3)
            return Refused(logger, $"is larger than {UiFeedbackLimits.MaxPictureBytes / (1024 * 1024)} MB");

        var bytes = new byte[encoded.Length / 4 * 3];
        if (!Convert.TryFromBase64Chars(encoded, bytes, out var written))
            return Refused(logger, "is not valid base64");

        if (written > UiFeedbackLimits.MaxPictureBytes)
            return Refused(logger, $"is larger than {UiFeedbackLimits.MaxPictureBytes / (1024 * 1024)} MB");

        if (written < PngSignature.Length || !bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
            return Refused(logger, "is not a PNG picture");

        return GenericResult<UiFeedbackPicture>.Success(new UiFeedbackPicture { ContentType = PngContentType, Content = bytes.AsSpan(0, written).ToArray() });
    }

    private static IGenericResult<UiFeedbackPicture> Refused(ILogger logger, string reason)
        => GenericResult<UiFeedbackPicture>.Failure(MessagingLog.UiFeedbackFieldInvalid(logger, "screenshot", reason));
}
