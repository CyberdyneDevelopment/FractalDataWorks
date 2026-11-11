using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Fdw.Services.Connections.Http.Tests.Paging;

/// <summary>
/// A remote API for the paging tests (FDW-4938): it holds rows 1..M and never returns more than N in a response, whatever is asked, and
/// pages them in the way it is built for. Every request it is sent is kept, so a test can check each carried the query's filter.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class CappedRemote : HttpMessageHandler
{
    private readonly int _rows;
    private readonly int _cap;
    private readonly string _way;

    /// <summary>Initializes a new instance of the <see cref="CappedRemote"/> class.</summary>
    /// <param name="rows">M, the rows it holds.</param>
    /// <param name="cap">N, the most rows it returns in a response.</param>
    /// <param name="way">How it pages: the paging style it answers to.</param>
    public CappedRemote(int rows, int cap, string way)
    {
        _rows = rows;
        _cap = cap;
        _way = way;
    }

    public List<Uri> Requests { get; } = [];

    /// <summary>Gets or sets whether it ignores the limit asked for and always returns twice the cap (a remote that does not honour the size).</summary>
    public bool IgnoresLimit { get; set; }

    /// <summary>Gets or sets whether its next link points back at the page it is on.</summary>
    public bool LinksToItself { get; set; }

    /// <summary>Gets or sets an origin its next link points to instead of its own.</summary>
    public string? LinksToOrigin { get; set; }

    /// <summary>Gets or sets whether it always says there is another page.</summary>
    public bool NeverEnds { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        Requests.Add(uri);
        var query = HttpUtility.ParseQueryString(uri.Query);

        int start;
        int asked;
        switch (_way)
        {
            case "OffsetLimit":
                start = Number(query["skip"], 0);
                asked = Number(query["top"], _cap);
                break;
            case "PageNumber":
                asked = Number(query["size"], _cap);
                start = (Number(query["p"], 1) - 1) * Math.Min(asked, _cap);
                break;
            case "SingleResponse":
                start = 0;
                asked = _rows;
                break;
            default:
                start = Number(query["at"], 0);
                asked = Number(query["per_page"], _cap);
                break;
        }

        var size = IgnoresLimit ? _cap * 2 : Math.Min(asked, Is("SingleResponse") ? _rows : _cap);
        var ids = Enumerable.Range(start + 1, Math.Max(0, Math.Min(size, _rows - start))).ToList();
        var end = start + ids.Count;
        var more = NeverEnds || end < _rows;
        var nextAddress = LinksToItself ? uri.AbsoluteUri
            : LinksToOrigin is not null ? LinksToOrigin + uri.PathAndQuery
            : Readdress(uri, end);

        var body = new Dictionary<string, object?> { ["rows"] = ids.Select(id => new Dictionary<string, object> { ["id"] = id }).ToList() };
        if (more && Is("BodyNextLink")) body["@odata.nextLink"] = nextAddress;
        if (more && Is("ContinuationToken")) body["meta"] = new Dictionary<string, object> { ["next_cursor"] = LinksToItself ? Number(query["at"], 0).ToString(CultureInfo.InvariantCulture) : end.ToString(CultureInfo.InvariantCulture) };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            RequestMessage = request,
        };

        // Prev is listed before next, and last after, so a parser that takes the first link follows the wrong one.
        if (Is("LinkHeader"))
        {
            var links = new List<string> { $"<{Readdress(uri, Math.Max(0, start - _cap))}>; rel=\"prev\"" };
            if (more) links.Add($"<{nextAddress}>; rel=\"next\"");
            links.Add($"<{Readdress(uri, _rows - (_rows % _cap))}>; rel=\"last\"");
            response.Headers.TryAddWithoutValidation("Link", string.Join(", ", links));
        }

        return Task.FromResult(response);
    }

    private bool Is(string way) => string.Equals(_way, way, StringComparison.Ordinal);

    private static int Number(string? text, int absent) => text is null ? absent : int.Parse(text, CultureInfo.InvariantCulture);

    // The remote's own links keep the query's other parameters and move its position parameter on.
    private static string Readdress(Uri uri, int at)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        query["at"] = at.ToString(CultureInfo.InvariantCulture);
        return new UriBuilder(uri) { Query = query.ToString() }.Uri.AbsoluteUri;
    }
}
