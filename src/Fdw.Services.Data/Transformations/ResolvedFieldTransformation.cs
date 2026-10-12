using System.Collections.Generic;
using Fdw.Data.Transformations;

namespace Fdw.Services.Data.Transformations;

/// <summary>
/// One link of a field mapping's transformation chain, ready to run: the operation and the parameter
/// values the attached transformation configures.
/// </summary>
/// <param name="Name">The transformation's name, for logs and errors.</param>
/// <param name="Operation">The per-field operation (a <c>TransformationTypes</c> option).</param>
/// <param name="Parameters">The parameter values, by parameter name.</param>
public sealed record ResolvedFieldTransformation(
    string Name,
    FieldTransformationBase Operation,
    IReadOnlyDictionary<string, string> Parameters);
