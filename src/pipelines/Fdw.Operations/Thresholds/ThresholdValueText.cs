using System.Collections.Generic;
using System.Linq;

namespace Fdw.Operations.Thresholds;

/// <summary>Reads a stored value back from its text and data type (FDW-3935): the one reader for a criteria condition and a threshold parameter.</summary>
internal static class ThresholdValueText
{
    // The data types Read understands, for a validator that refuses a row naming any other: the names of ThresholdDataTypes.
    public static IReadOnlyList<string> DataTypes => [.. ThresholdDataTypes.All().Select(type => type.Name)];

    public static object? Read(string dataType, string text) => ThresholdDataTypes.ByName(dataType).Read(text);
}
