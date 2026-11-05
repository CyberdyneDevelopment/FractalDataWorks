namespace Fdw.Conventions;

/// <summary>
/// The texts a log line or a failure detail shows where a value is absent, shared by every project that names one. None of them is ever a
/// valid value of what it stands for (FDW-3580).
/// </summary>
/// <remarks>
/// Why one class here: FDW055 accepts a placeholder only as a [PlaceholderText] constant, and the hosts, secret managers and services that
/// log an absent command, key or type each wrote their own literal. The wording each used is kept.
/// </remarks>
[PlaceholderText]
public static class LogPlaceholders
{
    /// <summary>Stands for an absent object, such as a null command, where its type name would go.</summary>
    public const string Null = "(null)";

    /// <summary>Stands for an absent type name, as the secret manager factories and services wrote it.</summary>
    public const string NullName = "null";

    /// <summary>Stands for an absent secret key on a command that names none.</summary>
    public const string NoSecretKey = "N/A";

    /// <summary>Stands for an absent exception type when a validator gave no exception.</summary>
    public const string NoExceptionType = "unknown";

    /// <summary>Stands for a caller with no name, such as an anonymous one.</summary>
    public const string Anonymous = "(anonymous)";

    /// <summary>Stands for a credential issued with no lease expiry.</summary>
    public const string NoLease = "no lease";

    /// <summary>Stands for a secret read without naming a version, which is its latest one.</summary>
    public const string LatestVersion = "latest";

    /// <summary>Stands for a file path not known yet when a load failed.</summary>
    public const string NoPath = "unknown";

    /// <summary>Stands for the component a message came from when the message names none, as MessageTemplate wrote it.</summary>
    public const string NoSource = "Unknown";

    /// <summary>Stands for a value the message names when it has none, such as an implementation or a tenant.</summary>
    public const string None = "(none)";

    /// <summary>Stands for an authentication level (ACR) that was not reached or not required.</summary>
    public const string NoLevel = "none";

    /// <summary>Stands for a key field whose referenced field could not be resolved.</summary>
    public const string Unresolved = "(unresolved)";

    /// <summary>Stands for a configuration that carries no name.</summary>
    public const string Unnamed = "unnamed";

    /// <summary>Stands for a validated identity that carries no name claim.</summary>
    public const string NoName = "(no name)";

    /// <summary>Stands for a cached refusal that recorded no reason.</summary>
    public const string NoReason = "unknown reason";

    /// <summary>Stands for an authentication run that ended with no decision.</summary>
    public const string NoDecision = "no decision was reached";

    /// <summary>Stands for a message that names no one recipient: it goes to everyone.</summary>
    public const string Broadcast = "broadcast";

    /// <summary>Stands for a request whose client address is not known.</summary>
    public const string NoAddress = "unknown";

    /// <summary>Stands for the reason phrase of an HTTP response that carried none.</summary>
    public const string NoReasonPhrase = "Unknown";

    /// <summary>Stands for the data type name of a field declared before its type is known.</summary>
    public const string NoTypeName = "Unknown";

    /// <summary>Stands for a list request that names no filter, so every item is listed.</summary>
    public const string NoFilter = "all";
}
