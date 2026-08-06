using Fdw.Web.Http.Abstractions.EndPoints;

namespace Fdw.Web.Http.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="EndpointTypeBase"/> properties and constructor behavior.
/// Concrete types exercise the base class constructor and property assignments.
/// </summary>
public sealed class EndpointTypeBaseTests
{
    // --- CRUD endpoint ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudHasCorrectId()
    {
        var sut = new CrudEndpoint();

        sut.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudHasCorrectName()
    {
        var sut = new CrudEndpoint();

        sut.Name.ShouldBe("CrudEndpoint");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudRequiresAuthentication()
    {
        var sut = new CrudEndpoint();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudIsNotReadOnly()
    {
        var sut = new CrudEndpoint();

        sut.IsReadOnly.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudDoesNotSupportCaching()
    {
        var sut = new CrudEndpoint();

        sut.SupportsCaching.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudHasNullCacheDuration()
    {
        var sut = new CrudEndpoint();

        sut.DefaultCacheDurationSeconds.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudRequiresValidation()
    {
        var sut = new CrudEndpoint();

        sut.RequiresValidation.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudHasNoCacheCachingStrategy()
    {
        var sut = new CrudEndpoint();

        sut.CachingStrategy.ShouldBe("NoCache");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudHasAllHttpMethods()
    {
        var sut = new CrudEndpoint();

        sut.DefaultHttpMethods.ShouldBe(["GET", "POST", "PUT", "DELETE", "PATCH"]);
    }

    // --- Query endpoint ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryHasCorrectId()
    {
        var sut = new QueryEndpoint();

        sut.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryHasCorrectName()
    {
        var sut = new QueryEndpoint();

        sut.Name.ShouldBe("QueryEndpoint");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryDoesNotRequireAuthentication()
    {
        var sut = new QueryEndpoint();

        sut.RequiresAuthentication.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryIsReadOnly()
    {
        var sut = new QueryEndpoint();

        sut.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QuerySupportsCaching()
    {
        var sut = new QueryEndpoint();

        sut.SupportsCaching.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryHasFiveMinuteCacheDuration()
    {
        var sut = new QueryEndpoint();

        sut.DefaultCacheDurationSeconds.ShouldBe(300);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryDoesNotRequireValidation()
    {
        var sut = new QueryEndpoint();

        sut.RequiresValidation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryHasCacheCachingStrategy()
    {
        var sut = new QueryEndpoint();

        sut.CachingStrategy.ShouldBe("Cache");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryHasGetOnly()
    {
        var sut = new QueryEndpoint();

        sut.DefaultHttpMethods.ShouldBe(["GET"]);
    }

    // --- Command endpoint ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandHasCorrectId()
    {
        var sut = new CommandEndpoint();

        sut.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandHasCorrectName()
    {
        var sut = new CommandEndpoint();

        sut.Name.ShouldBe("CommandEndpoint");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandRequiresAuthentication()
    {
        var sut = new CommandEndpoint();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandIsNotReadOnly()
    {
        var sut = new CommandEndpoint();

        sut.IsReadOnly.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandHasPostPutPatchMethods()
    {
        var sut = new CommandEndpoint();

        sut.DefaultHttpMethods.ShouldBe(["POST", "PUT", "PATCH"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandRequiresValidation()
    {
        var sut = new CommandEndpoint();

        sut.RequiresValidation.ShouldBeTrue();
    }

    // --- EventEndpoint ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EventEndpointHasCorrectId()
    {
        var sut = new EventEndpoint();

        sut.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EventEndpointHasCorrectName()
    {
        var sut = new EventEndpoint();

        sut.Name.ShouldBe("EventEndpoint");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EventEndpointRequiresAuthentication()
    {
        var sut = new EventEndpoint();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EventEndpointHasPostAndGetMethods()
    {
        var sut = new EventEndpoint();

        sut.DefaultHttpMethods.ShouldBe(["POST", "GET"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EventEndpointIsNotReadOnly()
    {
        var sut = new EventEndpoint();

        sut.IsReadOnly.ShouldBeFalse();
    }

    // --- File endpoint ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FileHasCorrectId()
    {
        var sut = new FileEndpoint();

        sut.Id.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FileHasCorrectName()
    {
        var sut = new FileEndpoint();

        sut.Name.ShouldBe("FileEndpoint");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FileRequiresAuthentication()
    {
        var sut = new FileEndpoint();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FileHasCrudHttpMethods()
    {
        var sut = new FileEndpoint();

        sut.DefaultHttpMethods.ShouldBe(["GET", "POST", "PUT", "DELETE"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FileRequiresValidation()
    {
        var sut = new FileEndpoint();

        sut.RequiresValidation.ShouldBeTrue();
    }

    // --- Health endpoint ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthHasCorrectId()
    {
        var sut = new HealthEndpoint();

        sut.Id.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthHasCorrectName()
    {
        var sut = new HealthEndpoint();

        sut.Name.ShouldBe("HealthEndpoint");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthDoesNotRequireAuthentication()
    {
        var sut = new HealthEndpoint();

        sut.RequiresAuthentication.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthIsReadOnly()
    {
        var sut = new HealthEndpoint();

        sut.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthDoesNotSupportCaching()
    {
        var sut = new HealthEndpoint();

        sut.SupportsCaching.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthDoesNotRequireValidation()
    {
        var sut = new HealthEndpoint();

        sut.RequiresValidation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthHasGetOnly()
    {
        var sut = new HealthEndpoint();

        sut.DefaultHttpMethods.ShouldBe(["GET"]);
    }

}
