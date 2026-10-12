using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Components.Pages;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Concrete implementation of a page model.
/// </summary>
public sealed class PageModel : IPageModel
{
    private readonly List<SectionModel> _sections = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ISectionModel> Sections => _sections.AsReadOnly();

    /// <inheritdoc />
    public IPageMode Mode { get; set; } = PageModes.View;

    /// <inheritdoc />
    public bool HasChanges { get; set; }

    /// <summary>
    /// Adds a section to the page.
    /// </summary>
    /// <param name="section">The section to add.</param>
    public void AddSection(SectionModel section)
    {
        _sections.Add(section);
    }

    /// <summary>
    /// Adds multiple sections to the page.
    /// </summary>
    /// <param name="sections">The sections to add.</param>
    public void AddSections(IEnumerable<SectionModel> sections)
    {
        _sections.AddRange(sections);
    }

    /// <summary>
    /// Gets a section by ID.
    /// </summary>
    /// <param name="id">The section ID.</param>
    /// <returns>The section, or null if not found.</returns>
    public SectionModel? GetSection(string id)
    {
        return _sections.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var results = _sections
            .SelectMany(s => s.AllComponents)
            .Select(c => c.Validate())
            .ToList();

        return ValidationResult.Combine(results.ToArray());
    }
}