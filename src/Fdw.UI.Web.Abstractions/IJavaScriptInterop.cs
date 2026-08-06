using System.Threading.Tasks;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Generic JavaScript interop interface.
/// Implementations vary by framework (Blazor, Node.js, browser).
/// </summary>
public interface IJavaScriptInterop
{
    /// <summary>
    /// Invokes a JavaScript function and returns a value.
    /// </summary>
    /// <typeparam name="T">The type of value to return from the JavaScript function.</typeparam>
    /// <param name="identifier">The JavaScript function identifier to invoke.</param>
    /// <param name="args">Arguments to pass to the JavaScript function.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the JavaScript function.</returns>
    Task<T> Invoke<T>(string identifier, params object[] args);

    /// <summary>
    /// Invokes a JavaScript function without returning a value.
    /// </summary>
    /// <param name="identifier">The JavaScript function identifier to invoke.</param>
    /// <param name="args">Arguments to pass to the JavaScript function.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task InvokeVoid(string identifier, params object[] args);
}
