using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Abstractions.Tests.Translators;

/// <summary>
/// Tests for DataCommandTranslators runtime registration.
/// </summary>
public sealed class DataCommandTranslatorsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterAddsTranslatorType()
    {
        // Arrange
        var translatorName = $"TestTranslator_{Guid.NewGuid()}";

        // Act
        DataCommandTranslators.Register(translatorName, typeof(TestTranslator));
        var result = DataCommandTranslators.GetTranslatorType(translatorName);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(typeof(TestTranslator));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            DataCommandTranslators.Register(null!, typeof(TestTranslator)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterThrowsWhenNameIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            DataCommandTranslators.Register(string.Empty, typeof(TestTranslator)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterThrowsWhenNameIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            DataCommandTranslators.Register("   ", typeof(TestTranslator)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterThrowsWhenTypeIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            DataCommandTranslators.Register("TestTranslator", null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterThrowsWhenTypeDoesNotImplementInterface()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            DataCommandTranslators.Register("InvalidType", typeof(string)));

        exception.Message.ShouldContain("must implement IDataCommandTranslator");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetTranslatorTypeReturnsNullForUnknownTranslator()
    {
        // Act
        var result = DataCommandTranslators.GetTranslatorType("UnknownTranslator_" + Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetTranslatorTypeReturnsNullForNullName()
    {
        // Act
        var result = DataCommandTranslators.GetTranslatorType(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetTranslatorTypeReturnsNullForEmptyName()
    {
        // Act
        var result = DataCommandTranslators.GetTranslatorType(string.Empty);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetTranslatorTypeReturnsNullForWhitespaceName()
    {
        // Act
        var result = DataCommandTranslators.GetTranslatorType("   ");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetAllTranslatorTypesIncludesRegisteredTranslators()
    {
        // Arrange
        var translatorName = $"TestTranslator_{Guid.NewGuid()}";
        DataCommandTranslators.Register(translatorName, typeof(TestTranslator));

        // Act
        var allTypes = DataCommandTranslators.GetAllTranslatorTypes().ToList();

        // Assert
        allTypes.ShouldContain(typeof(TestTranslator));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetTranslatorNamesIncludesRegisteredTranslators()
    {
        // Arrange
        var translatorName = $"TestTranslator_{Guid.NewGuid()}";
        DataCommandTranslators.Register(translatorName, typeof(TestTranslator));

        // Act
        var allNames = DataCommandTranslators.GetTranslatorNames().ToList();

        // Assert
        allNames.ShouldContain(translatorName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExistsReturnsTrueForRegisteredTranslator()
    {
        // Arrange
        var translatorName = $"TestTranslator_{Guid.NewGuid()}";
        DataCommandTranslators.Register(translatorName, typeof(TestTranslator));

        // Act
        var exists = DataCommandTranslators.Exists(translatorName);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExistsReturnsFalseForUnknownTranslator()
    {
        // Act
        var exists = DataCommandTranslators.Exists("UnknownTranslator_" + Guid.NewGuid());

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SetLoggerAcceptsLogger()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();

        // Act - Should not throw
        DataCommandTranslators.SetLogger(mockLogger.Object);

        // Assert - Implicit pass
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SetLoggerAcceptsNull()
    {
        // Act - Should not throw (falls back to NullLogger)
        DataCommandTranslators.SetLogger(null!);

        // Assert - Implicit pass
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterOverwritesPreviousRegistration()
    {
        // Arrange
        var translatorName = $"TestTranslator_{Guid.NewGuid()}";
        DataCommandTranslators.Register(translatorName, typeof(TestTranslator));

        // Act - Register again with different type
        DataCommandTranslators.Register(translatorName, typeof(AnotherTestTranslator));
        var result = DataCommandTranslators.GetTranslatorType(translatorName);

        // Assert - Latest registration wins
        result.ShouldBe(typeof(AnotherTestTranslator));
    }

    // Test doubles
    private sealed class TestTranslator : DataCommandTranslatorBase<string>
    {
        public TestTranslator() : base("TestTranslator", "Test") { }

        public override Task<IGenericResult<string>> Translate(
            IDataCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult<string>>(
                GenericResult<string>.Success("SELECT * FROM Test"));
        }
    }

    private sealed class AnotherTestTranslator : DataCommandTranslatorBase<string>
    {
        public AnotherTestTranslator() : base("AnotherTestTranslator", "Test") { }

        public override Task<IGenericResult<string>> Translate(
            IDataCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult<string>>(
                GenericResult<string>.Success("SELECT * FROM Another"));
        }
    }
}
