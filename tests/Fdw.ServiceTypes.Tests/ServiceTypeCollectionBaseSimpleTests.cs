using Fdw.Collections;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes.Tests.TestDoubles;

namespace Fdw.ServiceTypes.Tests;

public class ServiceTypeCollectionBaseSimpleTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeBaseExposesCorrectServiceType()
    {
        // Arrange & Act
        var sut = new TestServiceType();

        // Assert
        sut.ServiceType.ShouldBe(typeof(ITestService));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeBaseExposesCorrectFactoryType()
    {
        // Arrange & Act
        var sut = new TestServiceType();

        // Assert
        sut.FactoryType.ShouldBe(typeof(TestServiceFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeBaseExposesCorrectConfigurationType()
    {
        // Arrange & Act
        var sut = new TestServiceType();

        // Assert
        sut.ConfigurationType.ShouldBe(typeof(TestConfiguration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeBaseSectionNameMatchesConstructorArg()
    {
        // Arrange & Act
        var sut = new TestServiceType();

        // Assert
        sut.SectionName.ShouldBe("Services:TestType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeBaseProducesDeterministicId()
    {
        // Arrange & Act — two instances of the same option
        var firstInstance = new TestServiceType();
        var secondInstance = new TestServiceType();

        // Assert — identity is stable across instances, and it is not empty
        firstInstance.Id.ShouldBe(secondInstance.Id);
        firstInstance.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypesThatCloseTheBaseIdenticallyStillGetDistinctIds()
    {
        // A domain's options routinely share generic arguments — every option in SessionStateTypes is a
        // ServiceTypeBase<IGenericService, ISessionStateServiceFactory, IServiceConfiguration>. Identity
        // computed from those arguments is one value for the whole domain, and because
        // ServiceTypeCollectionBase.RegisterMember keys membership on it, every option after the first
        // is dropped without a word. Identity comes from the option's own type, which is unique.
        var first = new SameShapeServiceType("First");
        var second = new OtherShapeServiceType("Second");

        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypeIdTracksTheTypeRatherThanTheName()
    {
        // A name is unique only inside its collection, and Id is global: "Default" is an option name in
        // fifteen of them. Two options sharing a name are still two options, and their ids differ.
        new SameShapeServiceType("Shared").Id.ShouldNotBe(new OtherShapeServiceType("Shared").Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ServiceTypeCollectionBaseHasCorrectGenericParameters()
    {
        // Arrange & Act
        var type = typeof(ServiceTypeCollectionBase<,>);

        // Assert
        type.ShouldNotBeNull();
        type.GetGenericArguments().Length.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ServiceTypeCollectionBaseIsAbstractClass()
    {
        // Arrange & Act
        var type = typeof(ServiceTypeCollectionBase<,>);

        // Assert
        type.IsAbstract.ShouldBeTrue();
        type.IsClass.ShouldBeTrue();
    }
}
