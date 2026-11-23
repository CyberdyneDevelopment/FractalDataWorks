using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a data store's container (a table, a view, a file, an endpoint): what it holds (FDW-6589). Read by whoever holds <c>datastores:read</c>, the permission that reads the store. Its owner is the data store: the store's save writes it, the store's read fills it, and it is removed with the store.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataContainerDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data store provider writes and reads these descriptions under.</summary>
    public const string OptionName = "DataContainer";

    /// <summary>Initializes a new instance of the <see cref="DataContainerDescriptionType"/> class.</summary>
    public DataContainerDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: true)
    {
    }
}
