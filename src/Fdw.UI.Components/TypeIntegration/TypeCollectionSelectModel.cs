using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.TypeIntegration;

/// <summary>
/// Select model that integrates with TypeCollections for strongly-typed dropdown selection.
/// </summary>
/// <remarks>
/// <para>
/// This model wraps a TypeCollection to provide a dropdown of all registered options.
/// The value is stored as an integer ID, which can be persisted to the database.
/// </para>
/// <para>
/// Usage:
/// <code>
/// // Create a select for AuthenticationTypes TypeCollection
/// var select = new TypeCollectionSelectModel&lt;int&gt;
/// {
///     Id = "authType",
///     Label = "Authentication Type",
///     TypeCollectionName = "AuthenticationTypes"
/// };
/// select.LoadOptions(AuthenticationTypes.All().Select(t => (t.Id, t.Name)));
/// </code>
/// </para>
/// </remarks>
public sealed class TypeCollectionSelectModel : ISelectableComponentModel<int>
{
    private readonly List<SelectOption<int>> _options = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public string? HelpText { get; set; }

    /// <inheritdoc />
    public bool IsRequired { get; set; }

    /// <inheritdoc />
    public bool IsReadOnly { get; set; }

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the selected type ID.
    /// </summary>
    public int? Value { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public int? DefaultValue { get; set; }

    /// <inheritdoc />
    object? IInputComponentModel.ValueAsObject
    {
        get => Value;
        set => Value = value as int?;
    }

    /// <inheritdoc />
    object? IInputComponentModel.DefaultValueAsObject => DefaultValue;

    /// <inheritdoc />
    public Type ValueType => typeof(int);

    /// <inheritdoc />
    public int OptionsCount => _options.Count;

    /// <inheritdoc />
    public IReadOnlyList<SelectOption<int>> Options => _options.AsReadOnly();

    /// <inheritdoc />
    public Func<int, string>? DisplayConverter { get; set; }

    /// <summary>
    /// Gets or sets the custom validator function.
    /// </summary>
    public Func<int?, ValidationResult>? CustomValidator { get; set; }

    /// <summary>
    /// Gets or sets the name of the TypeCollection this select represents.
    /// </summary>
    /// <remarks>
    /// Used for documentation and debugging. The actual options are loaded via LoadOptions.
    /// </remarks>
    public string? TypeCollectionName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow no selection (null).
    /// </summary>
    public bool AllowEmpty { get; set; }

    /// <summary>
    /// Gets or sets the text for the empty option.
    /// </summary>
    public string EmptyOptionText { get; set; } = "(None)";

    /// <summary>
    /// Loads options from a TypeCollection.
    /// </summary>
    /// <param name="options">Tuples of (Id, Name) from the TypeCollection.</param>
    public void LoadOptions(IEnumerable<(int Id, string Name)> options)
    {
        _options.Clear();
        foreach (var (id, name) in options)
        {
            _options.Add(new SelectOption<int>(id, name));
        }
    }

    /// <summary>
    /// Adds an option.
    /// </summary>
    /// <param name="id">The type ID.</param>
    /// <param name="name">The display name.</param>
    /// <param name="isDisabled">Whether the option is disabled.</param>
    public void AddOption(int id, string name, bool isDisabled = false)
    {
        _options.Add(new SelectOption<int>(id, name, isDisabled));
    }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        if (IsRequired && !Value.HasValue)
        {
            return ValidationResult.Error($"{Label ?? Id} is required.");
        }

        if (Value.HasValue && !_options.Any(o => o.Value == Value.Value))
        {
            return ValidationResult.Error($"{Label ?? Id} has an invalid selection.");
        }

        if (CustomValidator != null)
        {
            return CustomValidator(Value);
        }

        return ValidationResult.Success();
    }
}
