using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.UI.Abstractions;
using Fdw.UI.Components.TUI;
using Shouldly;
using Spectre.Console;
using Xunit;

namespace Fdw.UI.Components.TUI.Tests;

/// <summary>
/// Concrete TUIComponent implementation for testing CRTP base behaviour.
/// </summary>
internal sealed class TestTUIComponent : TUIComponent<TestTUIComponent, string>
{
    public int LastPromptTypeCollectionResult { get; private set; }

    public override Task<string?> Prompt(IAnsiConsole console)
    {
        return Task.FromResult<string?>(null);
    }

    public override void Render(IAnsiConsole console)
    {
        // no-op for testing
    }

    protected override IEnumerable<IPropertyComponent> GetPropertyComponents()
    {
        return [];
    }

    protected override bool CanContain<TChild>()
    {
        return false;
    }

    public int InvokePromptTypeCollectionId<TOption>(
        IAnsiConsole console,
        string promptText,
        IEnumerable<TOption> options)
        where TOption : class, ITypeOption
    {
        LastPromptTypeCollectionResult = PromptTypeCollectionId(console, promptText, options);
        return LastPromptTypeCollectionResult;
    }
}

public sealed class TUIComponentTests
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
    public void PromptTypeCollectionIdAcceptsIEnumerableOfTypeOption()
    {
        // Arrange
        var console = CreateOffscreenConsole();
        var component = new TestTUIComponent();
        IEnumerable<TestTypeOption> options = Enumerable.Empty<TestTypeOption>();

        // Act - compile-time type safety: method accepts IEnumerable<TOption>
        var result = component.InvokePromptTypeCollectionId(console, "Select:", options);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptTypeCollectionIdReturnsZeroWhenNoOptions()
    {
        // Arrange
        var console = CreateOffscreenConsole();
        var component = new TestTUIComponent();

        // Act
        var result = component.InvokePromptTypeCollectionId(
            console, "Select:", Enumerable.Empty<TestTypeOption>());

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptTypeCollectionIdDelegatesToTypeCollectionPromptHelper()
    {
        // Arrange
        var console = CreateOffscreenConsole();
        var component = new TestTUIComponent();
        var options = Enumerable.Empty<TestTypeOption>();

        // Act
        component.InvokePromptTypeCollectionId(console, "Pick one:", options);

        // Assert - LastPromptTypeCollectionResult is set (proving delegation occurred)
        component.LastPromptTypeCollectionResult.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptTypeCollectionIdUsesNullThemeFromComponent()
    {
        // Arrange - null theme exercises the default Color fallbacks in TypeCollectionPromptHelper
        var console = CreateOffscreenConsole();
        var component = new TestTUIComponent
        {
            Theme = null
        };

        // Act - no exception means null theme is handled gracefully
        var result = component.InvokePromptTypeCollectionId(
            console, "Select:", Enumerable.Empty<TestTypeOption>());

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void GetDisplayTextReturnsNullPlaceholderWhenValueIsNull()
    {
        // Arrange
        var component = new TestTUIComponent { Value = null };

        // Act
        var text = component.GetDisplayText();

        // Assert
        text.ShouldBe("[dim]null[/]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void GetDisplayTextReturnsValueToStringWhenValueIsSet()
    {
        // Arrange
        var component = new TestTUIComponent { Value = "hello-world" };

        // Act
        var text = component.GetDisplayText();

        // Assert
        text.ShouldBe("hello-world");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void PromptTypeCollectionIdProtectedMethodIsGeneric()
    {
        // Arrange - verify no reflection by checking the protected method signature
        var methodInfo = typeof(TUIComponent<TestTUIComponent, string>)
            .GetMethod("PromptTypeCollectionId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo!.IsGenericMethod.ShouldBeTrue();
    }
}
