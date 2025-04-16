# Overview

This repository contains a set of projects to support a comprehensive REST style web service that hosts and publishes Carbon cross-tabulation functionality.

> :star: These projects originally started as a simple example of how Carbon could be used in a .NET Web API, but they slowly grew to cover most of the Carbon API and became adopted for use in production software suites. However, the service may not be in a suitable shape for production use by other developers, but they are free to inspect the code and clone any code or techniques they might find useful to create their own services.

The [Carbon Overview][carbover] article explains how the Carbon libraries can be consumed by any type of .NET platform application as well as scripts and VS Code notebooks. This project demonstrates how Carbon can be hosted within a web service that follows REST conventions, making Carbon functionality available to any language or platform that supports REST style web services.

The service has some endpoints customised for [Python][pyorg] language clients. The Python software ecosystem provides many packages for complex data analysis, reporting and charting. The Carbon web service gives Python developers the ability to incorporate sophisticated Carbon cross-tabulation processing into their data analysis.

These projects began as small test harnesses to verify that Carbon operated correctly in a web hosting environment where performance stress is unpredictable and requests may arrive on multiple overlapping threads. Tests proved that multiple instances of the Carbon cross-tabulation engine can save and restore their *state* over different *sessions* in web service hosting. The projects have expanded to become a reasonably sophisticated web service to support more complex testing from scripts and VS Code notebooks.

The great majority of the codebase is boilerplate code *plumbing* to make a web service function, only a small subset of the code is involved in feeding request data into the Carbon API and sending it back as a response. The `DTO` folder contains all of the .NET classes that form the request and response contract. .NET clients may reference the [RCS.Carbon.Examples.WebService.Common][excommon] NuGet package which contains strongly-typed classes to bind to the web service.

The projects use [T4 templates][t4] to generate a large amount of repetitive boilerplate code for the web service implementation and the .NET service client class.

Red Centre Software has published a fully working versions of the example web service here:

| Uri | Description |
| --- | --- |
| <https://rcsapps.azurewebsites.net/carbon/swagger/> | RCS latest stable release |
| <https://rcsapps.azurewebsites.net/carbontest/swagger/> | RCS testing preview release |
| <https://bayesprice.azurewebsites.net/carbon/swagger/> | BayesPrice latest stable release |
| <https://bayesprice.azurewebsites.net/carbontest/swagger/> | BayesPrice testing preview release |


The RCS and BayesPrice deployments use different [licensing providers][licprov] internally for authentication.

---

## Database Endpoints

Version 9.0.11 of the service introduces some endpoints which implement a very simple schemaless database for storing keyed string values. The endpoints are:

```
POST /db/key1/key2 
GET /db/key1/key2  
DELETE /db/key1/key2  
GET /db/list?includeValues=true|false
```

The Swagger page describes the endpoints in more detail. Each internal row in the database is composed of 3 values: primary string key, secondary string key, string value. The primary and secondary string key values together form a unique compound key and each must be from 1 to 256 characters in length and any characters are acceptable. The string value may contain any characters and be null or up to approximately 1MB in length.

Clients are free to use the simple database endpoints to store application specific data in any way that is useful for them. Using a two-part compound key allows the unique keys to be divided into non-overlapping partitions if that is useful.

Note that the POST and GET endpoints process the request and response bodies as content-type `text/plain`. This allows the value strings to contain arbitrary data and avoid being interpreted or transformed in any way. Clients may of course use strings of JSON or XML (or ay other special format), but it is their responsibility to serialize and desrialize the strings correctly according to their conventions.

Note that the database does not internally store rows with null values. If you put a null value, then any existing row will be deleted, or if there is no existing row then nothing is saved.

Last updated: 25-Sep-2024

[carbover]: https://rcsapps.azurewebsites.net/doc/carbon/articles/overview.htm
[pyorg]: https://www.python.org/
[excommon]: https://www.nuget.org/packages/RCS.Carbon.Examples.WebService.Common
[t4]: https://learn.microsoft.com/en-us/visualstudio/modeling/code-generation-and-t4-text-templates?view=vs-2022
[licprov]: https://github.com/redcentre/Carbon.Examples.Licensing.Provider