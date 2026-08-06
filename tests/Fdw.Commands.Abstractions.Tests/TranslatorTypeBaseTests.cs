using Fdw.Commands.Abstractions;
using Fdw.Messages;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Abstractions.Tests;

public sealed class TranslatorTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 2 && f.Name == "REST");
        var capabilities = TranslationCapabilities.Full;

        // Act
        var translator = new TestTranslatorType(
            id: 1,
            name: "SqlToRest",
            description: "SQL to REST translator",
            sourceFormat: sourceFormat,
            targetFormat: targetFormat,
            capabilities: capabilities,
            priority: 75);

        // Assert
        translator.Id.ShouldBe(1);
        translator.Name.ShouldBe("SqlToRest");
        // Description is passed as 'category' to TypeOptionBase, not 'description'
        // TypeOptionBase auto-generates description as "Type option: {name}"
        translator.Description.ShouldBe("Type option: SqlToRest");
        translator.SourceFormat.ShouldBe(sourceFormat);
        translator.TargetFormat.ShouldBe(targetFormat);
        // Compare capabilities properties since Full creates new instances
        translator.Capabilities.SupportsProjection.ShouldBe(capabilities.SupportsProjection);
        translator.Capabilities.MaxComplexityLevel.ShouldBe(capabilities.MaxComplexityLevel);
        translator.Priority.ShouldBe(75);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorUsesDefaultPriority()
    {
        // Arrange
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 2 && f.Name == "REST");
        var capabilities = TranslationCapabilities.Basic;

        // Act
        var translator = new TestTranslatorType(
            id: 2,
            name: "DefaultPriority",
            description: "Translator with default priority",
            sourceFormat: sourceFormat,
            targetFormat: targetFormat,
            capabilities: capabilities);

        // Assert
        translator.Priority.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesBasicCapabilities()
    {
        // Arrange
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 2 && f.Name == "REST");
        var basicCaps = TranslationCapabilities.Basic;

        // Act
        var translator = new TestTranslatorType(
            id: 3,
            name: "BasicTranslator",
            description: "Translator with basic capabilities",
            sourceFormat: sourceFormat,
            targetFormat: targetFormat,
            capabilities: basicCaps,
            priority: 10);

        // Assert - compare properties since Basic creates new instances
        translator.Capabilities.SupportsProjection.ShouldBeTrue();
        translator.Capabilities.SupportsFiltering.ShouldBeTrue();
        translator.Capabilities.SupportsOrdering.ShouldBeTrue();
        translator.Capabilities.SupportsPaging.ShouldBeFalse();
        translator.Capabilities.SupportsJoins.ShouldBeFalse();
        translator.Capabilities.MaxComplexityLevel.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesZeroPriority()
    {
        // Arrange
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 2 && f.Name == "REST");

        // Act
        var translator = new TestTranslatorType(
            id: 4,
            name: "LowPriority",
            description: "Translator with zero priority",
            sourceFormat: sourceFormat,
            targetFormat: targetFormat,
            capabilities: TranslationCapabilities.Full,
            priority: 0);

        // Assert
        translator.Priority.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesMaxPriority()
    {
        // Arrange
        var sourceFormat = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");
        var targetFormat = Mock.Of<IDataFormat>(f => f.Id == 2 && f.Name == "REST");

        // Act
        var translator = new TestTranslatorType(
            id: 5,
            name: "HighPriority",
            description: "Translator with high priority",
            sourceFormat: sourceFormat,
            targetFormat: targetFormat,
            capabilities: TranslationCapabilities.Full,
            priority: 100);

        // Assert
        translator.Priority.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesSameSourceAndTargetFormat()
    {
        // Arrange
        var format = Mock.Of<IDataFormat>(f => f.Id == 1 && f.Name == "SQL");

        // Act
        var translator = new TestTranslatorType(
            id: 6,
            name: "Passthrough",
            description: "Passthrough translator",
            sourceFormat: format,
            targetFormat: format,
            capabilities: TranslationCapabilities.Full);

        // Assert
        translator.SourceFormat.ShouldBe(format);
        translator.TargetFormat.ShouldBe(format);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestTranslatorType : TranslatorTypeBase
    {
        public TestTranslatorType(
            int id,
            string name,
            string description,
            IDataFormat sourceFormat,
            IDataFormat targetFormat,
            TranslationCapabilities capabilities,
            int priority = 50)
            : base(id, name, description, sourceFormat, targetFormat, capabilities, priority)
        {
        }

        public override IGenericResult<IGenericCommandTranslator> CreateTranslator(IServiceProvider services)
            => GenericResult<IGenericCommandTranslator>.Failure(new GenericMessage("Not implemented"));
    }

}
