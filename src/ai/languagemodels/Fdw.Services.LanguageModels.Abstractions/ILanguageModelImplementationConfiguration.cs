using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// The configuration every language model provider's row carries. The secret fields are the names a secret manager is asked by, never values.
/// </summary>
/// <remarks>
/// Every property here is data on the row: the model id, which secret manager holds the key and under what name, and what the model carries.
/// Nothing is defaulted in code. A row without a model id is refused when it is written.
/// </remarks>
public interface ILanguageModelImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the id the provider knows the model by. Required.</summary>
    string? ModelId { get; set; }

    /// <summary>Gets or sets the name of the secret manager that holds the provider's key. Required.</summary>
    string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the name the provider's key is kept under in the secret manager. Required.</summary>
    string? ApiKeySecretName { get; set; }

    /// <summary>Gets the names of the <see cref="ModelCapabilities"/> options the model carries.</summary>
    IReadOnlyCollection<string> CapabilityNames { get; }

    /// <summary>Gets the names of the <see cref="ReasoningEfforts"/> options the model carries.</summary>
    IReadOnlyCollection<string> ReasoningEffortNames { get; }
}
