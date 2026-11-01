using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Abstractions.Tests.Translators;

/// <summary>
/// Tests for DataCommandTranslatorBase.
/// </summary>
public sealed class DataCommandTranslatorBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var translator = new TestTranslator("TestTranslator", "TestDomain");

        // Assert
        translator.Name.ShouldBe("TestTranslator");
        translator.DomainName.ShouldBe("TestDomain");
        translator.Category.ShouldBe("TestDomain");
        translator.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IdComesFromTheTypeNotTheName()
    {
        // Arrange & Act
        var translator1 = new TestTranslator("Translator1", "Domain");
        var translator2 = new TestTranslator("Translator2", "Domain");

        // Assert - The id is derived from the translator's type, so two names on one type share it
        translator1.Id.ShouldBe(translator2.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestTranslator(null!, "Domain"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestTranslator(string.Empty, "Domain"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryMatchesDomainName()
    {
        // Arrange & Act
        var translator = new TestTranslator("TestTranslator", "CustomDomain");

        // Assert
        translator.Category.ShouldBe("CustomDomain");
        translator.Category.ShouldBe(translator.DomainName);
    }

    // Test double
    private sealed class TestTranslator : DataCommandTranslatorBase<string>
    {
        public TestTranslator(string name, string domainName)
            : base(name, domainName)
        {
        }

        public override Task<IGenericResult<string>> Translate(
            IDataCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IGenericResult<string>>(
                GenericResult<string>.Success("SELECT * FROM Test"));
        }
    }
}
