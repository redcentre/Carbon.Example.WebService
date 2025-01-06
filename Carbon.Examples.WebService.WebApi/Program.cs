using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Carbon.Examples.WebService.Common;
using Carbon.Examples.WebService.Logging;
using Carbon.Examples.WebService.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RCS.Carbon.Licensing.Example;

#if SQL_PRODUCTION || SQL_TESTING
using RCS.Carbon.Licensing.Example;
#endif
using RCS.Carbon.Licensing.RedCentre;
using RCS.Carbon.Licensing.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var asm = typeof(Program).Assembly;
var an = asm.GetName();
var infoattr = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

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
	logTableName  ="ServiceLog3Test";
}
#endif

WebLog.Startup(builder.Configuration, storageConnect, logTableName);
WebLog.Info($"Start {an.Name} {an.Version}");

builder.Services.AddControllers(opt =>
{
	opt.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
}).AddXmlSerializerFormatters();

SessionManager.CacheSlidingSeconds = builder.Configuration.GetValue<int>("CarbonApi:SessionCacheSlideSeconds");

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Carbon Web API",
		Version = "v1",
		Description = $"REST style web service version {an.Version} (build {infoattr!.InformationalVersion}). This web service is under development by Red Centre Software. Access to the service requires a registered authorization key to be present in the request headers.",
		Contact = new OpenApiContact()
		{
			Name = "Red Centre Software",
			Url = new Uri("https://www.redcentresoftware.com/"),
			Email = "support@redcentresoftware.com"
		}
	});
	var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
	c.IncludeXmlComments(xmlFile);
	var dir = new DirectoryInfo(AppContext.BaseDirectory);
	foreach (var file in dir.GetFiles("RCS.Carbon.*.xml"))
	{
		c.IncludeXmlComments(file.FullName);
		WebLog.Info($"Include XML file {file.FullName}");
	}
	c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
	{
		Name = CarbonServiceClient.SessionIdHeaderKey,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "basic",
		In = ParameterLocation.Header,
		Description = "Simple authorisation using a request header."
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "basic"
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddControllers()
	.AddXmlSerializerFormatters();

// Different licensing providers with possibly different parameters are
// created depending on the build configuration.

#if (SQL_PRODUCTION || SQL_TESTING)
string prodkey = builder.Configuration["CarbonApi:ProductKey"]!;
string adoconnect = builder.Configuration["CarbonApi:AdoConnect"]!;
var licprov = new ExampleLicensingProvider(prodkey, adoconnect);
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
#elif (DEBUG || RELEASE)
//━━━━━━━━━━━━━ RCS DEBUGGING ━━━━━━━━━━━━━
//string licaddress = "http://localhost:52123/";
//string licaddress = "https://localhost:7238/";
string licaddress = "https://rcsapps.azurewebsites.net/licensing2test/";
string apiKey= builder.Configuration["CarbonApi:LicensingApiKey"]!;
int timeout = builder.Configuration.GetValue<int>("CarbonApi:LicensingTimeout");
var licprov = new RedCentreLicensingProvider(licaddress, apiKey, timeout);
//━━━━━━━━━━━━━ SQL DEBUGGING ━━━━━━━━━━━━━
//string prodkey = builder.Configuration["CarbonApi:ProductKey"]!;
//string adoconnect = builder.Configuration["CarbonApi:AdoConnect"]!;
//var licprov = new ExampleLicensingProvider(prodkey, adoconnect);
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
