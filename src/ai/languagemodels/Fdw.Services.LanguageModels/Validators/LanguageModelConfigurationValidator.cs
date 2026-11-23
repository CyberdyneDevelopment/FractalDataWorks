using System.Collections.Generic;
using System.Linq;
using Fdw.Services.LanguageModels.Abstractions;
using Fdw.Validation;
using FluentValidation;

namespace Fdw.Services.LanguageModels.Validators;

/// <summary>
/// Refuses a language model row that cannot be used: no model id, no secret manager or secret name, no capabilities, a capability or effort that is not an
/// option of its collection, or a model that does not carry Chat. There is no default for any of them.
/// </summary>
public sealed class LanguageModelConfigurationValidator : FdwConfigurationValidator<ILanguageModelImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelConfigurationValidator"/> class.</summary>
    public LanguageModelConfigurationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.ModelId).NotEmpty().WithMessage("ModelId is required: a language model row states the model it calls");
        RuleFor(x => x.SecretManagerName).NotEmpty().WithMessage("SecretManagerName is required: the key is read from a secret manager");
        RuleFor(x => x.ApiKeySecretName).NotEmpty().WithMessage("ApiKeySecretName is required: the key is read by this name");
        RuleFor(x => x.CapabilityNames).Must(CarryChat).WithMessage("CapabilityNames must include Chat");
        RuleFor(x => x.CapabilityNames).Must(names => AllAreOptions(names, ModelCapabilityExists))
            .WithMessage("CapabilityNames holds a name that is not an option of ModelCapabilities");
        RuleFor(x => x.ReasoningEffortNames).NotEmpty().WithMessage("ReasoningEffortNames must state at least one effort the model carries");
        RuleFor(x => x.ReasoningEffortNames).Must(names => AllAreOptions(names, ReasoningEffortExists))
            .WithMessage("ReasoningEffortNames holds a name that is not an option of ReasoningEfforts");
    }

    private static bool CarryChat(IReadOnlyCollection<string> names)
        => names.Any(name => string.Equals(name, "Chat", System.StringComparison.OrdinalIgnoreCase));

    private static bool AllAreOptions(IReadOnlyCollection<string> names, System.Func<string, bool> exists)
        => names.All(exists);

    private static bool ModelCapabilityExists(string name) => !ReferenceEquals(ModelCapabilities.ByName(name), ModelCapabilities.NotFound);

    private static bool ReasoningEffortExists(string name) => !ReferenceEquals(ReasoningEfforts.ByName(name), ReasoningEfforts.NotFound);
}
