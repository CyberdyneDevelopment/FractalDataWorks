using System;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Base class for translator type definitions.
/// </summary>
/// <remarks>
/// Translator types provide metadata and factory methods for command translators.
/// Inherit from this class to define specific translator types that will be discovered
/// by the TypeCollection source generator.
/// </remarks>
public abstract class TranslatorTypeBase : TypeOptionBase<int, TranslatorTypeBase>, ITranslatorType
{
    /// <inheritdoc/>
    public IDataFormat SourceFormat { get; }

    /// <inheritdoc/>
    public IDataFormat TargetFormat { get; }

    /// <inheritdoc/>
    public TranslationCapabilities Capabilities { get; }

    /// <inheritdoc/>
    public int Priority { get; }

    /// <inheritdoc/>
    public abstract IGenericResult<IGenericCommandTranslator> CreateTranslator(IServiceProvider services);

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this translator type.</param>
    /// <param name="name">The name of this translator type.</param>
    /// <param name="description">The description of this translator type.</param>
    /// <param name="sourceFormat">The source format this translator handles.</param>
    /// <param name="targetFormat">The target format this translator produces.</param>
    /// <param name="capabilities">The translation capabilities.</param>
    /// <param name="priority">The priority for translator selection (default 50).</param>
    protected TranslatorTypeBase(
        int id,
        string name,
        string description,
        IDataFormat sourceFormat,
        IDataFormat targetFormat,
        TranslationCapabilities capabilities,
        int priority = 50)
        : base(id, name, description)
    {
        SourceFormat = sourceFormat;
        TargetFormat = targetFormat;
        Capabilities = capabilities;
        Priority = priority;
    }
}