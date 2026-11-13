namespace Fdw.Services.Connections.Http.Paging;

/// <summary>Where a paging value an HTTP read uses was stated (FDW-5562): the names a container's resolved paging reports for each of its values.</summary>
public static class HttpPagingSources
{
    /// <summary>The container states the value, overriding its connection's.</summary>
    public const string Container = "container";

    /// <summary>The connection states the value, and the container does not.</summary>
    public const string Connection = "connection";

    /// <summary>Neither states it: the paging style declares the value (a host may have changed it at registration).</summary>
    public const string Style = "style";
}
