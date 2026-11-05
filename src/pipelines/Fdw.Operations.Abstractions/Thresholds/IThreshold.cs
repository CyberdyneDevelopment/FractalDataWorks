using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// A kind of threshold (FDW-3936): the subject it is attached to and the values that subject reports, which are the fields its rules may name. An option of
/// <c>ThresholdTypes</c> is one.
/// </summary>
public interface IThreshold : ITypeOption<int, IThreshold>
{
    /// <summary>Gets the kind of subject the threshold is attached to: Pipeline, Component, Connection, Schedule or DataSet.</summary>
    string SubjectKind { get; }

    /// <summary>Gets what the threshold watches, in a sentence (the description every type option states).</summary>
    string Description { get; }

    /// <summary>Gets the values the subject reports. A rule criteria names only these.</summary>
    IReadOnlyList<ThresholdField> Fields { get; }

    /// <summary>Gets the parameters the type takes: how its measure is taken. A type whose values need no parameter declares none.</summary>
    IReadOnlyList<ThresholdParameterDeclaration> Parameters { get; }
}
