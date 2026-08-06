using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Fdw.Collections;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;

namespace Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;

/// <summary>
/// Base class for Azure credential types using the CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class AzureCredentialTypeBase : TypeOptionBase<int, AzureCredentialTypeBase>, IAzureCredentialType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCredentialTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this credential type.</param>
    /// <param name="name">The name for this credential type.</param>
    protected AzureCredentialTypeBase(int id, string name) : base(id, name) { }

    /// <inheritdoc/>
    public abstract TokenCredential Create(AzureKeyVaultConfiguration config);
}
