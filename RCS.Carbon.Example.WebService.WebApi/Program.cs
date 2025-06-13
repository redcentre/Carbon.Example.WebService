using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using RCS.Carbon.Example.WebService.Common;
using RCS.Carbon.Example.WebService.Logging;
using RCS.Carbon.Example.WebService.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RCS.Licensing.Provider.Shared;
using RCS.Licensing.Example.Provider;

#if SQL_PRODUCTION || SQL_TESTING
#else
using RCS.Licensing.Provider;
#endif
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var asm = typeof(Program).Assembly;
var an = asm.GetName();
var buildTime = asm.GetCustomAttributes<AssemblyMetadataAttribute>().First(a => a.Key == "BuildTime")!.Value;

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
	});
});
builder.Host.UseSerilog();
string storageConnect = builder.Configuration["CarbonApi:ApplicationStorageConnect"]!;
#if (SQL_PRODUCTION || RCS_PRODUCTION)
string? logTableName = builder.Configuration["CarbonApi:LogTableName"];
if (string.IsNullOrEmpty(logTableName))
{
	logTableName  ="ServiceLog3";
}
#else
string? logTableName = builder.Configuration["CarbonApi:LogTestTableName"];
if (string.IsNullOrEmpty(logTableName))
{
	logTableName = "ServiceLog3Test";
}
#endif

WebLog.Startup(builder.Configuration, storageConnect, logTableName);
WebLog.Info($"Start {an.Name} {an.Version}");

builder.Services.AddControllers(opt =>
{
	opt.OutputFormatters.RemoveType<XmlSerializerOutputFormatter>();
	opt.OutputFormatters.RemoveType<StringOutputFormatter>();
	opt.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
	opt.OutputFormatters.Add(new TextPlainOutputFormatter());
	opt.InputFormatters.Add(new TextPlainInputFormatter());
});

SessionManager.CacheSlidingSeconds = builder.Configuration.GetValue<int>("CarbonApi:SessionCacheSlideSeconds");

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
string swaggerTitle = builder.Configuration["CarbonApi:SwaggerTitle"]!;
string swaggerDesc = builder.Configuration["CarbonApi:SwaggerDesc"]!;
string swaggerName = builder.Configuration["CarbonApi:SwaggerName"]!;
string swaggerEmail = builder.Configuration["CarbonApi:SwaggerEmail"]!;
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = swaggerTitle,
		Version = "v1",
		Description = string.Format(swaggerDesc, an.Version, buildTime),
		Contact = new OpenApiContact()
		{
			Name = swaggerName,
			Url = new Uri("https://www.redcentresoftware.com/"),
			Email = swaggerEmail
		}
	});
	var dir = new DirectoryInfo(AppContext.BaseDirectory);
	foreach (var file in dir.GetFiles("RCS.*.xml"))
	{
		c.IncludeXmlComments(file.FullName);
		WebLog.Info($"Include XML file {file.FullName}");
	}
	c.AddSecurityDefinition("session", new OpenApiSecurityScheme
	{
		Name = CarbonServiceClient.SessionIdHeaderKey,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "basic",
		In = ParameterLocation.Header,
		Description = "Authorisation using a Session Id in a request header."
	});
	c.AddSecurityDefinition("apikey", new OpenApiSecurityScheme
	{
		Name = CarbonServiceClient.ApiKeyHeaderKey,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "basic",
		In = ParameterLocation.Header,
		Description = "Authorisation using an Api Key in a request header."
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "session"
				}
			},
			Array.Empty<string>()
		}
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "apikey"
				}
			},
			Array.Empty<string>()
		}
	});
});

// Different licensing providers with possibly different parameters are
// created depending on the build configuration.

#if (SQL_PRODUCTION || SQL_TESTING)
string adoconnect = builder.Configuration["CarbonApi:AdoConnect"]!;
string productKey = builder.Configuration["CarbonApi:ProductKey"]!;
var licprov = new ExampleLicensingProvider(adoconnect, productKey);
#elif RCS_PRODUCTION
int timeout = builder.Configuration.GetValue<int>("CarbonApi:LicensingTimeout");
string? licaddress = builder.Configuration["CarbonApi:LicensingBaseAddress"];
if (string.IsNullOrEmpty(licaddress))
{
	licaddress = "https://rcsapps.azurewebsites.net/licensing2/";
}
var licprov = new RedCentreLicensingProvider(licaddress, null, timeout);
#elif RCS_TESTING
int timeout = builder.Configuration.GetValue<int>("CarbonApi:LicensingTimeout");
string? licaddress = builder.Configuration["CarbonApi:LicensingTestBaseAddress"];
if (string.IsNullOrEmpty(licaddress))
{
	licaddress = "https://rcsapps.azurewebsites.net/licensing2test/";
}
var licprov = new RedCentreLicensingProvider(licaddress, null, timeout);
#elif (DEBUG || DEBUG_CARBON || RELEASE)
// ┌───────────────────────────────────────────────────────────────┐
// │  In local debug or release configuration it's necessary to    │
// │  manually choose the provider its parameters.                 │
// └───────────────────────────────────────────────────────────────┘
//━━━━━━━━━━━━━ RCS DEBUGGING ━━━━━━━━━━━━━
//string licaddress = "http://localhost:52123/";
//string licaddress = "https://localhost:7238/";
string licaddress = "https://rcsapps.azurewebsites.net/licensing2test/";
string apiKey = builder.Configuration["CarbonApi:LicensingApiKey"]!;
int timeout = builder.Configuration.GetValue<int>("CarbonApi:LicensingTimeout");
var licprov = new RedCentreLicensingProvider(licaddress, apiKey, timeout);
//━━━━━━━━━━━━━ SQL DEBUGGING ━━━━━━━━━━━━━
//string adoconnect = builder.Configuration["CarbonApi:AdoConnect"]!;
//string productKey = builder.Configuration["CarbonApi:ProductKey"]!;
//var licprov = new ExampleLicensingProvider(adoconnect, productKey);
//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#else
#error "No recognised licensing provider defined"
#endif

builder.Services.AddSingleton<ILicensingProvider>(licprov);

var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(() =>
{
	WebLog.Info("Application stopped");
	WebLog.Shutdown();
});

app.UseCors();

SessionManager.Load();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
	//app.UseDeveloperExceptionPage();
	app.UseExceptionHandler("/error");  // <------ Use this to see live error handling when debugging
}
else
{
	app.UseExceptionHandler("/error");
}

//app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

RCS.Carbon.Shared.Log.CarbonLog += (s, e) =>
{
	System.Diagnostics.Trace.WriteLine($"{s} {e.Level} {e.Message} {e.Error?.Message}");
	// We have to crack the Carbon logging into app logging.
	switch (e.Level)
	{
		case RCS.Carbon.Shared.LogLevel.Trace:
			WebLog.Trace(e.Message);
			break;
		case RCS.Carbon.Shared.LogLevel.Debug:
			WebLog.Debug(e.Message);
			break;
		case RCS.Carbon.Shared.LogLevel.Info:
			WebLog.Info(e.Message);
			break;
		case RCS.Carbon.Shared.LogLevel.Warn:
			WebLog.Warn(e.Message);
			break;
		case RCS.Carbon.Shared.LogLevel.Error:
			WebLog.Error(e.Error, e.Message);
			break;
		case RCS.Carbon.Shared.LogLevel.Critical:
			WebLog.Fatal(e.Error, e.Message);
			break;
	}
};

app.Run();
