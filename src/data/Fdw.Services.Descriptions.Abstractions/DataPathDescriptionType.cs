using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a data store's path (a schema, a folder, a set of endpoints): what it holds, in the words of whoever configured the store (FDW-6589). Read by whoever holds <c>datastores:read</c>, the permission that reads the store. Its owner is the data store: the store's save writes it, the store's read fills it, and it is removed with the store.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataPathDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data store provider writes and reads these descriptions under.</summary>
    public const string OptionName = "DataPath";

    /// <summary>Initializes a new instance of the <see cref="DataPathDescriptionType"/> class.</summary>
    public DataPathDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: true)
    {
    }
}
