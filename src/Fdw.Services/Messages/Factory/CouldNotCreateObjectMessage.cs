using System.Globalization;
using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that an object could not be created through reflection or factory methods.
/// The source generator will create FactoryMessages.CouldNotCreateObject(objectType) method.
/// </summary>
[Message("CouldNotCreateObject")]
public sealed class CouldNotCreateObjectMessage : FactoryMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CouldNotCreateObjectMessage"/> class with default message template.
    /// </summary>
    public CouldNotCreateObjectMessage()
        : base(2004, "CouldNotCreateObject", MessageSeverity.Error,
               "Could not create object of type {0}. This may be due to missing parameterless constructor, dependency injection configuration issues, or type instantiation constraints.", "COULD_NOT_CREATE_OBJECT")
    { }

    /// <summary>
    /// Initializes a new instance with the object type that could not be created.
    /// </summary>
    /// <param name="objectType">The type of object that could not be created.</param>
    public CouldNotCreateObjectMessage(string objectType)
        : base(2004, "CouldNotCreateObject", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Could not create object of type {0}. This may be due to missing parameterless constructor, dependency injection configuration issues, or type instantiation constraints.", objectType),
               "COULD_NOT_CREATE_OBJECT")
    { }

    /// <summary>
    /// Initializes a new instance with the object type and specific reason for creation failure.
    /// </summary>
    /// <param name="objectType">The type of object that could not be created.</param>
    /// <param name="reason">The specific reason why object creation failed.</param>
    public CouldNotCreateObjectMessage(string objectType, string reason)
        : base(2004, "CouldNotCreateObject", MessageSeverity.Error,
               string.Format(CultureInfo.InvariantCulture, "Could not create object of type {0}: {1}", objectType, reason),
               "COULD_NOT_CREATE_OBJECT")
    { }

}