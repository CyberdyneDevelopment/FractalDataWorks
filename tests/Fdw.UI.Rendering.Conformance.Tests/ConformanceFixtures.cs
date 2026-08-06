using Fdw.UI.Components.Models;
using Fdw.UI.Components.Pages;

namespace Fdw.UI.Rendering.Conformance.Tests;

/// <summary>
/// Canonical page models shared across renderer conformance tests. The SAME model instances
/// (well, equivalently-built ones) are fed to every renderer under test so a passing test
/// demonstrates identical behavioral outcomes across backends — the actual claim FDW-546 makes.
/// </summary>
internal static class ConformanceFixtures
{
    /// <summary>
    /// A page with one valid, populated required field — Save should succeed.
    /// </summary>
    internal static PageModel CreateSavablePage()
    {
        var name = new TextInputModel
        {
            Id = "name",
            Label = "Name",
            IsRequired = true,
            Value = "Acme Widgets",
        };

        var page = new PageModel
        {
            Id = "conformance-page",
            Title = "Conformance Page",
            Mode = new EditPageMode(),
        };
        page.AddSection(SectionModel.SingleColumn("main", "Details", name));
        return page;
    }

    /// <summary>
    /// A page with a required field left empty — Save should fail validation.
    /// </summary>
    internal static PageModel CreateInvalidPage()
    {
        var name = new TextInputModel
        {
            Id = "name",
            Label = "Name",
            IsRequired = true,
            Value = null,
        };

        var page = new PageModel
        {
            Id = "conformance-page-invalid",
            Title = "Conformance Page",
            Mode = new EditPageMode(),
        };
        page.AddSection(SectionModel.SingleColumn("main", "Details", name));
        return page;
    }

    /// <summary>
    /// A single canonical text input component for Render()-level (non-page) tests.
    /// </summary>
    internal static TextInputModel CreateTextInput() => new()
    {
        Id = "conformance-input",
        Label = "Conformance Input",
        Value = "hello",
    };
}
