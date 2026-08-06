using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// HTTP client operations including REST API calls and web service requests.
/// Designed for network-related transient failures and rate limiting scenarios.
/// </summary>
[TypeOption(typeof(ResiliencyCategories), "HttpClient")]
[ExcludeFromCodeCoverage]
public sealed class HttpClientResiliencyCategory : ResiliencyCategoryBase
{
    /// <summary>Initializes a new instance of <see cref="HttpClientResiliencyCategory"/>.</summary>
    public HttpClientResiliencyCategory() : base(2, "HttpClient") { }
}
