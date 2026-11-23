using System;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// One fact a pipeline type requires of the configuration of its pipelines (FDW-6141): the interface the layer reads the fact through and the test of whether a row states it.
/// </summary>
/// <remarks>
/// The layer owns each fact and states it as a small interface; an implementation row carries the value. A type that needs the fact says so here, once, and the layer
/// refuses a type whose configuration cannot state it (at Initialize) and a row that leaves it out (at the save): nothing is ignored silently and no layer code
/// knows which implementation it is reading.
/// </remarks>
public sealed class PipelineConfigurationRequirement
{
    private readonly Func<object, bool> _isStated;

    private PipelineConfigurationRequirement(string factName, Type configurationInterface, Func<object, bool> isStated)
    {
        FactName = factName;
        ConfigurationInterface = configurationInterface;
        _isStated = isStated;
    }

    /// <summary>Gets the name of the fact, as its column and property are named.</summary>
    public string FactName { get; }

    /// <summary>Gets the interface a configuration states the fact through.</summary>
    public Type ConfigurationInterface { get; }

    /// <summary>Makes the requirement that a row states a fact.</summary>
    /// <typeparam name="TInterface">The interface the layer reads the fact through.</typeparam>
    /// <param name="factName">The name of the fact.</param>
    /// <param name="isStated">Whether a configuration states it.</param>
    /// <returns>The requirement.</returns>
    public static PipelineConfigurationRequirement Of<TInterface>(string factName, Func<TInterface, bool> isStated)
        where TInterface : class
        => new(factName, typeof(TInterface), configuration => configuration is TInterface typed && isStated(typed));

    /// <summary>Says whether a configuration states the fact.</summary>
    /// <param name="configuration">The implementation row.</param>
    /// <returns><see langword="true"/> when the row implements the interface and states the fact.</returns>
    public bool IsStatedBy(object configuration) => _isStated(configuration);
}
