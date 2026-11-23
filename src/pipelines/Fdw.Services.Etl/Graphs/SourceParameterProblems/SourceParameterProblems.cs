using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The ways a setting that is read when a stage starts can name a parameter or variable wrongly: a variable no lower stage sets, a
/// variable a component sets while the run goes, and a name the pipeline does not declare.
/// </summary>
/// <remarks>The three exclude one another and hold every pair of facts, so a name that is wrong has exactly one of them.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SourceParameterProblemBase), typeof(ISourceParameterProblem), typeof(SourceParameterProblems))]
public abstract partial class SourceParameterProblems : TypeCollectionBase<SourceParameterProblemBase, ISourceParameterProblem>
{
    /// <summary>Finds the problem of a name with these facts.</summary>
    /// <param name="isAssignedVariable">Whether the name is a variable some component sets while the run goes.</param>
    /// <param name="late">Whether no component of a lower stage sets the variable before this setting is read.</param>
    /// <returns>The problem that holds the pair.</returns>
    public static ISourceParameterProblem Of(bool isAssignedVariable, bool late)
        => All().First(problem => problem.Matches(isAssignedVariable, late));
}
