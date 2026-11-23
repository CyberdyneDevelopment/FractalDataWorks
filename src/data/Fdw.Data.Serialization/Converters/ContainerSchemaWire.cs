using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Serialization.Converters;

/// <summary>The JSON shape of an <see cref="IContainerSchema"/>, read as a typed value rather than property by property.</summary>
internal sealed class ContainerSchemaWire
{
    /// <summary>Gets or sets the schema's fields; each is read by <see cref="FieldConverter"/>.</summary>
    public List<IField>? Fields { get; set; }
}
