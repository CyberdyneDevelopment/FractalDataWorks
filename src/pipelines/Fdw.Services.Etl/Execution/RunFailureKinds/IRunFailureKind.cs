using Fdw.Collections;

namespace Fdw.Services.Etl.Execution;

/// <summary>A kind of failure that ended a pipeline run, by whether it is classified and whether it is a refusal, and how it is told in words.</summary>
public interface IRunFailureKind : ITypeOption<int, RunFailureKindBase>
{
    /// <summary>Gets whether a failure of this kind carries a coded failure message in a known category.</summary>
    bool Classified { get; }

    /// <summary>Gets whether a failure of this kind does not count toward a failure threshold.</summary>
    bool Refused { get; }

    /// <summary>Words a failure of this kind for the event's message.</summary>
    /// <param name="failure">The classification of the failure: its category, its code and whether the category is one the platform retries.</param>
    /// <returns>The sentence.</returns>
    string Describe(RunFailureClassification failure);
}
