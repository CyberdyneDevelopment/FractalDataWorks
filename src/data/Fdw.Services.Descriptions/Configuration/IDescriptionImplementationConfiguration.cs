using System;
using Fdw.Configuration;

namespace Fdw.Services.Descriptions.Configuration;

/// <summary>The contract every description implementation carries.</summary>
/// <remarks>
/// The key lives on the domain row (<see cref="DescriptionDomainConfiguration"/>), because a list shows it. The implementation row holds the text, which
/// only the description itself carries.
/// </remarks>
public interface IDescriptionImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the text, which is never empty.</summary>
    string Text { get; set; }

    /// <summary>Gets or sets the name of the description type. A list field: it is written to the domain row.</summary>
    string DescriptionType { get; set; }

    /// <summary>Gets or sets the name of the description sub-type. A list field: it is written to the domain row.</summary>
    string DescriptionSubType { get; set; }

    /// <summary>Gets or sets the logical Id of the thing described. A list field: it is written to the domain row.</summary>
    Guid ThingId { get; set; }

    /// <summary>Gets or sets the language tag of the text. A list field: it is written to the domain row.</summary>
    string Language { get; set; }

    /// <summary>Gets or sets the part of the text this row holds, from 1 in reading order. A list field: it is written to the domain row.</summary>
    int Ordinal { get; set; }

    /// <summary>Gets or sets the name of the thing described. A list field: it is written to the domain row.</summary>
    string ThingName { get; set; }

    /// <summary>Gets or sets the name of the thing's owner, or nothing when it has none. A list field: it is written to the domain row.</summary>
    string? OwnerName { get; set; }

    /// <summary>Gets or sets the logical Id of the thing's owner, or nothing when it has none. A list field: it is written to the domain row.</summary>
    Guid? OwnerId { get; set; }
}
