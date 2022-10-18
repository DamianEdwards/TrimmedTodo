using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

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

var connectionString = builder.Configuration.GetConnectionString("TodoDb")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? "Data Source=todos.db;Cache=Shared";
builder.Services.AddSqlite<TodoDb>(connectionString, null, options => options.UseModel(TrimmedTodo.MinimalApi.EfCore.Sqlite.CompiledModels.TodoDbModel.Instance))
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

app.UseSwagger();
app.UseSwaggerUI();

app.MapTodoApi();

await app.StartApp("/api/todos");

static async Task EnsureDb(string cs, IServiceProvider services, ILogger logger)
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_DB_INIT") != "true")
    {
        logger.LogInformation("Ensuring database exists and is up to date at connection string '{connectionString}'", cs);

        using var db = services.CreateScope().ServiceProvider.GetRequiredService<TodoDb>();
        await db.Database.MigrateAsync();
    }
    else
    {
        Console.WriteLine($"Database initialization disabled for connection string '{cs}'");
    }
}
