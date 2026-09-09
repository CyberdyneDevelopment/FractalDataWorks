using Fdw.Configuration;
using Moq;
using System;
using System.Linq;

namespace Fdw.Abstractions.Tests;

/// <summary>
/// Tests the configuration contract: what each of the three tiers is required to carry.
/// </summary>
/// <remarks>
/// The tiers are the point. <see cref="IGenericConfiguration"/> is identity and nothing else, so a
/// plain child row is never asked for a name it has no reason to have. A domain record adds the name
/// it is resolved by, the domain it is, and the implementation it names. An implementation record
/// adds its own name, so it can be looked up without first resolving the domain row pointing at it,
/// and deliberately carries no discriminator -- that value is the domain's to state.
/// </remarks>
public class IGenericConfigurationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericConfigurationCarriesIdentityOnly()
    {
        var type = typeof(IGenericConfiguration);

        type.IsInterface.ShouldBeTrue();
        type.GetProperties().Select(p => p.Name).ShouldBe(new[] { "Id" });

        var id = type.GetProperty("Id");
        id.ShouldNotBeNull();
        id!.PropertyType.ShouldBe(typeof(Guid));
        id.CanRead.ShouldBeTrue();
        id.CanWrite.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericConfigurationDoesNotDeclareTheMembersItsTiersOwn()
    {
        var type = typeof(IGenericConfiguration);

        // Why each is absent: Name belongs to the two tiers that are resolvable by name; SectionName
        // named an appsettings section nothing has read since the gateway became the one source; and
        // ServiceType/ServiceOptionType were a discriminator every record had to invent whether or
        // not it discriminated anything -- which is what let malformed rows claim one.
        type.GetProperty("Name").ShouldBeNull();
        type.GetProperty("SectionName").ShouldBeNull();
        type.GetProperty("ServiceType").ShouldBeNull();
        type.GetProperty("ServiceOptionType").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DomainConfigurationNamesItselfItsDomainAndItsImplementation()
    {
        var type = typeof(IDomainConfiguration);

        typeof(IGenericConfiguration).IsAssignableFrom(type).ShouldBeTrue();

        var name = type.GetProperty("Name");
        name.ShouldNotBeNull();
        name!.PropertyType.ShouldBe(typeof(string));
        name.CanWrite.ShouldBeTrue();

        var domain = type.GetProperty("Domain");
        domain.ShouldNotBeNull();
        domain!.PropertyType.ShouldBe(typeof(string));
        domain.CanWrite.ShouldBeFalse("the domain a record belongs to is stated, never assigned");

        var implementation = type.GetProperty("Implementation");
        implementation.ShouldNotBeNull();
        implementation!.PropertyType.ShouldBe(typeof(string));
        implementation.CanWrite.ShouldBeTrue("the discriminator is read from the row, not compiled in");

        type.GetProperty("ImplementationConfiguration").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ImplementationConfigurationCarriesItsOwnNameAndNoDiscriminator()
    {
        var type = typeof(IImplementationConfiguration);

        typeof(IGenericConfiguration).IsAssignableFrom(type).ShouldBeTrue();

        var name = type.GetProperty("Name");
        name.ShouldNotBeNull();
        name!.CanWrite.ShouldBeTrue();

        // The value that selected this implementation is the domain's to state, not this record's to
        // restate. Restating it is what produced rows claiming a discriminator they had no right to.
        type.GetProperty("Domain").ShouldBeNull();
        type.GetProperty("Implementation").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationCanSetId()
    {
        var expectedId = Guid.NewGuid();
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.Id).Returns(expectedId);

        mockConfig.Object.Id.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockDomainConfigurationCanSetName()
    {
        var mockConfig = new Mock<IDomainConfiguration>();
        mockConfig.Setup(c => c.Name).Returns("TestConfig");

        mockConfig.Object.Name.ShouldBe("TestConfig");
    }
}
