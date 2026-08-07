using Fdw.Services.Etl.JobTriggerSources;
using Xunit;

namespace Fdw.Services.Etl.JobTriggerSources.Tests;

/// <summary>
/// Shared fixture that ensures the TypeCollection source-generated module initializer
/// has run before any tests that depend on TypeCollection lookups (ByName, ById).
/// </summary>
public sealed class TypeCollectionFixture
{
    public TypeCollectionFixture()
    {
        // Trigger registration by accessing All() which forces the source-generated
        // initializer to populate the FrozenDictionary lookups.
        _ = JobTriggerSourceTypes.All();
    }
}

[CollectionDefinition("TypeCollection")]
public sealed class TypeCollectionCollection : ICollectionFixture<TypeCollectionFixture>
{
}
