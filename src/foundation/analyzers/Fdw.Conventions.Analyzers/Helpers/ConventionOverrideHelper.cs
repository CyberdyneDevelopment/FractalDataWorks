using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers.Helpers;

/// <summary>
/// Shared helper for reading [ConventionOverride] attribute values and MSBuild properties.
/// </summary>
internal static class ConventionOverrideHelper
{
    /// <summary>
    /// Gets the override value for a property from [ConventionOverride] on the method or containing class.
    /// </summary>
    internal static int? GetOverrideValue(SyntaxNode node, string propertyName)
    {
        // Check the node itself first (method level)
        var value = GetAttributeValue(node, propertyName);
        if (value.HasValue)
            return value;

        // Walk up to containing type declaration
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is TypeDeclarationSyntax)
            {
                value = GetAttributeValue(parent, propertyName);
                if (value.HasValue)
                    return value;
                break;
            }
            parent = parent.Parent;
        }

        return null;
    }

    private static int? GetAttributeValue(SyntaxNode node, string propertyName)
    {
        // Get attribute lists from the node
        SyntaxList<AttributeListSyntax> attributeLists;
        if (node is BaseMethodDeclarationSyntax method)
            attributeLists = method.AttributeLists;
        else if (node is TypeDeclarationSyntax type)
            attributeLists = type.AttributeLists;
        else
            return null;

        foreach (var attrList in attributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (!string.Equals(name, "ConventionOverride", StringComparison.Ordinal) &&
                    !string.Equals(name, "ConventionOverrideAttribute", StringComparison.Ordinal))
                    continue;

                if (attr.ArgumentList == null)
                    continue;

                foreach (var arg in attr.ArgumentList.Arguments)
                {
                    if (arg.NameEquals != null &&
                        string.Equals(arg.NameEquals.Name.Identifier.Text, propertyName, StringComparison.Ordinal) &&
                        arg.Expression is LiteralExpressionSyntax literal &&
                        literal.Token.Value is int intValue &&
                        intValue >= 0)
                    {
                        return intValue;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Reads an MSBuild property as an integer.
    /// </summary>
    internal static int GetBuildPropertyInt(AnalyzerConfigOptionsProvider globalOptions, string name, int defaultValue)
    {
        if (globalOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value) &&
            int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    /// Reads an MSBuild property as a string.
    /// </summary>
    internal static string GetBuildPropertyString(AnalyzerConfigOptionsProvider globalOptions, string name, string defaultValue)
    {
        if (globalOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value) &&
            !string.IsNullOrEmpty(value))
        {
            return value;
        }

        return defaultValue;
    }
}
