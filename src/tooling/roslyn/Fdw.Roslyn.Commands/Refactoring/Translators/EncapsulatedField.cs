using System;
using Fdw.Conventions;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// A field being encapsulated as a property, and the property name FDW's naming convention gives it.
/// </summary>
/// <param name="FieldName">The name of the field being encapsulated.</param>
internal readonly record struct EncapsulatedField(string FieldName)
{
    /// <summary>
    /// Gets the property name under FDW's naming convention: the field name without its <c>_</c> or <c>m_</c>
    /// prefix, in Pascal case. Used when the command names no property.
    /// </summary>
    [PlatformDefault]
    public string ConventionalPropertyName
    {
        get
        {
            if (FieldName.Length > 1 && FieldName[0] == '_')
                return char.ToUpperInvariant(FieldName[1]) + FieldName.Substring(2);

            if (FieldName.Length > 2 && FieldName.StartsWith("m_", StringComparison.Ordinal))
                return char.ToUpperInvariant(FieldName[2]) + FieldName.Substring(3);

            return char.ToUpperInvariant(FieldName[0]) + FieldName.Substring(1);
        }
    }
}
