using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Lists the types of category an administrator can create (FDW-1465): the names a category states as its type.</summary>
public abstract class GetCategoryTypesEndpointBase : EndpointWithoutRequest<CategoryTypesPayload>
{
    /// <summary>Gets the policy a caller holds to read the types.</summary>
    protected virtual string ReadPolicy => "categories:read";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/category-types");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "List the category types";
            s.Description = "Answers DataSet (a category for organising data sets), Glossary (for glossary terms) and Custom (a hierarchy an administrator makes at runtime), each with what it is for.";
        });
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(
            new CategoryTypesPayload
            {
                Types = [.. CategoryTypes.All()
                    .OrderBy(type => type.Name, System.StringComparer.Ordinal)
                    .Select(type => new CategoryTypePayload { Name = type.Name, Description = type.Description })],
            },
            ct);
}
