using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Fdw.CodeBuilder.CSharp.Builders;
using Fdw.SourceGenerators.Configuration;
using Fdw.SourceGenerators.Models;

namespace Fdw.SourceGenerators.Generators;

/// <summary>
/// Generates static properties or methods for collection values.
/// </summary>
/// <typeparam name="TId">The type used for collection IDs (int for Collections, Guid for ServiceTypes).</typeparam>
public sealed class ValuePropertyGenerator<TId>
    where TId : struct
{
    private readonly CollectionBuilderConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValuePropertyGenerator{TId}"/> class.
    /// </summary>
    /// <param name="config">The configuration for collection generation.</param>
    public ValuePropertyGenerator(CollectionBuilderConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Generates static properties for each value in the collection.
    /// </summary>
    /// <param name="values">The list of collection values.</param>
    /// <param name="returnType">The return type for the properties.</param>
    /// <param name="useMethods">Whether to generate methods instead of properties.</param>
    /// <returns>A list of property builders.</returns>
    public static IEnumerable<IPropertyBuilder> GenerateValueProperties(
        IList<GenericValueInfoModel<TId>> values,
        string returnType,
        bool useMethods = false)
    {
        var properties = new List<IPropertyBuilder>();

        foreach (var value in values.Where(v => v.Include && !v.IsAbstract && !v.IsStatic))
        {
            var property = GenerateValueProperty(value, returnType);
            if (property != null)
            {
                properties.Add(property);
            }
        }

        return properties;
    }

    /// <summary>
    /// Generates a static property for a single value.
    /// </summary>
    private static IPropertyBuilder? GenerateValueProperty(GenericValueInfoModel<TId> value, string returnType)
    {
        string expressionBody;
        string xmlDoc;

        if (value.IsAbstract || value.IsStatic)
        {
            // Abstract/static types return empty instance
            expressionBody = "_empty";
            xmlDoc = $"Gets the {value.Name} value. Returns empty instance since type is {(value.IsAbstract ? "abstract" : "static")}.";
        }
        else if (value.BaseConstructorId.HasValue)
        {
            // Use literal ID from base constructor argument
            expressionBody = $"_all.TryGetValue({value.BaseConstructorId.Value}, out var result) ? result : _empty";
            xmlDoc = $"Gets the {value.Name} value from the collection.";
        }
        else
        {
            // Fallback: use stored ID field
            var idFieldName = $"_{value.Name.ToLower(CultureInfo.InvariantCulture)}Id";
            expressionBody = $"_all.TryGetValue({idFieldName}, out var result) ? result : _empty";
            xmlDoc = $"Gets the {value.Name} value from the collection.";
        }

        var propertyBuilder = new PropertyBuilder()
            .WithName(value.Name)
            .WithType(returnType)
            .WithAccessModifier("public")
            .AsStatic()
            .WithXmlDoc(xmlDoc)
            .WithExpressionBody(expressionBody);

        return propertyBuilder;
    }
}
