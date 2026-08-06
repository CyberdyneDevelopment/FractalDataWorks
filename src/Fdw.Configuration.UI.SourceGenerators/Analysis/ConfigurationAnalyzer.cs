using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Fdw.Configuration.UI.SourceGenerators.Models;

namespace Fdw.Configuration.UI.SourceGenerators.Analysis;

/// <summary>
/// Analyzes configuration classes for UI generation.
/// </summary>
public sealed class ConfigurationAnalyzer
{
    private const string ManagedConfigurationAttribute = "Fdw.Configuration.ManagedConfigurationAttribute";

    /// <summary>
    /// Analyzes a configuration class and creates a configuration model.
    /// </summary>
    /// <param name="classSymbol">The class symbol to analyze.</param>
    /// <returns>A configuration model containing analyzed metadata.</returns>
    public static ConfigurationModel Analyze(INamedTypeSymbol classSymbol)
    {
        var model = new ConfigurationModel
        {
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            ClassName = classSymbol.Name
        };

        // Extract values from [ManagedConfiguration] attribute
        var managedConfigAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => string.Equals(a.AttributeClass?.ToDisplayString(), ManagedConfigurationAttribute, StringComparison.Ordinal));

        if (managedConfigAttr != null)
        {
            foreach (var namedArg in managedConfigAttr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "GenerateWeb":
                        model.GenerateWeb = namedArg.Value.Value is bool genWeb && genWeb;
                        break;
                    case "GenerateBlazor":
                        model.GenerateBlazor = namedArg.Value.Value is bool genBlazor && genBlazor;
                        break;
                    case "GenerateTui":
                        model.GenerateTui = namedArg.Value.Value is bool genTui && genTui;
                        break;
                    case "DisplayName":
                        model.DisplayName = namedArg.Value.Value as string;
                        break;
                    case "Description":
                        model.Description = namedArg.Value.Value as string;
                        break;
                    case "ServiceCategory":
                        model.ServiceCategory = namedArg.Value.Value as string;
                        break;
                    case "ServiceType":
                        model.ServiceType = namedArg.Value.Value as string;
                        break;
                }
            }
        }

        // Analyze all public properties
        var properties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public)
            .Select(p => PropertyAnalyzer.Analyze(p))
            .ToList();

        model.Properties = properties;
        model.HasNestedCollections = properties.Any(p => p.IsCollection);

        return model;
    }
}
