using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5079));
}

builder.Services.AddAuthentication()
    .AddJwtBearer(o =>
    {
        if (!builder.Environment.IsDevelopment())
        {
            // When not running in development configure the JWT signing key from environment variable
            var jwtKeyMaterialValue = builder.Configuration["JWT_SIGNING_KEY"];

            if (string.IsNullOrEmpty(jwtKeyMaterialValue))
                throw new InvalidOperationException("JWT signing key not found!");

            var jwtKeyMaterial = Convert.FromBase64String(jwtKeyMaterialValue);
            var jwtSigningKey = new SymmetricSecurityKey(jwtKeyMaterial);
            o.TokenValidationParameters.IssuerSigningKey = jwtSigningKey;
        }
    });

builder.Services.AddAuthorization();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Validate<IHostEnvironment, ILoggerFactory>(ValidateJwtOptions,
        "JWT options are not configured. Run 'dotnet user-jwts create' in project directory to configure JWT.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("TodoDb") ?? "Data Source=todos.db;Cache=Shared";
builder.Services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await EnsureDb(app.Services, app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500))
        .ExcludeFromDescription();
}

//app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.MapTodoApi();

app.Run();

async Task EnsureDb(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Ensuring database exists at connection string '{connectionString}'", connectionString);

    using var db = services.CreateScope().ServiceProvider.GetRequiredService<IDbConnection>();
    var sql = $"""
                  CREATE TABLE IF NOT EXISTS Todos (
                  {nameof(Todo.Id)} INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                  {nameof(Todo.Title)} TEXT NOT NULL,
                  {nameof(Todo.IsComplete)} INTEGER DEFAULT 0 NOT NULL CHECK({nameof(Todo.IsComplete)} IN (0, 1))
                  );
               """;
    await db.ExecuteAsync(sql);
}

static bool ValidateJwtOptions(JwtBearerOptions options, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
{
    var relevantOptions = new JwtOptionsSummary
    {
        Audience = options.Audience,
        ClaimsIssuer = options.ClaimsIssuer,
        Audiences = options.TokenValidationParameters?.ValidAudiences,
        Issuers = options.TokenValidationParameters?.ValidIssuers,
        IssuerSigningKey = options.TokenValidationParameters?.IssuerSigningKey.ToString()
    };
    if ((string.IsNullOrEmpty(relevantOptions.Audience) && relevantOptions.Audiences?.Any() != true)
        || (relevantOptions.ClaimsIssuer is null && relevantOptions.Issuers?.Any() != true)
        || string.IsNullOrEmpty(relevantOptions.IssuerSigningKey))
    {
        return false;
    }
    var logger = loggerFactory.CreateLogger(hostEnvironment.ApplicationName ?? nameof(Program));
    logger.LogInformation("JwtBearerAuthentication options configuration: {JwtOptions}",
        JsonSerializer.Serialize(relevantOptions, ProgramJsonSerializerContext.Default.JwtOptionsSummary));
    return true;
}

internal class JwtOptionsSummary
{
    public string? Audience { get; set; }
    public string? ClaimsIssuer { get; set; }
    public IEnumerable<string>? Audiences { get; set; }
    public IEnumerable<string>? Issuers { get; set; }
    public string? IssuerSigningKey { get; set; }
}

[JsonSerializable(typeof(JwtOptionsSummary))]
internal partial class ProgramJsonSerializerContext : JsonSerializerContext
{
}
