using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// An endpoint collection a service type can drive through the three phases without naming it.
/// </summary>
/// <remarks>
/// This interface exists because <c>All()</c> is a generated STATIC on each derived collection, so
/// no base and no service type can call it generically. A service type holding
/// <c>IEnumerable&lt;IEndpointTypeCollection&gt;</c> can cycle every resource it owns; without it,
/// every service type would have to name each collection by hand and would silently miss one added
/// later.
///
/// Each concrete collection satisfies <see cref="Members"/> by returning its own generated
/// <c>All()</c>. That one line per collection is the bridge between the static generated surface
/// and the polymorphism the registration sweep needs.
/// </remarks>
public interface IEndpointTypeCollection : IServiceTypeRegistration
{
    /// <summary>
    /// Gets or sets a value indicating whether this whole resource should be passed over.
    /// </summary>
    bool SkipRegistration { get; set; }

    /// <summary>Gets the data store this collection's configuration rows live in.</summary>
    /// <remarks>
    /// Declared here so a collection is a first-class member of a parent collection: the parent is
    /// a ServiceTypeCollection, and its members must satisfy IServiceTypeRegistration. These three
    /// are a property of a resource, not of a single endpoint, which is why they belong at this level.
    /// </remarks>
    /// <summary>Gets this collection's identity as its parent collection sees it.</summary>
    /// <remarks>
    /// Narrowed from ITypeOption's object. A parent collection keys its members by Guid, and the
    /// lookup it builds infers its key type from this property - left as object, the dictionary
    /// comes out keyed by object and does not match the Guid one the parent declares.
    /// </remarks>
    new Guid Id { get; }

    /// <summary>Gets the data store this collection's configuration rows live in.</summary>
    new string DataStore { get; }

    /// <summary>Gets the schema within that store.</summary>
    new string PathName { get; }

    /// <summary>Gets the table within that schema.</summary>
    new string Container { get; }

    /// <summary>
    /// Gets the endpoints declared against this collection, skipped ones included.
    /// </summary>
    /// <remarks>
    /// Filtering happens during the phases, not here, so a caller enumerating this sees everything
    /// declared — which is what a diagnostic surface listing "what exists and what is switched off"
    /// needs.
    /// </remarks>
    IEnumerable<IEndpointTypeOption> Members { get; }



}
