using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a field of a data store's container: what the column or element holds, in the user's words, apart from the source's own (SourceDescription) (FDW-6589). Read by whoever holds <c>datastores:read</c>, the permission that reads the store. Its owner is the data store: the store's save writes it, the store's read fills it, and it is removed with the store.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataContainerFieldDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data store provider writes and reads these descriptions under.</summary>
    public const string OptionName = "DataContainerField";

    /// <summary>Initializes a new instance of the <see cref="DataContainerFieldDescriptionType"/> class.</summary>
    public DataContainerFieldDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: true)
    {
    }
}
