using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5079));
}

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtConfigHelper.ConfigureJwtBearer(builder));

builder.Services.AddAuthorization();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Validate<IHostEnvironment, ILoggerFactory>(JwtConfigHelper.ValidateJwtOptions,
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

await app.StartApp("/api/todos");

async Task EnsureDb(IServiceProvider services, ILogger logger)
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_DB_INIT") != "true")
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
    else
    {
        Console.WriteLine($"Database initialization disabled for connection string '{connectionString}'");
    }
}
