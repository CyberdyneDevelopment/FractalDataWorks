using Fdw.Services.Resiliency.Abstractions;
using Xunit;

namespace Fdw.Services.Resiliency.Tests;

/// <summary>
/// Fixture that ensures ResiliencyPolicies and ResiliencyTypes TypeCollections are
/// fully initialized before any tests run. This prevents race conditions in the
/// source-generated EnsureFrozen() method when xUnit runs tests in parallel.
/// </summary>
public sealed class TypeCollectionFixture
{
    public TypeCollectionFixture()
    {
        // Force legacy TypeCollection to initialize.
        _ = ResiliencyPolicies.All();

        // Why: force ResiliencyTypes to initialize with all strategy types registered.
        // Referencing types from each strategy assembly triggers their module initializers,
        // which register the TypeOption with ResiliencyTypes before ByName() is called in tests.
        _ = ResiliencyTypes.All();
        _ = new Fdw.Services.Resiliency.Polly.PollyRetryResiliencyType();
        _ = new Fdw.Services.Resiliency.PrimaryBackup.PrimaryBackupResiliencyType();
        _ = new Fdw.Services.Resiliency.RetryNotify.RetryNotifyResiliencyType();
    }
}

/// <summary>
/// Collection definition that applies the TypeCollectionFixture to all tests.
/// All test classes that access ResiliencyPolicies should be decorated with
/// [Collection(nameof(ResiliencyTestCollection))].
/// </summary>
[CollectionDefinition(nameof(ResiliencyTestCollection))]
public sealed class ResiliencyTestCollection : ICollectionFixture<TypeCollectionFixture>
{
}
