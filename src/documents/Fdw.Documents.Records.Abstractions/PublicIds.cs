using System;
using System.Globalization;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// The key rule of the document record layer (FDW-3395): every record carries a public id that the application sets, and it must be a
/// UUIDv7. An id that is empty or of another version is refused; none is ever generated in its place.
/// </summary>
/// <remarks>
/// <para>
/// This is on purpose the opposite of FDW's configuration rows, whose providers mint an id when theirs is empty. A document record's id is
/// referenced from outside the application (links, legal holds, retention), so it is the application's to choose, and a missing one is the
/// application's bug to fix, not FDW's to paper over. Do not "fix" this check into a generator.
/// </para>
/// <para>
/// The application's own integer row id, which its tables join on, never appears in these abstractions.
/// </para>
/// </remarks>
public static class PublicIds
{
    // In the 32 hex digits of ToString("N"), the 13th is the version and the 17th carries the RFC 9562 variant bits (10xx).
    private const int VersionDigit = 12;
    private const int VariantDigit = 16;

    /// <summary>Checks that an id is a UUIDv7 the application set.</summary>
    /// <param name="id">The id.</param>
    /// <param name="entity">What the id is of (for example Document or StoredMedia), named in the refusal.</param>
    /// <returns>The id, or a failure coded <c>PublicIdInvalid</c> naming the entity, the id and the reason.</returns>
    public static IGenericResult<Guid> Check(Guid id, string entity)
    {
        if (string.IsNullOrWhiteSpace(entity))
        {
            throw new ArgumentException("The entity the id is of must be named.", nameof(entity));
        }

        if (id == Guid.Empty)
        {
            return Refused(id, entity, "it is empty, and a public id is set by the application, never generated");
        }

        var digits = id.ToString("N", CultureInfo.InvariantCulture);
        if (digits[VersionDigit] != '7')
        {
            return Refused(id, entity, string.Concat("it is a version ", digits.Substring(VersionDigit, 1), " id, not version 7"));
        }

        return digits[VariantDigit] is '8' or '9' or 'a' or 'b'
            ? GenericResult<Guid>.Success(id)
            : Refused(id, entity, "its variant is not the RFC 9562 variant");
    }

    private static IGenericResult<Guid> Refused(Guid id, string entity, string reason) =>
        GenericResult<Guid>.Failure(
            DocumentRecordResultCodes.ByName("PublicIdInvalid"),
            ResultDetails.Create("Id", id.ToString("D", CultureInfo.InvariantCulture), "Entity", entity, "Reason", reason));
}
