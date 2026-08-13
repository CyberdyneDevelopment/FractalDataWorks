using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services.SessionState;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Results;

namespace Fdw.Services.SessionState.Clients;

/// <summary>
/// HTTP client session state service type that registers <see cref="HttpSessionStateService"/>
/// as the <see cref="ISessionStateService"/> implementation with a named HttpClient
/// configured with bearer token authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(SessionStateTypes), "Http")]
public sealed class HttpSessionStateServiceType : SessionStateServiceTypeBase<IGenericService, ISessionStateServiceFactory>
{
    internal const string HttpClientName = "SessionStateApi";

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpSessionStateServiceType"/> class.
    /// </summary>
    public HttpSessionStateServiceType()
        : base(
            "Http",
            "SessionState:Http",
            "HTTP Session State Client",
            "HTTP-backed session state client for Blazor UI applications")
    {
        Configuration(builder =>
        {

            // Why the ApiClients hierarchy rather than this option's own SessionState:Http section: this
            // registers a named API client exactly like the other 34, and reading a private section meant a
            // host that moved its API had to restate the same endpoint in a second place — which is how the
            // UI ended up pointing session state at a dev URL while its other clients were correct.
            builder.Services.AddApiHttpClient(builder.Configuration, HttpClientName);
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddScoped<ISessionStateService, HttpSessionStateService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
