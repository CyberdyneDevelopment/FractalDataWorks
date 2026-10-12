using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>Names the probe data store types and connection kinds the data store type selection tests use.</summary>
public static class DataStoreTypeSelectionProbes
{
    public const string ProbeStoreName = "ProbeFiles";
    public const string AbsentStoreName = "ProbeAbsent";
}

[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataStoreTypes), DataStoreTypeSelectionProbes.ProbeStoreName)]
public sealed class ProbeFilesDataStoreType : DataStoreTypeBase<IDataStoreImplementationConfiguration>
{
    public ProbeFilesDataStoreType() : base(DataStoreTypeSelectionProbes.ProbeStoreName, "Probe files", "A data store type only these tests register") { }

    public override IDataStoreBuilder SupplyBuilder(ILogger? logger = null) => throw new NotSupportedException();
}

[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), "ProbeTransport")]
public sealed class ProbeTransportConnectionType : ConnectionTypeBase<IGenericConnection, IMockConnectionFactory, MockConnectionConfiguration>
{
    public ProbeTransportConnectionType() : base("ProbeTransport", "ProbeTransport", "Probe transport", "Carries files for these tests", "Test") { }

    protected override Fdw.Results.IGenericResult<bool> DecideHostIdentity(MockConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<bool>.Success(false);

    protected override Fdw.Results.IGenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots> DescribeCredentials(MockConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots>.Success(Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots.None);

    protected override Fdw.Results.IGenericResult BindCredentialReferences(
        MockConnectionConfiguration configuration,
        string secretManagerName,
        System.Collections.Generic.IReadOnlyDictionary<string, string> secretKeyByField)
        => Fdw.Results.GenericResult.Success();

    protected override IReadOnlyList<string> DataStoreTypeNames => [DataStoreTypeSelectionProbes.AbsentStoreName, DataStoreTypeSelectionProbes.ProbeStoreName];
}

[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), "ProbeNoStores")]
public sealed class ProbeNoStoresConnectionType : ConnectionTypeBase<IGenericConnection, IMockConnectionFactory, MockConnectionConfiguration>
{
    public ProbeNoStoresConnectionType() : base("ProbeNoStores", "ProbeNoStores", "Probe no stores", "Backs no data store type", "Test") { }

    protected override Fdw.Results.IGenericResult<bool> DecideHostIdentity(MockConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<bool>.Success(false);

    protected override Fdw.Results.IGenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots> DescribeCredentials(MockConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots>.Success(Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots.None);

    protected override Fdw.Results.IGenericResult BindCredentialReferences(
        MockConnectionConfiguration configuration,
        string secretManagerName,
        System.Collections.Generic.IReadOnlyDictionary<string, string> secretKeyByField)
        => Fdw.Results.GenericResult.Success();
}

