namespace Fdw.Data.Components.Models;

/// <summary>
/// String constants matching the <see cref="MappingTypes"/> TypeCollection option names.
/// Use these for string comparisons in consumer markup where TypeCollection lookup is unnecessary.
/// </summary>
public static class MappingTypeNames
{
    /// <summary>Source field value is copied directly to the target field.</summary>
    public const string Direct = "Direct";

    /// <summary>Source field value is transformed before writing to the target field.</summary>
    public const string Transform = "Transform";

    /// <summary>Target field is populated with a constant value regardless of source data.</summary>
    public const string Constant = "Constant";

    /// <summary>Target field is populated by a calculated expression.</summary>
    public const string Calculated = "Calculated";

    /// <summary>Source field has no corresponding target field mapping.</summary>
    public const string Unmapped = "Unmapped";
}
