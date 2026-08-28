using System.Linq;
using Fdw.UI.Abstractions.Components;
using Fdw.Validation.Abstractions;
using Fdw.UI.Components.Models;
using Fdw.UI.Rendering.Blazor.Components;
using Fdw.UI.Rendering.Blazor.Dispatch;
using Shouldly;
using Xunit;

namespace Fdw.UI.Rendering.Conformance.Tests;

/// <summary>
/// Covers the component dispatch registry that replaced the closed <c>switch (Model)</c> in
/// FdwComponent.
/// </summary>
/// <remarks>
/// The behaviour under test is extensibility: before this, a component model declared outside
/// Fdw.UI.Rendering.Blazor could not be painted at all, because both the dispatcher's switch and
/// the renderer's IsSupported gate enumerated a fixed set of concrete types.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "Rendering")]
public class OpenComponentDispatchTests
{
    [Fact]
    public void EveryBuiltInRendererIsRegistered()
    {
        var names = BlazorComponentRenderers.All().Select(r => r.Name).ToList();

        names.ShouldContain("TextInput");
        names.ShouldContain("Checkbox");
        names.ShouldContain("DatePicker");
        names.ShouldContain("TypeCollectionSelect");
        names.ShouldContain("NumericInput");
        names.ShouldContain("Select");
        names.ShouldContain("MultiSelect");
    }

    [Fact]
    public void TextInputModelResolvesToTheTextInputComponent()
    {
        var renderer = BlazorComponentRendererExtensions.ResolveFor(new TextInputModel());

        renderer.ShouldNotBeNull();
        renderer!.ComponentType.ShouldBe(typeof(FdwTextInput));
    }

    [Fact]
    public void CheckboxModelResolvesToTheCheckboxComponent()
    {
        var renderer = BlazorComponentRendererExtensions.ResolveFor(new CheckboxModel());

        renderer.ShouldNotBeNull();
        renderer!.ComponentType.ShouldBe(typeof(FdwCheckbox));
    }

    /// <summary>
    /// The precedence contract: a concrete mapping must beat an interface-level one that would also
    /// claim the model. The previous switch encoded this in case order, where it was invisible.
    /// </summary>
    [Fact]
    public void ConcreteMappingsOutrankInterfaceLevelFallbacks()
    {
        var ordered = BlazorComponentRendererExtensions.InDispatchOrder();

        var textInput = ordered.First(r => r.Name == "TextInput");
        var numeric = ordered.First(r => r.Name == "NumericInput");
        var select = ordered.First(r => r.Name == "Select");

        textInput.Precedence.ShouldBeLessThan(numeric.Precedence);
        numeric.Precedence.ShouldBeLessThan(select.Precedence);
        ordered.Select(r => r.Precedence).ShouldBe(ordered.Select(r => r.Precedence).OrderBy(p => p));
    }

    [Fact]
    public void AModelNoRegisteredRendererClaimsResolvesToNull()
    {
        BlazorComponentRendererExtensions.ResolveFor(new UnmappedComponentModel()).ShouldBeNull();
    }

    [Fact]
    public void ResolveForNullModelReturnsNull()
    {
        BlazorComponentRendererExtensions.ResolveFor(null!).ShouldBeNull();
    }

    /// <summary>A component model no built-in renderer claims — stands in for a downstream model.</summary>
    private sealed class UnmappedComponentModel : IComponentModel
    {
        public string Id => "unmapped";

        public string? Label => null;

        public string? HelpText => null;

        public bool IsRequired => false;

        public bool IsReadOnly => false;

        public bool IsVisible => true;

        public int Order => 0;

        public ValidationResult Validate() => ValidationResult.Success();
    }
}
