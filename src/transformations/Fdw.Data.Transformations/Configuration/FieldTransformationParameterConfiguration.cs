using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One parameter value of a field transformation, named by a parameter its operation declares.
/// A child of <see cref="FieldTransformationConfiguration"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class FieldTransformationParameterConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>
    /// Not a column. A parameter row is identified by its owner and its name (the container's declared
    /// key), and the columns read and written are the ones the container declares.
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parameter name, as the operation declares it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter value.</summary>
    public string Value { get; set; } = string.Empty;
}
