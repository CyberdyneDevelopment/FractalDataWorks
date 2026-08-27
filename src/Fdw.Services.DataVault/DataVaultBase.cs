using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Fdw.Services.DataVault.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.DataVault;

/// <summary>
/// Abstract base for every data vault. Holds the resolved <see cref="IDataConnection"/> and the
/// pepper (HMAC key) privately and exposes ONLY the four protected primitives a concrete vault
/// needs: <see cref="Query{T}"/>, <see cref="NonQuery"/>, <see cref="Pepper"/>, and
/// <see cref="ConstantTimeEquals"/>. There is no command surface and no way for code authored
/// elsewhere to obtain the pepper.
/// </summary>
/// <remarks>
/// <para>
/// The connection and the pepper are resolved ONCE, in system context, by
/// <see cref="DataVaultProvider"/> during cache population, then handed to the vault
/// constructor. A vault is therefore fully resolved AT CONSTRUCTION and immutable — there is no
/// init method and no per-call re-check. <see cref="Query{T}"/>/<see cref="NonQuery"/> are
/// DB-specific and abstract here; a connection-type-specific base (e.g. <c>MsSqlDataVaultBase</c>)
/// implements them with parameterized ADO. Connection type stays invisible above the vault —
/// consumers only ever see a narrow per-domain interface.
/// </para>
/// </remarks>
public abstract class DataVaultBase : IDataVault, IDisposable
{
    // Why: connection + pepper are the dangerous capabilities. They are PRIVATE, immutable, and
    // have no accessor of any kind for the pepper. The connection is reachable by a DB-specific
    // subclass only through RequireConnection() (it is not a secret — the pepper is), so the
    // abstract Query/NonQuery can run ADO. Nothing here is exposed to vault consumers.
    private readonly string _vaultName;
    private readonly IDataConnection _connection;
    private readonly byte[] _pepper;
    private readonly ILogger<DataVaultBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultBase"/> class with its already-resolved
    /// connection and pepper. There is no async initialization — the provider resolves both ONCE in
    /// system context and hands them here; the vault is ready the moment it is constructed.
    /// </summary>
    /// <param name="vaultName">The vault's name (used as its service identity).</param>
    /// <param name="connection">The resolved data connection the vault rides.</param>
    /// <param name="pepper">The resolved pepper (HMAC key) bytes; ownership transfers to the vault.</param>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger"/>.</param>
    protected DataVaultBase(
        string vaultName,
        // Why: connection is resolved once by DataVaultProvider in system context and handed
        // in here — the vault is immutable at construction, not a live service-locator dependency.
        [ServiceOptionDependency] IDataConnection connection,
        byte[] pepper,
        ILogger<DataVaultBase>? logger)
    {
        _vaultName = vaultName ?? throw new ArgumentNullException(nameof(vaultName));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _pepper = pepper ?? throw new ArgumentNullException(nameof(pepper));
        _logger = logger ?? NullLogger<DataVaultBase>.Instance;
    }

    /// <summary>Gets the vault's name.</summary>
    public string Name => _vaultName;

    // ── IGenericService ────────────────────────────────────────────────────────

    /// <inheritdoc />
    string IGenericService.Id => _vaultName;

    /// <inheritdoc />
    string IGenericService.ServiceType => "DataVault";

    /// <inheritdoc />
    // Why: a constructed vault is always available — its connection and pepper are resolved before
    // construction. There is no init state to gate on.
    bool IGenericService.IsAvailable => true;

    /// <inheritdoc />
    // Why: a vault has NO generic command surface — that closed set is the access policy. A generic
    // command is rejected fail-loud; capabilities are reached only through a narrow per-domain interface.
    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(DataVaultLog.GenericCommandRejected(_logger, _vaultName)));

    /// <inheritdoc />
    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(DataVaultLog.GenericCommandRejected(_logger, _vaultName)));

    // ── Protected primitives (the ONLY surface a concrete vault uses) ──────────

    /// <summary>Gets the vault's logger for MessageLogging inside concrete verbs.</summary>
    protected ILogger<DataVaultBase> Logger => _logger;

    /// <summary>
    /// Returns the resolved connection for a DB-specific subclass to run parameterized ADO. The
    /// connection is set at construction and never null; this is the (non-secret) transport — the
    /// pepper is never exposed.
    /// </summary>
    protected IGenericResult<IDataConnection> RequireConnection()
        => GenericResult<IDataConnection>.Success(_connection);

    /// <summary>
    /// Runs a parameterized read and returns the scalar value as <typeparamref name="T"/> (the first
    /// column of the first row), or <c>default</c> when no row is present. DB-specific — implemented by
    /// a connection-type base. Parameterized only; never interpolate values; never log secret material.
    /// </summary>
    /// <typeparam name="T">The scalar value type (e.g. <c>byte[]</c>).</typeparam>
    /// <param name="sql">Parameterized SQL with named parameters only.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <param name="parameters">Named parameter name/value pairs bound as DB parameters.</param>
    protected abstract Task<IGenericResult<T>> Query<T>(string sql, CancellationToken cancellationToken, params (string name, object? value)[] parameters);

    /// <summary>
    /// Runs a parameterized non-query and returns rows affected. DB-specific — implemented by a
    /// connection-type base. Parameterized only; never interpolate values; never log secret material.
    /// </summary>
    /// <param name="sql">Parameterized SQL with named parameters only.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <param name="parameters">Named parameter name/value pairs bound as DB parameters.</param>
    protected abstract Task<IGenericResult<int>> NonQuery(string sql, CancellationToken cancellationToken, params (string name, object? value)[] parameters);

    /// <summary>
    /// Applies the pepper to a derived hash: <c>HMAC-SHA256(derivedHash, pepper)</c>. The pepper
    /// never leaves this method.
    /// </summary>
    /// <param name="derivedHash">The KDF output to pepper.</param>
    protected byte[] Pepper(byte[] derivedHash)
    {
        if (derivedHash is null)
            throw new ArgumentNullException(nameof(derivedHash));

        using var hmac = new HMACSHA256(_pepper);
        return hmac.ComputeHash(derivedHash);
    }

    /// <summary>
    /// Constant-time byte comparison. Use this — never <c>==</c>/<c>SequenceEqual</c> — for secret
    /// material so the number of matching leading bytes does not leak via timing.
    /// </summary>
    protected static bool ConstantTimeEquals(byte[] a, byte[] b)
        => CryptographicOperations.FixedTimeEquals(a, b);

    // ── IDisposable ────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases resources and clears the pepper from memory.</summary>
    /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        Array.Clear(_pepper, 0, _pepper.Length);
    }
}
