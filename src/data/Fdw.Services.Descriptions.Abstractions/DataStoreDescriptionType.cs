using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A data store's description: what the store is, in the words of whoever configured it (FDW-6208). Read by whoever holds <c>datastores:read</c>, the permission the data stores list asks. The store is the thing described, so the type has no owner: a store is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataStoreDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data store provider writes and reads its descriptions under.</summary>
    public const string OptionName = "DataStore";

    /// <summary>Initializes a new instance of the <see cref="DataStoreDescriptionType"/> class.</summary>
    public DataStoreDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: false)
    {
    }
}
