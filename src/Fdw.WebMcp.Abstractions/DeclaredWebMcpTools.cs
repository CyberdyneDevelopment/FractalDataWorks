using System;
using System.Collections.Generic;

namespace Fdw.WebMcp.Abstractions;

/// <summary>
/// The endpoints that declared themselves as WebMCP tools, for the host to serve.
/// </summary>
/// <remarks>
/// The WebMCP counterpart of <c>DeclaredEndpoints</c>, and deliberately the same shape: an option
/// adds itself as it registers, and the host reads the collection afterwards. Tools are GATHERED
/// from options that attached themselves — never scraped out of an assembly list, which is what the
/// discovery this replaced did and why it found nothing: the routes it needed are declared inside
/// FastEndpoints <c>Configure()</c> bodies, which no assembly scan can read.
///
/// ORDERING: the WebMcp registrant's Initialize (<c>WebMcpHosting</c>, deferred and run last) reads
/// this after every domain's Register; read earlier, it is an empty collection and the script serves
/// no tools. <see cref="Count"/> exists so a caller can
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
    /// Idempotent per NAME, not per endpoint type: an option reachable both directly and through
    /// its collection can register twice under its own name, and a tool offered to an agent twice
    /// is a duplicate name in the generated script — that is the actual failure this guards against,
    /// since <c>modelContext.registerTool</c> is keyed on name. Keying on endpoint type instead would
    /// also block the legitimate case of two differently-named tools deliberately sharing one
    /// endpoint (a preset field distinguishes them), and would miss the real bug of two unrelated
    /// endpoints accidentally choosing the same tool name.
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
                if (string.Equals(existing.Name, declaration.Name, StringComparison.Ordinal))
                {
                    return;
                }
            }

            Declared.Add(declaration);
        }
    }
}
