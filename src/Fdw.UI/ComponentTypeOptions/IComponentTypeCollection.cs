using System;
using System.Collections.Generic;
using System.Reflection;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.ComponentTypeOptions;

/// <summary>
/// A component collection a UI service type can drive through the three phases without naming it.
/// </summary>
/// <remarks>
/// Exists because <c>All()</c> is a generated STATIC on each derived collection, so no base and no
/// service type can call it generically.
/// </remarks>
public interface IComponentTypeCollection : IServiceTypeRegistration
{
    /// <summary>Gets or sets a value indicating whether this whole domain should be passed over.</summary>
    bool SkipRegistration { get; set; }

    /// <summary>Gets the data store this collection's configuration rows live in.</summary>
    /// <remarks>
    /// Declared here so a collection is a first-class member of a parent collection: the parent is
    /// a ServiceTypeCollection, and its members must satisfy IServiceTypeRegistration. These three
    /// are a property of a resource, not of a single component, which is why they belong at this level.
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

    /// <summary>Gets the components declared against this collection, skipped ones included.</summary>
    IEnumerable<IComponentTypeOption> Members { get; }

    /// <summary>
    /// Gets the assemblies holding the declared components.
    /// </summary>
    /// <remarks>
    /// Blazor discovers components per assembly, not per type, so a host calling
    /// <c>AddAdditionalAssemblies</c> needs this rather than the types. Distinct by construction:
    /// several components in one package yield one assembly, and Blazor throws
    /// "Assembly already defined" on a duplicate.
    /// </remarks>
    IEnumerable<Assembly> ComponentAssemblies { get; }



}
