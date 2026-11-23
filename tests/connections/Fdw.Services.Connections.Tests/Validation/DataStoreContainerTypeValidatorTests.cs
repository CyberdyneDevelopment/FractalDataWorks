using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections.TestDouble;
using Fdw.Services.Connections.Validation;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using FluentValidation.TestHelper;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests.Validation;

/// <summary>
/// FDW-6428: a container is checked when its store is written, not only when the store is built. A container that states no type, or a type nobody registered, is refused
/// by the store's own validator, naming the container and the field, so one POST cannot store a store whose builder will then refuse it and leave the whole store out of the tree.
/// </summary>
[Trait("Category", "Validation")]
public sealed class DataStoreContainerTypeValidatorTests
{
    private readonly DataStoreConfigurationValidator _validator = new();

    private static DataStoreImplementationConfiguration Store(string? containerTypeId, string implementation = "MsSql")
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Platform",
            ConnectionId = Guid.NewGuid(),
            Implementation = implementation,
            Paths =
            [
                new DataPathConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = "data",
                    PathValue = "data",
                    Containers = [new DataContainerConfiguration { Id = Guid.NewGuid(), Name = "Orders", TypeId = containerTypeId }],
                },
            ],
        };

    [Fact]
    [Trait("Priority", "P0")]
    public void AContainerThatStatesNoTypeIsRefusedNamingTheContainerAndTheField()
    {
        var result = _validator.TestValidate(Store(null));

        var refusal = result.Errors.ShouldHaveSingleItem();
        refusal.PropertyName.ShouldEndWith("TypeId");
        refusal.ErrorMessage.ShouldContain("Orders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AContainerThatStatesATypeNobodyRegisteredIsRefusedNamingTheContainerAndTheType()
    {
        var result = _validator.TestValidate(Store("NoSuchContainerType"));

        var refusal = result.Errors.ShouldHaveSingleItem();
        refusal.PropertyName.ShouldEndWith("TypeId");
        refusal.ErrorMessage.ShouldContain("Orders");
        refusal.ErrorMessage.ShouldContain("NoSuchContainerType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AStoreWithNoContainersIsStillValid()
    {
        var store = Store("Table");
        store.Paths.Single().Containers.Clear();

        _validator.TestValidate(store).IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AContainerOfAnotherStoreTypesOptionIsRefusedNamingTheContainerTheTypeAndWhatTheStoreTypeHolds()
    {
        // ProbeFiles states that it holds Endpoint; a Table is a registered container type, but not one of its own.
        var result = _validator.TestValidate(Store("Table", DataStoreTypeSelectionProbes.ProbeStoreName));

        var refusal = result.Errors.ShouldHaveSingleItem();
        refusal.PropertyName.ShouldEndWith("TypeId");
        refusal.ErrorMessage.ShouldContain("Orders");
        refusal.ErrorMessage.ShouldContain("Table");
        refusal.ErrorMessage.ShouldContain(DataStoreTypeSelectionProbes.ProbeStoreName);
        refusal.ErrorMessage.ShouldContain("Endpoint");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AContainerOfTheStoreTypesOwnOptionIsAccepted()
        => _validator.TestValidate(Store("Endpoint", DataStoreTypeSelectionProbes.ProbeStoreName)).IsValid.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRuleTheBuildersAskGivesTheSameRefusalAsTheWrite()
    {
        var container = new DataContainerConfiguration { Id = Guid.NewGuid(), Name = "Orders", TypeId = "Table" };

        var built = DataContainerTypeRule.Check(container, DataStoreTypes.ByName(DataStoreTypeSelectionProbes.ProbeStoreName), NullLogger.Instance);

        built.IsFailure.ShouldBeTrue();
        built.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe("DATA-31043");
    }
}
