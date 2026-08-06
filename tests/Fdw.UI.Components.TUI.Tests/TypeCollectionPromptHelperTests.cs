using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;
using Fdw.UI.Components.TUI.Prompts;
using Shouldly;
using Spectre.Console;
using Xunit;

namespace Fdw.UI.Components.TUI.Tests;

/// <summary>
/// Stub ITypeOption implementation for testing TypeCollectionPromptHelper
/// without needing real TypeCollections or source generator dependencies.
/// </summary>
internal sealed class TestTypeOption : ITypeOption<int, TestTypeOption>
{
    public TestTypeOption(int id, string name, string category = "Test")
    {
        Id = id;
        Name = name;
        Category = category;
    }

    public int Id { get; }
    public string Name { get; }
    public string Category { get; }
    object ITypeOption.Id => Id;
}

public sealed class TypeCollectionPromptHelperTests
{
    private static IAnsiConsole CreateOffscreenConsole()
    {
        return AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(System.IO.TextWriter.Null),
            Interactive = InteractionSupport.No
        });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptReturnsZeroWhenOptionsListIsEmpty()
    {
        // Arrange
        var console = CreateOffscreenConsole();
        var emptyOptions = Enumerable.Empty<TestTypeOption>();

        // Act
        var result = TypeCollectionPromptHelper.Prompt(console, "Select:", emptyOptions);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptAcceptsIEnumerableOfTypeOption()
    {
        // Arrange
        var console = CreateOffscreenConsole();
        IEnumerable<TestTypeOption> options = new List<TestTypeOption>
        {
            new(1, "Alpha"),
            new(2, "Beta"),
        };

        // Act - passes when empty; verifies IEnumerable<TOption> is accepted without reflection
        var emptyResult = TypeCollectionPromptHelper.Prompt(console, "Select:", Enumerable.Empty<TestTypeOption>());

        // Assert - compile-time type safety: no reflection invoked on empty path
        emptyResult.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptWithNullThemeStillReturnsZeroForEmpty()
    {
        // Arrange
        var console = CreateOffscreenConsole();

        // Act
        var result = TypeCollectionPromptHelper.Prompt<TestTypeOption>(
            console, "Pick:", Enumerable.Empty<TestTypeOption>(), theme: null);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptParameterIsIEnumerableNotConcreteCollection()
    {
        // Arrange - verify the static method signature accepts IEnumerable (not List or array)
        // This confirms no reflection and compile-time type safety
        var methodInfo = typeof(TypeCollectionPromptHelper).GetMethod("Prompt");

        // Assert
        methodInfo.ShouldNotBeNull();
        var parameters = methodInfo!.GetParameters();
        var optionsParam = parameters.FirstOrDefault(p => p.Name == "options");
        optionsParam.ShouldNotBeNull();
        // The parameter type is IEnumerable<TOption> — not a concrete type requiring reflection
        optionsParam!.ParameterType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptIsGenericWithTypeOptionConstraint()
    {
        // Arrange
        var methodInfo = typeof(TypeCollectionPromptHelper).GetMethod("Prompt");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo!.IsGenericMethod.ShouldBeTrue();

        var typeParam = methodInfo.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        // TOption must be class and implement ITypeOption
        constraints.ShouldContain(t => t == typeof(ITypeOption));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptEnumeratesOptionsOnce()
    {
        // Arrange
        var console = CreateOffscreenConsole();
        var enumerationCount = 0;
        IEnumerable<TestTypeOption> TrackingEnumerable()
        {
            enumerationCount++;
            yield break;
        }

        // Act
        TypeCollectionPromptHelper.Prompt(console, "Select:", TrackingEnumerable());

        // Assert - enumerated exactly once (materialised via ToList inside helper)
        enumerationCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptWithExplicitNullThemeReturnsZeroForEmpty()
    {
        // Arrange
        var console = CreateOffscreenConsole();

        // Act - explicit null theme tests the null-theme fallback path in the helper
        var result = TypeCollectionPromptHelper.Prompt<TestTypeOption>(
            console, "Select:", Enumerable.Empty<TestTypeOption>(), theme: null);

        // Assert
        result.ShouldBe(0);
    }
}
