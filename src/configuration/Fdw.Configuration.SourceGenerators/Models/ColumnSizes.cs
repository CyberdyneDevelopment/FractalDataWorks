using System;
using System.Globalization;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Configuration.SourceGenerators.Models;

/// <summary>
/// The column sizes the build states for a configuration field that states none of its own (FDW-7107).
/// </summary>
/// <remarks>
/// <para>
/// A string field without <c>[MaxLength]</c> and a decimal field without <c>[Precision]</c> have no size of their own. The size is the
/// build's, read from three MSBuild properties that the generator package's props declares (<see cref="StringMaxLengthProperty"/>,
/// <see cref="DecimalPrecisionProperty"/>, <see cref="DecimalScaleProperty"/>). A project changes one by setting the property itself. The
/// generator holds no number of its own.
/// </para>
/// <para>
/// A property that is absent, empty or not a whole number in its range reads as not stated, and a field that needs it is reported
/// (CFG004), never sized on a guess.
/// </para>
/// </remarks>
public sealed class ColumnSizes : IEquatable<ColumnSizes>
{
    /// <summary>The MSBuild property that states the length of a string column whose field states none.</summary>
    public const string StringMaxLengthProperty = "FDW_DdlStringMaxLength";

    /// <summary>The MSBuild property that states the precision of a decimal column whose field states none.</summary>
    public const string DecimalPrecisionProperty = "FDW_DdlDecimalPrecision";

    /// <summary>The MSBuild property that states the scale of a decimal column whose field states none.</summary>
    public const string DecimalScaleProperty = "FDW_DdlDecimalScale";

    /// <summary>Initializes a new instance of the <see cref="ColumnSizes"/> class.</summary>
    /// <param name="stringMaxLength">The length of a string column, or <see langword="null"/> when the build states none.</param>
    /// <param name="decimalPrecision">The precision of a decimal column, or <see langword="null"/> when the build states none.</param>
    /// <param name="decimalScale">The scale of a decimal column, or <see langword="null"/> when the build states none.</param>
    public ColumnSizes(int? stringMaxLength, int? decimalPrecision, int? decimalScale)
    {
        StringMaxLength = stringMaxLength;
        DecimalPrecision = decimalPrecision;
        DecimalScale = decimalScale;
    }

    /// <summary>Gets the length of a string column whose field states none, or <see langword="null"/> when the build states none.</summary>
    public int? StringMaxLength { get; }

    /// <summary>Gets the precision of a decimal column whose field states none, or <see langword="null"/> when the build states none.</summary>
    public int? DecimalPrecision { get; }

    /// <summary>Gets the scale of a decimal column whose field states none, or <see langword="null"/> when the build states none.</summary>
    public int? DecimalScale { get; }

    /// <summary>Reads the sizes the build states.</summary>
    /// <param name="options">The compilation's global analyzer options, which carry the MSBuild properties as <c>build_property.*</c>.</param>
    /// <returns>The sizes; each one the build does not state, or states as anything but a whole number in its range, is <see langword="null"/>.</returns>
    public static ColumnSizes From(AnalyzerConfigOptions options)
        => new(Read(options, StringMaxLengthProperty, 1), Read(options, DecimalPrecisionProperty, 1), Read(options, DecimalScaleProperty, 0));

    /// <inheritdoc />
    public bool Equals(ColumnSizes? other)
        => other is not null
            && StringMaxLength == other.StringMaxLength
            && DecimalPrecision == other.DecimalPrecision
            && DecimalScale == other.DecimalScale;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ColumnSizes);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = StringMaxLength.GetHashCode();
            hash = (hash * 397) ^ DecimalPrecision.GetHashCode();
            return (hash * 397) ^ DecimalScale.GetHashCode();
        }
    }

    // Why the lowest is passed: a length and a precision are at least one, and a scale may be zero.
    private static int? Read(AnalyzerConfigOptions options, string property, int lowest)
        => options.TryGetValue("build_property." + property, out var text)
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            && value >= lowest
                ? value
                : null;
}
