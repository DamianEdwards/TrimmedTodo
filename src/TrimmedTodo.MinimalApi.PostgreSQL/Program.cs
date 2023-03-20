//using Microsoft.AspNetCore.Authentication.JwtBearer;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddAuthentication()
//    .AddJwtBearer(JwtConfigHelper.ConfigureJwtBearer(builder));

//builder.Services.AddAuthorization();

//builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
//    .Validate<IHostEnvironment, ILoggerFactory>(JwtConfigHelper.ValidateJwtOptions,
//        "JWT options are not configured. Run 'dotnet user-jwts create' in project directory to configure JWT.")
//    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("TodoDb")
    ?? builder.Configuration["CONNECTION_STRING"]
    ?? "Server=localhost;Port=5432;User Id=TodosApp;Password=password;Database=Todos";
builder.Services.AddSingleton(_ =>
{
    var dataSourceBuilder = new NpgsqlSlimDataSourceBuilder(connectionString);
    return dataSourceBuilder.Build();
});
builder.Services.AddScoped(serviceProvider =>
{
    var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
    return dataSource.OpenConnection();
});

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(OpenApiExtensions.ConfigureSwaggerGen);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.AddContext<AppJsonSerializerContext>());

var app = builder.Build();

await EnsureDb(app.Services, app.Logger);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500))
        .ExcludeFromDescription();
}

//app.UseSwagger();
//app.UseSwaggerUI();

app.MapTodoApi();

await app.StartApp("/api/todos");

async Task EnsureDb(IServiceProvider services, ILogger logger)
{
    if (Environment.GetEnvironmentVariable("SUPPRESS_DB_INIT") != "true")
    {
        logger.LogInformation("Ensuring database exists and is up to date at connection string '{connectionString}'", ObscurePassword(connectionString));

        using var db = services.CreateScope().ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await db.OpenIfClosedAsync();
        var sql = $"""
                  CREATE TABLE IF NOT EXISTS public.todos
                  (
                      {nameof(Todo.Id)} SERIAL PRIMARY KEY,
                      {nameof(Todo.Title)} text NOT NULL,
                      {nameof(Todo.IsComplete)} boolean NOT NULL DEFAULT false
                  );

                  ALTER TABLE IF EXISTS public.todos
                      OWNER to "TodosApp";

                  DELETE FROM public.todos;
                  """;
        await db.ExecuteAsync(sql);
    }
    else
    {
        logger.LogInformation("Database initialization disabled for connection string '{connectionString}'", ObscurePassword(connectionString));
    }

    string ObscurePassword(string connectionString)
    {
        var passwordKey = "Password=";
        var passwordIndex = connectionString.IndexOf(passwordKey, 0, StringComparison.OrdinalIgnoreCase);
        if (passwordIndex < 0)
        {
            return connectionString;
        }
        var semiColonIndex = connectionString.IndexOf(";", passwordIndex, StringComparison.OrdinalIgnoreCase);
        return string.Concat(connectionString.AsSpan(0, passwordIndex + passwordKey.Length), "*****", semiColonIndex >= 0 ? connectionString[semiColonIndex..] : "");
    }
}
