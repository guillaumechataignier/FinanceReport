using FinanceReport.Infrastructure;
using FinanceReport.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

await app.Services.GetRequiredService<ReferentialSeeder>().SeedAsync();

app.MapControllers();

await app.RunAsync();

public partial class Program;
