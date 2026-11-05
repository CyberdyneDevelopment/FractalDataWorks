using Fdw.Conventions;

namespace Fdw.Web.RestEndpoints;

/// <summary>
/// The texts an endpoint response states where the failure it answers carries none (FDW-3580). None of them is ever a code or category
/// a producer states.
/// </summary>
/// <remarks>
/// Why declared here: FDW055 accepts a placeholder only as a [PlaceholderText] constant, and the CRUD, trigger and generic endpoint
/// bases and the result mapper each wrote the same literal. The wire values are kept so existing clients still match on them.
/// </remarks>
[PlaceholderText]
public static class EndpointPlaceholders
{
    /// <summary>The error code a response states when the failure message it answers carries no code.</summary>
    public const string InternalErrorCode = "INTERNAL_ERROR";

    /// <summary>The error code a response states when the failed result carries no result code and no coded message.</summary>
    public const string UnknownErrorCode = "UNKNOWN_ERROR";

    /// <summary>The category an endpoint option states when its subclass names none.</summary>
    public const string NoCategory = "Endpoint";
}
