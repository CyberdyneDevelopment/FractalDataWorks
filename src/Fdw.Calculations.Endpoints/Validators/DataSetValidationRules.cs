using FluentValidation;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Calculations.Endpoints.Validators;

/// <summary>
/// Reusable FluentValidation rules for DataSet-bound request fields.
/// </summary>
public static class DataSetValidationRules
{
    /// <summary>
    /// Async rule that fails when the supplied name does not resolve to a registered DataSet.
    /// Use this when the contract is "bad DataSetName → 400" (validator semantics). For
    /// "bad DataSetName → 404" semantics, call <see cref="DataSetLookup.Exists"/> directly
    /// from the endpoint.
    /// </summary>
    public static IRuleBuilderOptions<T, string> DataSetMustExist<T>(
        this IRuleBuilder<T, string> rule,
        IConfigurationGateway configGateway)
    {
        return rule
            .MustAsync((name, ct) => DataSetLookup.Exists(configGateway, name, ct))
            .WithMessage((_, name) => $"DataSet '{name}' was not found.");
    }
}
