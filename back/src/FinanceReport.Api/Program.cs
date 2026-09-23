using FinanceReport.Api.Authentication;
using FinanceReport.Api.Errors;
using FinanceReport.Api.Logging;
using FinanceReport.Api.Middleware;
using FinanceReport.Application;
using FinanceReport.Infrastructure;
using FinanceReport.Infrastructure.Persistence;
using FinanceReport.Infrastructure.Serialization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

const long MaxRequestBodySize = 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);

// Réseau (TS §1.2) : écoute sur localhost uniquement, requêtes limitées à 1 Mo.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(builder.Configuration.GetValue("Api:Port", 5080));
    options.Limits.MaxRequestBodySize = MaxRequestBodySize;
});

builder.Services.AddSerilog(
    (_, logger) => LoggingSetup.Configure(logger, builder.Configuration, builder.Environment),
    preserveStaticLogger: true);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddJwtAuthentication();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services
    .AddControllers()
    .AddJsonOptions(options => JsonDefaults.Configure(options.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ErrorResponses.FromModelState);

var app = builder.Build();

await app.Services.GetRequiredService<ReferentialSeeder>().SeedAsync();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging(options => options.Logger = app.Services.GetRequiredService<Serilog.ILogger>());
app.UseStatusCodePages(context => ErrorResponses.WriteForStatusCodeAsync(context.HttpContext));

// Production locale (TS §1.2) : le front compilé, copié dans wwwroot, est servi par l'API.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

// Routes du front (/accounts, /dashboard…) : index.html, sauf sous /api où une route inconnue reste un 404.
app.MapFallbackToFile("{*path:nonfile:regex(^(?!api(/|$)).*$)}", "index.html");

await app.RunAsync();

public partial class Program;
