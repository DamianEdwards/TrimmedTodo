using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtConfigHelper.ConfigureJwtBearer(builder));

builder.Services.AddAuthorization();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Validate<IHostEnvironment, ILoggerFactory>(JwtConfigHelper.ValidateJwtOptions,
        "JWT options are not configured. Run 'dotnet user-jwts create' in project directory to configure JWT.")
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("TodoDb")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? "Data Source=todos.db;Cache=Shared";
builder.Services.AddScoped(_ => new SqliteConnection(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ConfigureSwaggerGen);

var app = builder.Build();

await EnsureDb(app.Services, app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500))
        .ExcludeFromDescription();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapTodoApi();

await app.StartApp("/api/todos");

void ConfigureSwaggerGen(SwaggerGenOptions options)
{
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new()
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
}

async Task EnsureDb(IServiceProvider services, ILogger logger)
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_DB_INIT") != "true")
    {
        logger.LogInformation("Ensuring database exists at connection string '{connectionString}'", connectionString);

        using var db = services.CreateScope().ServiceProvider.GetRequiredService<SqliteConnection>();
        var sql = $"""
                  CREATE TABLE IF NOT EXISTS Todos (
                  {nameof(Todo.Id)} INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                  {nameof(Todo.Title)} TEXT NOT NULL,
                  {nameof(Todo.IsComplete)} INTEGER DEFAULT 0 NOT NULL CHECK({nameof(Todo.IsComplete)} IN (0, 1))
                  );
               """;
        await db.ExecuteAsync(sql);
    }
    else
    {
        Console.WriteLine($"Database initialization disabled for connection string '{connectionString}'");
    }
}
