using Fdw.Commands.Abstractions;

namespace Fdw.Commands.Abstractions.Tests;

public sealed class CommandCategoryBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var category = new TestCommandCategory(
            name: "Query",
            requiresTransaction: false,
            supportsStreaming: true,
            isCacheable: true,
            isMutation: false,
            executionPriority: 100);
        category.Name.ShouldBe("Query");
        category.RequiresTransaction.ShouldBeFalse();
        category.SupportsStreaming.ShouldBeTrue();
        category.IsCacheable.ShouldBeTrue();
        category.IsMutation.ShouldBeFalse();
        category.ExecutionPriority.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorUsesDefaultExecutionPriority()
    {
        // Arrange & Act
        var category = new TestCommandCategory(
            name: "Mutation",
            requiresTransaction: true,
            supportsStreaming: false,
            isCacheable: false,
            isMutation: true);

        // Assert
        category.ExecutionPriority.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesAllFlagsTrue()
    {
        // Arrange & Act
        var category = new TestCommandCategory(
            name: "Full",
            requiresTransaction: true,
            supportsStreaming: true,
            isCacheable: true,
            isMutation: true,
            executionPriority: 200);

        // Assert
        category.RequiresTransaction.ShouldBeTrue();
        category.SupportsStreaming.ShouldBeTrue();
        category.IsCacheable.ShouldBeTrue();
        category.IsMutation.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesAllFlagsFalse()
    {
        // Arrange & Act
        var category = new TestCommandCategory(
            name: "None",
            requiresTransaction: false,
            supportsStreaming: false,
            isCacheable: false,
            isMutation: false,
            executionPriority: 10);

        // Assert
        category.RequiresTransaction.ShouldBeFalse();
        category.SupportsStreaming.ShouldBeFalse();
        category.IsCacheable.ShouldBeFalse();
        category.IsMutation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesZeroPriority()
    {
        // Arrange & Act
        var category = new TestCommandCategory(
            name: "LowPriority",
            requiresTransaction: false,
            supportsStreaming: false,
            isCacheable: false,
            isMutation: false,
            executionPriority: 0);

        // Assert
        category.ExecutionPriority.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorHandlesMaxPriority()
    {
        // Arrange & Act
        var category = new TestCommandCategory(
            name: "HighPriority",
            requiresTransaction: false,
            supportsStreaming: false,
            isCacheable: false,
            isMutation: false,
            executionPriority: int.MaxValue);

        // Assert
        category.ExecutionPriority.ShouldBe(int.MaxValue);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestCommandCategory : CommandCategoryBase
    {
        public TestCommandCategory(
            string name,
            bool requiresTransaction,
            bool supportsStreaming,
            bool isCacheable,
            bool isMutation,
            int executionPriority = 50)
            : base(name, requiresTransaction, supportsStreaming, isCacheable, isMutation, executionPriority)
        {
        }
    }
}
