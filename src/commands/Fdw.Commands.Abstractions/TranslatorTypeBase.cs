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
public abstract class TranslatorTypeBase : TypeOptionBase<TranslatorTypeBase>, ITranslatorType
{
    /// <inheritdoc/>
    /// <remarks>
    /// Why abstract and not a constructor parameter: the collection's generated NotFound sentinel calls the base
    /// constructor and could only pass null for it (TC009, FDW-3467). An abstract member is stubbed instead.
    /// The same holds for <see cref="TargetFormat"/> and <see cref="Capabilities"/>.
    /// </remarks>
    public abstract IDataFormat SourceFormat { get; }

    /// <inheritdoc/>
    public abstract IDataFormat TargetFormat { get; }

    /// <inheritdoc/>
    public abstract TranslationCapabilities Capabilities { get; }

    /// <inheritdoc/>
    public int Priority { get; }

    /// <inheritdoc/>
    public abstract IGenericResult<IGenericCommandTranslator> CreateTranslator(IServiceProvider services);

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this translator type.</param>
    /// <param name="description">The description of this translator type.</param>
    /// <param name="priority">The priority for translator selection (default 50).</param>
    protected TranslatorTypeBase(
        string name,
        string description,
        int priority = 50)
        : base(name, description)
    {
        Priority = priority;
    }
}