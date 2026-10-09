Done: separate Gateway project (src/Gateway) hosts the ReverseProxy

    Token including validation
    YARP (Yet Another Reverse Proxy): In the .NET ecosystem, Microsoft's YARP middleware
    Client → YARP Gateway → Microservice A B

Build a Decorator pattern:
using either Scrutor or build.Services.AddScope<I>

add a SKILL.md


Redesign CQRS with Milan https://milanjovanovic.tech/blog/cqrs-pattern-the-way-it-should-have-been-from-the-start

Service:
* Sensitive Data Logs: https://learn.microsoft.com/en-us/dotnet/core/extensions/data-redaction?tabs=dotnet-cli
* Masking Production Errors:  https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0
* In the Development environment, ASP.NET Core turns on ValidateScopes and ValidateOnBuild
``` so the issue will detect in CI
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;    // throws when a scoped service is resolved from the root
    options.ValidateOnBuild = true;   // checks the whole graph at startup
});
```

UI:
* Auto Sign user out after the site is inactive for 5 minutes
* Add SignlaR capability to the chat submit button
