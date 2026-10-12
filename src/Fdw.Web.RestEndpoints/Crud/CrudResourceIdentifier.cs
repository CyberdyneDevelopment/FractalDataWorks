using System;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>Decides whether the identifier a CRUD endpoint was given actually names anything.</summary>
/// <remarks>
/// Why this exists: an endpoint whose route fails to bind — a pattern naming a property the request
/// does not carry — leaves the identifier at its type's default, and the lookup that follows returns
/// "not found". The caller is then told the resource does not exist when the truth is that no
/// identifier ever arrived, which sends the search to the database rather than the route.
///
/// Why the all-zero GUID is checked by text: the identifier reaches here as a string, and
/// <c>Guid.Empty.ToString()</c> is a perfectly non-empty one — a null-or-whitespace check alone
/// reads it as a real identifier and lets the same silence through.
/// </remarks>
internal static class CrudResourceIdentifier
{
    /// <summary>Returns true when the identifier is absent or is a default that names nothing.</summary>
    /// <param name="identifier">The identifier the endpoint resolved from its request.</param>
    /// <returns>True when the endpoint was given nothing to look up.</returns>
    internal static bool NamesNothing(string? identifier)
        => string.IsNullOrWhiteSpace(identifier)
           || (Guid.TryParse(identifier, out var parsed) && parsed == Guid.Empty);
}
