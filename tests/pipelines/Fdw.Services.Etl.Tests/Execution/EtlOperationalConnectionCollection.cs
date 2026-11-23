namespace Fdw.Services.Etl.Tests;

/// <summary>
/// Serializes the test classes that read or set <c>PipelineTypes.OperationalConnection</c>, host configuration held in a
/// static, so one class setting it for a run never changes what another class sees.
/// </summary>
[CollectionDefinition(Name)]
public sealed class EtlOperationalConnectionCollection
{
    /// <summary>The collection's name.</summary>
    public const string Name = "EtlOperationalConnection";
}
