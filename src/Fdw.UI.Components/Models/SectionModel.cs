using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Concrete implementation of a section model.
/// </summary>
public sealed class SectionModel : ISectionModel
{
    private readonly List<ColumnModel> _columns = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string? Title { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public bool IsCollapsible { get; set; }

    /// <inheritdoc />
    public bool IsExpanded { get; set; } = true;

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public IReadOnlyList<IColumnModel> Columns => _columns.AsReadOnly();

    /// <inheritdoc />
    public IEnumerable<IComponentModel> AllComponents =>
        _columns.SelectMany(c => c.Components);

    /// <summary>
    /// Adds a column to the section.
    /// </summary>
    /// <param name="column">The column to add.</param>
    public void AddColumn(ColumnModel column)
    {
        _columns.Add(column);
    }

    /// <summary>
    /// Adds multiple columns to the section.
    /// </summary>
    /// <param name="columns">The columns to add.</param>
    public void AddColumns(IEnumerable<ColumnModel> columns)
    {
        _columns.AddRange(columns);
    }

    /// <summary>
    /// Creates a section with a single full-width column containing the specified components.
    /// </summary>
    /// <param name="id">The section ID.</param>
    /// <param name="title">The section title.</param>
    /// <param name="components">The components to include.</param>
    /// <returns>A configured section model.</returns>
    public static SectionModel SingleColumn(string id, string? title, params IComponentModel[] components)
    {
        var section = new SectionModel { Id = id, Title = title };
        var column = new ColumnModel { Id = $"{id}-col", Width = 12 };
        column.AddComponents(components);
        section.AddColumn(column);
        return section;
    }

    /// <summary>
    /// Creates a section with two equal-width columns.
    /// </summary>
    /// <param name="id">The section ID.</param>
    /// <param name="title">The section title.</param>
    /// <param name="leftComponents">Components for the left column.</param>
    /// <param name="rightComponents">Components for the right column.</param>
    /// <returns>A configured section model.</returns>
    public static SectionModel TwoColumns(
        string id,
        string? title,
        IEnumerable<IComponentModel> leftComponents,
        IEnumerable<IComponentModel> rightComponents)
    {
        var section = new SectionModel { Id = id, Title = title };

        var leftColumn = new ColumnModel { Id = $"{id}-left", Width = 6 };
        leftColumn.AddComponents(leftComponents);

        var rightColumn = new ColumnModel { Id = $"{id}-right", Width = 6 };
        rightColumn.AddComponents(rightComponents);

        section.AddColumn(leftColumn);
        section.AddColumn(rightColumn);
        return section;
    }
}