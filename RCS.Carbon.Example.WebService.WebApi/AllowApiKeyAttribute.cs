using System;

namespace RCS.Carbon.Example.WebService.WebApi;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
sealed class AllowApiKeyAttribute : Attribute
{
}
