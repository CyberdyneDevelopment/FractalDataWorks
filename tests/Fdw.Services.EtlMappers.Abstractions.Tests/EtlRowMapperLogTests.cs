using Fdw.Messages;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace Fdw.Services.EtlMappers.Abstractions.Tests;

/// <summary>
/// Tests for EtlRowMapperLog MessageLogging methods.
/// These methods are source-generated, so we verify they exist and have correct signatures.
/// </summary>
public class EtlRowMapperLogTests
{
    private readonly Mock<ILogger> _logger = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EtlRowMapperLogClassExists()
    {
        // Act
        var type = typeof(EtlRowMapperLog);

        // Assert
        type.ShouldNotBeNull();
        type.IsClass.ShouldBeTrue();
        type.IsAbstract.ShouldBeTrue();
        type.IsSealed.ShouldBeTrue(); // static class
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperInitializingMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.MapperInitializing),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[0].Name.ShouldBe("logger");
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("mapperType");
        parameters[2].ParameterType.ShouldBe(typeof(int));
        parameters[2].Name.ShouldBe("fieldCount");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperCompiledMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.MapperCompiled),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("mapperType");
        parameters[2].ParameterType.ShouldBe(typeof(double));
        parameters[2].Name.ShouldBe("elapsedMs");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperFallbackMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.MapperFallback),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("mapperType");
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[2].Name.ShouldBe("reason");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperInitializationFailedMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.MapperInitializationFailed),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperCreationFailedMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.MapperCreationFailed),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("mapperType");
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[2].Name.ShouldBe("error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RowMappingFailedMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.RowMappingFailed),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperTypeRegisteredMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.MapperTypeRegistered),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("mapperType");
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[2].Name.ShouldBe("factoryType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ProviderInitializedMethodExists()
    {
        // Act
        var method = typeof(EtlRowMapperLog).GetMethod(
            nameof(EtlRowMapperLog.ProviderInitialized),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericMessage));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(ILogger));
        parameters[1].ParameterType.ShouldBe(typeof(int));
        parameters[1].Name.ShouldBe("mapperCount");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperInitializingReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.MapperInitializing(_logger.Object, "TestMapper", 5);

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperCompiledReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.MapperCompiled(_logger.Object, "TestMapper", 123.45);

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperFallbackReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.MapperFallback(_logger.Object, "TestMapper", "Test reason");

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperInitializationFailedReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.MapperInitializationFailed(_logger.Object, "Test error");

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperCreationFailedReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.MapperCreationFailed(_logger.Object, "TestMapper", "Test error");

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RowMappingFailedReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.RowMappingFailed(_logger.Object, "Test error");

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapperTypeRegisteredReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.MapperTypeRegistered(_logger.Object, "TestMapper", "TestFactory");

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ProviderInitializedReturnsMessage()
    {
        // Act
        var message = EtlRowMapperLog.ProviderInitialized(_logger.Object, 3);

        // Assert
        message.ShouldNotBeNull();
        message.ShouldBeAssignableTo<IGenericMessage>();
    }
}
