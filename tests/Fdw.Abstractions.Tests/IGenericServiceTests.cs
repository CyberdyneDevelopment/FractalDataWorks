using Fdw.Abstractions;
using Fdw.Results;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Abstractions.Tests;

/// <summary>
/// Tests for IGenericService interface contract.
/// </summary>
public class IGenericServiceTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericServiceInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericService);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericServiceHasIdProperty()
    {
        // Assert
        var type = typeof(IGenericService);
        var property = InterfaceProperty(type, "Id");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericServiceHasServiceTypeProperty()
    {
        // Assert
        var type = typeof(IGenericService);
        var property = InterfaceProperty(type, "ServiceType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericServiceHasIsAvailableProperty()
    {
        // Assert
        var type = typeof(IGenericService);
        var property = InterfaceProperty(type, "IsAvailable");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericServiceHasExecuteGenericMethod()
    {
        // Assert
        var type = typeof(IGenericService);
        var methods = type.GetMethods();
        var genericExecuteMethod = Array.Find(methods, m =>
            m.Name == "Execute" &&
            m.IsGenericMethod &&
            m.GetParameters().Length == 2);

        genericExecuteMethod.ShouldNotBeNull();
        genericExecuteMethod!.IsGenericMethod.ShouldBeTrue();
        genericExecuteMethod.ReturnType.IsGenericType.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericServiceHasExecuteNonGenericMethod()
    {
        // Assert
        var type = typeof(IGenericService);
        var methods = type.GetMethods();
        var executeMethod = Array.Find(methods, m =>
            m.Name == "Execute" &&
            !m.IsGenericMethod &&
            m.GetParameters().Length == 2);

        executeMethod.ShouldNotBeNull();
        executeMethod!.ReturnType.ShouldBe(typeof(Task<IGenericResult>));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task MockServiceCanExecuteGenericCommand()
    {
        // Arrange
        var mockService = new Mock<IGenericService>();
        var mockCommand = Mock.Of<IGenericCommand>();
        var expectedResult = Mock.Of<IGenericResult<string>>();

        mockService
            .Setup(s => s.Execute<string>(It.IsAny<IGenericCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await mockService.Object.Execute<string>(mockCommand, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResult);
        mockService.Verify(s => s.Execute<string>(mockCommand, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task MockServiceCanExecuteNonGenericCommand()
    {
        // Arrange
        var mockService = new Mock<IGenericService>();
        var mockCommand = Mock.Of<IGenericCommand>();
        var expectedResult = Mock.Of<IGenericResult>();

        mockService
            .Setup(s => s.Execute(It.IsAny<IGenericCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await mockService.Object.Execute(mockCommand, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedResult);
        mockService.Verify(s => s.Execute(mockCommand, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockServiceCanSetId()
    {
        // Arrange
        var mockService = new Mock<IGenericService>();
        mockService.Setup(s => s.Id).Returns("test-service-id");

        // Act
        var id = mockService.Object.Id;

        // Assert
        id.ShouldBe("test-service-id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockServiceCanSetServiceType()
    {
        // Arrange
        var mockService = new Mock<IGenericService>();
        mockService.Setup(s => s.ServiceType).Returns("TestService");

        // Act
        var serviceType = mockService.Object.ServiceType;

        // Assert
        serviceType.ShouldBe("TestService");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockServiceCanSetIsAvailable()
    {
        // Arrange
        var mockService = new Mock<IGenericService>();
        mockService.Setup(s => s.IsAvailable).Returns(true);

        // Act
        var isAvailable = mockService.Object.IsAvailable;

        // Assert
        isAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockServiceIsAvailableCanBeFalse()
    {
        // Arrange
        var mockService = new Mock<IGenericService>();
        mockService.Setup(s => s.IsAvailable).Returns(false);

        // Act
        var isAvailable = mockService.Object.IsAvailable;

        // Assert
        isAvailable.ShouldBeFalse();
    }

    // Why the hierarchy is walked rather than Type.GetProperty alone: on an INTERFACE, GetProperty
    // does not search base interfaces the way it searches base classes. Id, ServiceType and
    // IsAvailable moved to IPlatformService when it was split out of IGenericService, and these
    // three assertions have reported them missing ever since -- while every caller still reads them
    // off an IGenericService, because inheriting a member is having it.
    private static global::System.Reflection.PropertyInfo? InterfaceProperty(global::System.Type type, string name)
        => type.GetProperty(name)
           ?? global::System.Linq.Enumerable.FirstOrDefault(
                  global::System.Linq.Enumerable.Select(type.GetInterfaces(), i => i.GetProperty(name)),
                  p => p is not null);
}
