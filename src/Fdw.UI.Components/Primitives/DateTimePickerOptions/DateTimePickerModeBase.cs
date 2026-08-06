using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Components.Primitives.DateTimePickerOptions;

/// <summary>
/// Base class for DateTimePicker component modes.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class DateTimePickerModeBase : TypeOptionBase<int, DateTimePickerModeBase>, IDateTimePickerMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimePickerModeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this picker mode.</param>
    /// <param name="name">The name of this picker mode.</param>
    /// <param name="includesDate">Whether this mode includes date selection.</param>
    /// <param name="includesTime">Whether this mode includes time selection.</param>
    /// <param name="htmlInputType">The HTML5 input type.</param>
    /// <param name="displayFormat">The format string for displaying the value.</param>
    protected DateTimePickerModeBase(int id, string name, bool includesDate, bool includesTime, string htmlInputType, string displayFormat)
        : base(id, name)
    {
        IncludesDate = includesDate;
        IncludesTime = includesTime;
        HtmlInputType = htmlInputType;
        DisplayFormat = displayFormat;
    }

    /// <inheritdoc />
    public bool IncludesDate { get; }

    /// <inheritdoc />
    public bool IncludesTime { get; }

    /// <inheritdoc />
    public string HtmlInputType { get; }

    /// <inheritdoc />
    public string DisplayFormat { get; }
}
