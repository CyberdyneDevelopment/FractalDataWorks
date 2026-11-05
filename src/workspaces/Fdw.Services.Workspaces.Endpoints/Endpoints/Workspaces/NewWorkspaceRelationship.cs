using System;
using System.Globalization;
using Fdw.Conventions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// A relationship being created: its id, and the name it takes when the request states no label.
/// </summary>
/// <param name="Id">The relationship's new id.</param>
/// <remarks>
/// Why a type for one id: the unlabelled name used to be a second, unrelated new GUID, so a relationship's name was never its id.
/// FDW055 accepts a convention only as a [PlatformDefault] member, and the id is not on the request.
/// </remarks>
internal readonly record struct NewWorkspaceRelationship(Guid Id)
{
    /// <summary>Gets the name of a relationship the request does not label: its own id.</summary>
    [PlatformDefault]
    public string ConventionalName => Id.ToString("D", CultureInfo.InvariantCulture);
}
