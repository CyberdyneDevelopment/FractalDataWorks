using FastEndpoints;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that checks a data option path (FDW-6339).</summary>
public sealed class CheckDataOptionPathRequest
{
    /// <summary>Gets or sets the path to check: the whole body of the request.</summary>
    [FromBody]
    public DataOptionPathCheckBody? Path { get; set; }
}
