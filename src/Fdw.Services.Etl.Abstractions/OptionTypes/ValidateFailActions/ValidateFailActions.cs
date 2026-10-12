using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What a Validate stage does with a record that fails one of its checks: Reject or Fail (FDW-1120).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ValidateFailActionBase), typeof(IValidateFailAction), typeof(ValidateFailActions))]
public abstract partial class ValidateFailActions : TypeCollectionBase<ValidateFailActionBase, IValidateFailAction>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
