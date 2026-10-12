namespace Fdw.Web.Endpoints.SessionState;

/// <summary>Request model for getting a session state value by key.</summary>
public sealed class SessionStateKeyRequest
{
    /// <summary>Gets or sets the state key.</summary>
    public string Key { get; set; } = string.Empty;
}
