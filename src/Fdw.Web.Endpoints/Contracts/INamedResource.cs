namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Marker interface for resources identified by a unique name.
/// Used as a constraint on CRUD endpoint generic parameters.
/// </summary>
public interface INamedResource
{
    /// <summary>
    /// Gets the unique name that identifies this resource.
    /// </summary>
    string Name { get; }
}
