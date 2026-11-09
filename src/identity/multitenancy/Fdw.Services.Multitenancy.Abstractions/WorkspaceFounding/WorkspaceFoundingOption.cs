using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.ServiceTypes;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Declares whether founding a tenant or an organization makes a default workspace (FDW-4599).
/// </summary>
/// <remarks>
/// The field is also the name of a Boolean setting (<c>SettingDefinitions</c>): a server setting row states the value and wins; where there is no row the value declared here
/// applies, which a host overrides at registration with <see cref="ServiceTypeDefaults.Override"/>. The default keeps what founding did before the setting existed (it makes one).
/// A value declared nowhere is a coded failure, never a stand-in.
/// </remarks>
public class WorkspaceFoundingOption : ITypeOption, IDeclaresDefaults
{
    /// <summary>The setting and default field: whether founding makes a default workspace.</summary>
    public const string FoundingMakesDefaultWorkspace = "FoundingMakesDefaultWorkspace";

    private static readonly IReadOnlyDictionary<string, object> Declared =
        ServiceTypeDefaults.With(new Dictionary<string, object>(StringComparer.Ordinal), (FoundingMakesDefaultWorkspace, true));

    /// <summary>Gets the option the policy resolves its default from.</summary>
    public static WorkspaceFoundingOption Instance { get; } = new();

    /// <summary>Initializes a new instance of the <see cref="WorkspaceFoundingOption"/> class.</summary>
    public WorkspaceFoundingOption()
    {
    }

    /// <inheritdoc/>
    public string Name => "WorkspaceFounding";

    /// <inheritdoc/>
    public string Category => "Tenant";

    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, object> DeclaredDefaults => Declared;
}
