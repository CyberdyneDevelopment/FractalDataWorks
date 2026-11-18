using System;
using System.Linq;
using Fdw.Services.Connections;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-6548: a data store's working areas are saved with the store, so a save that carries an empty list clears them. That follows from one mechanism: the store's generated mapper lists the
/// working-path rows as a cascaded child collection, and a child that hangs from the owner's row takes no cascade mode, so a save retires the replaced version's rows and writes the
/// list again (ImplementationProviderBase, FDW-6394). These tests pin the two facts that mechanism rests on; they do not run a save, which the started-host proof does against a database.
/// </summary>
public sealed class DataStoreWorkingPathCascadeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataStores.FDW6548")]
    public void TheStoresMapperCascadesItsWorkingPathRowsAsATypedChildListThatTakesNoCascadeMode()
    {
        var rows = new DataStoreImplementationConfigurationPocoMapper().CascadeChildren
            .SingleOrDefault(child => string.Equals(child.BoundPropertyName, nameof(DataStoreImplementationConfiguration.WorkingPathRows), StringComparison.Ordinal));

        rows.ShouldNotBeNull();
        rows.ChildType.ShouldBe(typeof(DataStoreWorkingPathConfiguration));

        // A mode is declared only by a child that outlives its owner's versions (it hangs from the owner's durable Id); this one hangs from the owner's row, as data.DataPath does.
        rows.CascadeModeName.ShouldBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataStores.FDW6548")]
    public void TheReadOnlyListOfNamesIsNotAChildCollectionSoOnlyTheRowsAreStoredAndRetired()
    {
        new DataStoreImplementationConfigurationPocoMapper().CascadeChildren
            .Select(child => child.BoundPropertyName)
            .ShouldNotContain(nameof(IDataStoreImplementationConfiguration.WorkingPaths));
    }
}
