using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Files.Containers;
using Fdw.Data.Http.Containers;
using Fdw.Data.Http.Paths;
using Fdw.Data.MsSql;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Containers;

/// <summary>
/// FDW-5136: the container type options of this assembly are declared against <see cref="ContainerTypes"/>, which is
/// in another assembly. A lookup by name must answer the option, not the NotFound sentinel, or discovery stores every
/// table as containerType NotFound.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class ContainerTypesCrossAssemblyTests
{

    // FDW-6309: the Endpoint, File and DatabasePath options were declared with a private constructor and a static Instance, which the
    // module initializer cannot create (FDW027); each is a public parameterless type again, found by name.

    // FDW-6309: the option states what its containers support; nothing derives it from the option's name.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryRegisteredContainerTypeStatesAtLeastOneOperation()
    {
        var real = ContainerTypes.All().Where(option => !ReferenceEquals(option, ContainerTypes.NotFound)).ToList();

        real.ShouldNotBeEmpty();
        real.ShouldAllBe(option => option.SupportedOperations.Count > 0);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Table", "Query,Insert,Update,Delete")]
    [InlineData("View", "Query")]
    [InlineData("StoredProcedure", "Execute")]
    [InlineData("Endpoint", "Query")]
    [InlineData("File", "Query")]
    public void EachOptionStatesTheOperationsItsContainersDeclare(string name, string operations)
    {
        ContainerTypes.ByName(name).SupportedOperations.ShouldBe(operations.Split(','));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheNotFoundPlaceholderStatesNone()
    {
        ContainerTypes.NotFound.SupportedOperations.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnEndpointContainerIsBuiltWithTheRegisteredEndpointOption()
    {
        var created = EndpointContainer.Create(
            "Users", new HttpPath("/users"), new Mock<IContainerSchema>().Object, FormatTypes.ByName("Json"), ["GET"]);

        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        created.Value!.ContainerType.ShouldBeOfType<EndpointContainerType>();
        created.Value.ContainerType.ShouldNotBeSameAs(ContainerTypes.NotFound);
    }
}
