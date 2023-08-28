# Overview

This repository contains a set of projects to support a comprehensive REST style web service that hosts and publishes Carbon cross-tabulation functionality.

The [Carbon Overview][carbover] article explains how the Carbon libraries can be consumed by any type of .NET platform application as well as scripts and VS Code notebooks. This project demonstrates how Carbon can be hosted within a web service that follows REST conventions, making Carbon functionality available to any language or platform that supports REST style web services.

The service has some endpoints customised for [Python][pyorg] language clients. The Python software ecosystem provides many packages for complex data analysis, reporting and charting. The Carbon web service gives Python developers the ability to incorporate sophisticated Carbon cross-tabulation processing into their data analysis.

These projects began as small test harnesses to verify that Carbon operated correctly in a web hosting environment where performance stress is unpredictable and requests may arrive on multiple overlapping threads. Tests proved that multiple instances of the Carbon cross-tabulation engine can save and restore their *state* over different *sessions* in web service hosting. The projects have expanded to become a reasonably sophisticated web service to support more complex testing from scripts and VS Code notebooks. It is not intended that customers take this repository *as-is* and attempt to convert it into a real product, it only demonstrates how the functionality of the Carbon engine can be hosted and published as a web service.

The great majority of the codebase is boilerplate code *plumbing* to make a web service function, only a small subset of the code is involved in feeding request data into the Carbon API and sending it back as a response. The `DTO` folder contains all of the .NET classes that form the request and response contract. .NET clients may reference the [RCS.Carbon.Examples.WebService.Common][excommon] NuGet package which contains strongly-typed classes to bind to the web service.

> :star: The projects use [T4 templates][t4] to generate a large amount of repetitive boilerplate code for the web service implementation and the .NET service client class.

Red Centre Software has published a fully working version of the example web service here:

<https://rcsapps.azurewebsites.net/carbon/swagger/>

Last updated: 27-Apr-2023

[carbover]: https://rcsapps.azurewebsites.net/doc/carbon/articles/overview.htm
[pyorg]: https://www.python.org/
[excommon]: https://www.nuget.org/packages/RCS.Carbon.Examples.WebService.Common
[t4]: https://learn.microsoft.com/en-us/visualstudio/modeling/code-generation-and-t4-text-templates?view=vs-2022