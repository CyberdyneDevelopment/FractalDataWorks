using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Descriptions.Configuration;

/// <summary>A description as a list shows it: the four properties every domain has, and the four parts of its key.</summary>
/// <remarks>Read from <c>catalog.Description</c> alone, in one query per page, so a list or a search by type and thing never reads a text.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class DescriptionDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the name of the <c>DescriptionTypes</c> option the thing belongs to.</summary>
    public string DescriptionType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the <c>DescriptionSubTypes</c> option: the sub-type of the text.</summary>
    public string DescriptionSubType { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical Id of the thing described.</summary>
    public Guid ThingId { get; set; }

    /// <summary>Gets or sets the language tag of the text.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Gets or sets the part of the text this row holds, from 1 in reading order; a text within the part length is one row, Ordinal 1 (FDW-6123).</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the name of the thing described, copied at the save so a search names its hits from the table alone.</summary>
    public string ThingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the thing's owner when it is a child, or nothing when it has none.</summary>
    public string? OwnerName { get; set; }

    /// <summary>Gets or sets the logical Id of the thing's owner when it is a child, or nothing when it has none.</summary>
    public Guid? OwnerId { get; set; }

    /// <summary>Gets or sets the text, copied from the implementation at the save so a search over the table finds it: a list is answered from the domain row alone (FDW-6123).</summary>
    public string Text { get; set; } = string.Empty;
}
