using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5079));
}

builder.Authentication
    .AddJwtBearer(o =>
    {
        if (!builder.Environment.IsDevelopment())
        {
            var jwtKeyMaterialSecret = builder.Configuration.GetValue<string>("JWT_SIGNING_KEY");

            if (string.IsNullOrEmpty(jwtKeyMaterialSecret))
                throw new InvalidOperationException("JWT signing key not found!");

            var jwtKeyMaterial = Convert.FromBase64String(jwtKeyMaterialSecret);
            var jwtSigningKey = new SymmetricSecurityKey(jwtKeyMaterial);
            o.TokenValidationParameters.IssuerSigningKey = jwtSigningKey;
        }
    });

builder.Services.Configure<AuthenticationOptions>(o => o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme);

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Validate<IHostEnvironment, ILoggerFactory>(ValidateJwtOptions,
        "JWT options are not configured. Run 'dotnet user-jwts create' in project directory to configure JWT.");

var connectionString = builder.Configuration.GetConnectionString("TodoDb") ?? "Data Source=todos.db";
builder.Services.AddSqlite<TodoDb>(connectionString)
                .AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await EnsureDb(connectionString, app.Services, app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500))
        .ExcludeFromDescription();
}

//app.UseHttpsRedirection();

//app.UseAuthentication();
//app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapTodoApi();

app.Run();

static bool ValidateJwtOptions(JwtBearerOptions options, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
{
    var relevantOptions = new
    {
        Audience = options.Audience,
        ClaimsIssuer = options.ClaimsIssuer,
        Audiences = options.TokenValidationParameters.ValidAudiences,
        Issuers = options.TokenValidationParameters.ValidIssuers,
        IssuerSigningKey = options.TokenValidationParameters.IssuerSigningKey.ToString()
    };
    if ((relevantOptions.Audience is null && relevantOptions.Audiences is null)
        || (relevantOptions.ClaimsIssuer is null && relevantOptions.Issuers is null)
        || relevantOptions.IssuerSigningKey is null)
    {
        return false;
    }
    var logger = loggerFactory.CreateLogger(hostEnvironment.ApplicationName ?? nameof(Program));
    logger.LogInformation("JwtBearerAuthentication options configuration: {JwtOptions}", JsonSerializer.Serialize(relevantOptions));
    return true;
}

static async Task EnsureDb(string cs, IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Ensuring database exists and is up to date at connection string '{connectionString}'", cs);

    using var db = services.CreateScope().ServiceProvider.GetRequiredService<TodoDb>();
    await db.Database.MigrateAsync();
}
