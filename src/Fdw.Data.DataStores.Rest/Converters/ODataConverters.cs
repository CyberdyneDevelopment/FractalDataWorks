using System;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// TypeCollection for OData EDM primitive type converters.
/// Child collection of DataTypeConverters.
/// SourceType is the EDM name after stripping the "Edm." prefix (e.g. "Boolean", "Int32").
/// </summary>
[TypeOption(typeof(DataTypeConverters), "OData", RestrictToCurrentCompilation = true)]
[TypeCollection(typeof(DataTypeConverterBase), typeof(IDataTypeConverter), typeof(ODataConverters))]
public abstract partial class ODataConverters : DataTypeConverterCollectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataConverters"/> class.
    /// </summary>
    protected ODataConverters() : base("OData", "OData EDM") { }

    // Source generator creates ByName(), ById(), All(), NotFound, and one static property per [TypeOption].

    /// <summary>
    /// Gets a converter by EDM source type name (e.g. "Boolean", "TimeOfDay", "Int32").
    /// Returns NotFound if not found.
    /// </summary>
    public static IDataTypeConverter BySourceType(string sourceType)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            return NotFound;

        return All().FirstOrDefault(c =>
            c.SourceType.Equals(sourceType, StringComparison.Ordinal))
            ?? NotFound;
    }
}
