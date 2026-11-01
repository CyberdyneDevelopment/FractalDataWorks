using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Data.DataSets;

/// <summary>
/// A field mapping applies a transformation, in <see cref="Ordinal"/> order -- one row of
/// <c>transform.FieldMappingTransformation</c> (FDW-846).
/// </summary>
/// <remarks>
/// A factless fact: keys and order only. What the transformation does lives on its implementation
/// (<c>FieldTransformationConfiguration</c>), so the same transformation can be attached to many field
/// mappings and edited in one place. A field mapping and a transformation identify one attachment;
/// <see cref="Id"/> is the single logical key the configuration write path addresses it by.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class FieldMappingTransformationConfiguration
{
    /// <summary>
    /// Gets or sets the row's logical key. It carries no fact: the configuration write path versions and
    /// retires a row by one key, and the attachment's identity (field mapping, transformation) is two.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field mapping the transformation is applied to.</summary>
    public Guid DataSetFieldMappingId { get; set; }

    /// <summary>Gets or sets the transformation applied.</summary>
    public Guid TransformationId { get; set; }

    /// <summary>Gets or sets the position in the field mapping's chain; applied in ascending order.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}
