using System;
using System.Globalization;
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
    /// <param name="maxBytes">The most the picture may weigh once decoded, as this host configured it (<see cref="UiFeedbackLimits.MaxPictureBytes"/>).</param>
    /// <returns>The picture, or the refusal naming what is wrong with it.</returns>
    public static IGenericResult<UiFeedbackPicture> Decode(ILogger logger, string dataUrl, int maxBytes)
    {
        if (!dataUrl.StartsWith(Prefix, StringComparison.Ordinal))
            return Refused(logger, "must be a PNG data URL that starts with data:image/png;base64,");

        var encoded = dataUrl.AsSpan(Prefix.Length);

        // Why checked before decoding: a base64 text of n characters holds at most 3n/4 bytes, so an oversized picture is refused without allocating for it.
        // Why in long: a configured limit near int.MaxValue must not overflow the comparison.
        if ((long)encoded.Length / 4 * 3 > (long)maxBytes + 3)
            return TooLarge(logger, maxBytes);

        var bytes = new byte[encoded.Length / 4 * 3];
        if (!Convert.TryFromBase64Chars(encoded, bytes, out var written))
            return Refused(logger, "is not valid base64");

        if (written > maxBytes)
            return TooLarge(logger, maxBytes);

        if (written < PngSignature.Length || !bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
            return Refused(logger, "is not a PNG picture");

        return GenericResult<UiFeedbackPicture>.Success(new UiFeedbackPicture { ContentType = PngContentType, Content = bytes.AsSpan(0, written).ToArray() });
    }

    // The refusal names the configured limit in bytes, the unit it is configured in, so the number in the message is the one to change.
    private static IGenericResult<UiFeedbackPicture> TooLarge(ILogger logger, int maxBytes)
        => Refused(logger, $"is larger than {maxBytes.ToString(CultureInfo.InvariantCulture)} bytes, the limit this host is configured with (Messaging:Default:UiFeedback:MaxPictureBytes)");

    private static IGenericResult<UiFeedbackPicture> Refused(ILogger logger, string reason)
        => GenericResult<UiFeedbackPicture>.Failure(MessagingLog.UiFeedbackFieldInvalid(logger, "screenshot", reason));
}
