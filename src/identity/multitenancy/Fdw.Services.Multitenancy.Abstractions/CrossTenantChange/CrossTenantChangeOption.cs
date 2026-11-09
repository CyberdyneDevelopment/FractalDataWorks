using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.ServiceTypes;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Declares, for each kind of object, whether an administrator of one tenant may change that object of another tenant when they can see it (FDW-4458, FDW-4459).
/// </summary>
/// <remarks>
/// Each field is also the name of a Boolean setting (<c>SettingDefinitions</c>): a server setting row states the value for the server and wins; where there is no row the
/// value declared here applies, which a host overrides at registration with <see cref="ServiceTypeDefaults.Override"/>. Neither always on nor always refused: the value is
/// configuration, and the defaults below keep what each kind did before the setting existed. A value that is declared nowhere is a coded failure, never a stand-in.
/// </remarks>
public class CrossTenantChangeOption : ITypeOption, IDeclaresDefaults
{
    /// <summary>The setting and default field for enabling a disabled connection that belongs to another tenant.</summary>
    public const string ConnectionEnable = "CrossTenantConnectionEnable";

    /// <summary>The setting and default field for changing a data store that belongs to another tenant.</summary>
    public const string DataStoreWrite = "CrossTenantDataStoreWrite";

    private static readonly IReadOnlyDictionary<string, object> Declared =
        ServiceTypeDefaults.With(new Dictionary<string, object>(StringComparer.Ordinal), (ConnectionEnable, true), (DataStoreWrite, false));

    /// <summary>Gets the option the policy resolves its defaults from.</summary>
    public static CrossTenantChangeOption Instance { get; } = new();

    /// <summary>Initializes a new instance of the <see cref="CrossTenantChangeOption"/> class.</summary>
    public CrossTenantChangeOption()
    {
    }

    /// <inheritdoc/>
    public string Name => "CrossTenantChange";

    /// <inheritdoc/>
    public string Category => "Tenant";

    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, object> DeclaredDefaults => Declared;
}
