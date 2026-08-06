using System;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Interface for command translator type definitions.
/// </summary>
/// <remarks>
/// Translator types define the capabilities and metadata for command translators
/// that convert between different command formats (e.g., LINQ to SQL, LINQ to HTTP).
/// </remarks>
public interface ITranslatorType : ITypeOption<int, TranslatorTypeBase>
{
    /// <summary>
    /// Gets the source format this translator handles.
    /// </summary>
    /// <value>The input format that this translator can process.</value>
    IDataFormat SourceFormat { get; }

    /// <summary>
    /// Gets the target format this translator produces.
    /// </summary>
    /// <value>The output format that this translator generates.</value>
    IDataFormat TargetFormat { get; }

    /// <summary>
    /// Gets the translation capabilities of this translator.
    /// </summary>
    /// <value>Detailed capabilities for query planning and optimization.</value>
    TranslationCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the priority for translator selection.
    /// </summary>
    /// <value>Higher values indicate preference when multiple translators are available.</value>
    int Priority { get; }

    /// <summary>
    /// Creates a translator instance using dependency injection.
    /// </summary>
    /// <param name="services">The service provider for dependency resolution.</param>
    /// <returns>A result containing the translator instance or failure message.</returns>
    IGenericResult<IGenericCommandTranslator> CreateTranslator(IServiceProvider services);
}