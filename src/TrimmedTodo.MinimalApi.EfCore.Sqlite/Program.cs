using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5079));
}

builder.Authentication.AddJwtBearer(o =>
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

var connectionString = builder.Configuration.GetConnectionString("TodoDb") ?? "Data Source=todos.db";
builder.Services.AddSqlite<TodoDb>(connectionString)
                .AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await EnsureDb(app.Services, app.Logger);
EnsureJwt(app.Services, app.Logger);

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
    logger.LogInformation("Ensuring database exists and is up to date at connection string '{connectionString}'", connectionString);

    using var db = services.CreateScope().ServiceProvider.GetRequiredService<TodoDb>();
    await db.Database.MigrateAsync();
}

void EnsureJwt(IServiceProvider services, ILogger logger)
{
    var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
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
        throw new InvalidOperationException("JWT options are not configured. Run 'dotnet user-jwts create' in project directory to configure JWT.");
    }
    logger.LogInformation("JwtBearerAuthentication options configuration: {JwtOptions}", JsonSerializer.Serialize(relevantOptions));
}
