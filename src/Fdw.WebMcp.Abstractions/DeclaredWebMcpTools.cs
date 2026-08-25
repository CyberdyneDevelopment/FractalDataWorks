using System;
using System.Collections.Generic;

namespace Fdw.WebMcp.Abstractions;

/// <summary>
/// The endpoints that declared themselves as WebMCP tools, for the host to serve.
/// </summary>
/// <remarks>
/// The WebMCP counterpart of <c>DeclaredEndpoints</c>, and deliberately the same shape: an option
/// adds itself as it registers, and the host reads the collection afterwards. Tools are GATHERED
/// from options that attached themselves — never swept out of an assembly list, which is what the
/// discovery this replaced did and why it found nothing: the routes it needed are declared inside
/// FastEndpoints <c>Configure()</c> bodies, which no assembly scan can read.
///
/// ORDERING: whatever calls <c>MapWebMcp</c> must run AFTER every domain's Register, or it reads an
/// empty collection and serves a script with no tools. <see cref="Count"/> exists so a caller can
/// assert it is non-zero and fail loudly instead.
/// </remarks>
public static class DeclaredWebMcpTools
{
    private static readonly List<WebMcpToolDeclaration> Declared = new();
    private static readonly System.Threading.Lock Gate = new();

    /// <summary>
    /// Gets the tool declarations gathered so far.
    /// </summary>
    public static IReadOnlyList<WebMcpToolDeclaration> Declarations
    {
        get
        {
            lock (Gate)
            {
                return Declared.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets how many endpoints have declared themselves as tools.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (Gate)
            {
                return Declared.Count;
            }
        }
    }

    /// <summary>
    /// Records an endpoint as a WebMCP tool.
    /// </summary>
    /// <param name="declaration">What the option declared about itself.</param>
    /// <remarks>
    /// Idempotent per endpoint type: an option reachable both directly and through its collection
    /// can register twice, and a tool offered to an agent twice is a duplicate name in the generated
    /// script. The guard belongs here rather than leaving every caller to remember it, exactly as it
    /// does on <c>DeclaredEndpoints.Declare</c>.
    /// </remarks>
    public static void Declare(WebMcpToolDeclaration declaration)
    {
        if (declaration is null)
        {
            throw new ArgumentNullException(nameof(declaration));
        }

        lock (Gate)
        {
            foreach (var existing in Declared)
            {
                if (existing.EndpointType == declaration.EndpointType)
                {
                    return;
                }
            }

            Declared.Add(declaration);
        }
    }
}
