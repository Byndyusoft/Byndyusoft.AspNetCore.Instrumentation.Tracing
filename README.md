# Byndyusoft.AspNetCore.Instrumentation.Tracing
ASP.NET Core MVC tracing.

[![(License)](https://img.shields.io/github/license/Byndyusoft/Byndyusoft.AspNetCore.Instrumentation.Tracing.svg)](LICENSE.txt)
[![Nuget](http://img.shields.io/nuget/v/Byndyusoft.AspNetCore.Instrumentation.Tracing.svg?maxAge=10800)](https://www.nuget.org/packages/Byndyusoft.AspNetCore.Instrumentation.Tracing/) [![NuGet downloads](https://img.shields.io/nuget/dt/Byndyusoft.AspNetCore.Instrumentation.Tracing.svg)](https://www.nuget.org/packages/Byndyusoft.AspNetCore.Instrumentation.Tracing/) 


## Installing

```shell
dotnet add package Byndyusoft.AspNetCore.Instrumentation.Tracing
```

## Usages

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddTracing();
        ...
    }
}
```

## Configuring

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
                .AddTracing(options =>
                {
                    options.TagRequestParamsInTrace = true;
                    options.EnrichLogsWithParams = true;
                    options.EnrichLogsWithHttpInfo = true;
                    options.ValueMaxStringLength = 50;
                    options.Formatter = new SystemTextJsonFormatter
                    {
                        Options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                        {
                            Converters =
                            {
                                new JsonStringEnumConverter()
                            }
                        }
                    };
                });
        ...
    }
}
```

There are some default option parameters:
1. EnrichTraceWithTaggedRequestParams — if true then http request params will be added to trace tags. Default: true. For more detail how params are extracted, refer to [page](https://github.com/Byndyusoft/Byndyusoft.Telemetry#object-telemetry-item-collector).
2. EnrichLogsWithParams — if true then every log will be enriched with request params during this request. Default: true. For more detail how params are extracted, refer to [page](https://github.com/Byndyusoft/Byndyusoft.Telemetry#object-telemetry-item-collector).
3. EnrichLogsWithHttpInfo — if true then every log will be enriched with request http info during this request. Default: true.
4. ValueMaxStringLength — maximum number of serialized request and response body string length. This string will be written to trace events and/or logs. The excess will be trimmed. Default: null - unlimited.
5. Formatter — it is used to serialize request and response body. Default: [SystemTextJsonFormatter](src/Byndyusoft.AspNetCore.Instrumentation.Tracing/Serialization/Json/SystemTextJsonFormatter.cs).

## MaskedSerialization

Masked serialization package [Byndyusoft.MaskedSerialization](https://github.com/Byndyusoft/Byndyusoft.MaskedSerialization) is used to hide sensitive data. Is it implemented in [NewtonsoftJsonFormatter.cs](https://github.com/Byndyusoft/Byndyusoft.AspNetCore.Instrumentation.Tracing/blob/master/src/Byndyusoft.AspNetCore.Instrumentation.Tracing/Serialization/Json/NewtonsoftJsonFormatter.cs) class.

# Contributing

To contribute, you will need to setup your local environment, see [prerequisites](#prerequisites). For the contribution and workflow guide, see [package development lifecycle](#package-development-lifecycle).

A detailed overview on how to contribute can be found in the [contributing guide](CONTRIBUTING.md).

## Prerequisites

Make sure you have installed all of the following prerequisites on your development machine:

- Git - [Download & Install Git](https://git-scm.com/downloads). OSX and Linux machines typically have this already installed.
- .NET Core (version 8.0 or higher) - [Download & Install .NET Core](https://dotnet.microsoft.com/download/dotnet-core/8.0).

## General folders layout

### src
- source code

### tests
- unit-tests

### example
- example application

## Package development lifecycle

- Implement package logic in `src`
- Add or addapt unit-tests (prefer before and simultaneously with coding) in `tests`
- Add or change the documentation as needed
- Open pull request in the correct branch. Target the project's `master` branch

# Maintainers

[github.maintain@byndyusoft.com](mailto:github.maintain@byndyusoft.com)
