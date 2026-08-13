using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Fdw.Results;
using FastEndpoints;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// The parent of every endpoints collection.
/// </summary>
/// <remarks>
/// The level above a resource. ScheduleEndpoints holds the endpoints over schedules; this holds ScheduleEndpoints and its siblings, so there is a name for
/// "every one of these the application serves" that is not a list somebody maintains.
///
/// Its ServiceCategory is what puts it in PlatformServices and names the generated accessor, so
/// <c>PlatformServices.Endpoints</c> reaches all of them without going through any one domain's service
/// type. That is the level that was missing: a tag every the endpoints over one resource shares belongs on that resource's
/// collection, and something they all share belongs here. Before this, both had to be repeated per
/// member or hoisted into a host that owns neither.
///
/// TBase is the interface rather than a class because a non-generic base cannot be inserted:
/// each resource collection already derives a closed TypeCollectionBase, and that base slot is taken.
/// The interface is the only type they all are.
///
/// A group joins EndpointGroups rather than naming this collection as a parent. The parent/child
/// arguments on the collection attributes emit a partial class onto the parent, and a partial cannot
/// span assemblies - the parent is here and the groups are in another package, so that route silently
/// produces nothing. Membership has no such limit.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(IEndpointTypeCollection),
    typeof(IEndpointTypeCollection),
    typeof(Endpoints),
    ServiceCategory = "Endpoints")]
public partial class Endpoints : ServiceTypeCollectionBase<IEndpointTypeCollection, IEndpointTypeCollection>
{
    // Why the framework's own registration lives here rather than in each Program.cs: it is the
    // endpoint framework, and this is the collection of endpoints. Splitting the pair - Add in the
    // composition root, Use in a host - is what let them drift apart, and the app then built, started,
    // and threw on the first request looking for IServiceResolver.
    //
    // A different framework is a different collection that brings its own pair. Nothing above here
    // names FastEndpoints.
    /// <summary>Gets every group of endpoints the application serves — one per resource.</summary>
    /// <returns>The groups, in registration order.</returns>
    /// <remarks>
    /// Read through to <see cref="EndpointGroups"/> rather than held here: a group joins by declaring
    /// membership of that collection, which works across packages, where a parent/child attribute would
    /// emit a partial onto this type and cannot cross an assembly.
    /// </remarks>
    public static IReadOnlyCollection<IEndpointTypeCollection> Groups() => EndpointGroups.All();

    static Endpoints()
    {
        // Why appended rather than assigned: on this base the func IS the member cycle, so assigning
        // would replace it and nothing would register. Appending runs this after every member has
        // registered - which is required, not stylistic: AddFastEndpoints performs endpoint discovery
        // eagerly inside its own call, so every DeclaredEndpoints.Declare must already have happened.
        // Run it first and it throws 'unable to find any endpoint declarations'.
        AppendConfiguration((builder, loggerFactory) =>
        {
            foreach (var group in Groups())
            {
                var result = group.Configure(builder, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        AppendRegistration((builder, loggerFactory) =>
        {
            // Why the groups are cycled here rather than being this collection's own members: a group
            // joins EndpointGroups, which is membership and crosses packages. This collection is what
            // PlatformServices drives, so it is where that list gets walked.
            foreach (var group in Groups())
            {
                var result = group.Register(builder, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            // Why the count is checked rather than left to fail later: with nothing declared,
            // AddFastEndpoints throws its own "unable to find any endpoint declarations", which says
            // nothing about which step went wrong. An application that reaches here having registered
            // no endpoint has a broken registration chain, and this names it.
            if (DeclaredEndpoints.Count == 0)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(
                    EndpointRegistrationLog.NoEndpointsDeclared(
                        loggerFactory?.CreateLogger(nameof(Endpoints)) ?? NullLogger.Instance));
            }

            builder.Services.AddFastEndpoints(o =>
            {
                // Why discovery is off and the types are handed over instead: an endpoint is
                // registered because its collection declared it. Scanning would add every endpoint
                // type in every loaded assembly regardless of whether a collection claims it, which
                // is the opposite of the switch these collections exist to provide.
                //
                // FastEndpoints offers no per-endpoint registration call. With auto-discovery off the
                // only way in is this list, which is why DeclaredEndpoints exists - each option adds
                // its own type as it registers, and this is where the collection gets passed across.
                // Filter is not used: it narrows what discovery found, and there is no discovery.
                o.DisableAutoDiscovery = true;
                o.SourceGeneratorDiscoveredTypes.AddRange(DeclaredEndpoints.Types);
            });

            builder.Services.AddHttpContextAccessor();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        AppendInitialization((host, loggerFactory) =>
        {
            foreach (var group in Groups())
            {
                var result = group.Initialize(host, loggerFactory);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            if (host is IApplicationBuilder app)
            {
                app.UseFastEndpoints();
            }

            return GenericResult<IHost>.Success(host);
        });
    }
}
