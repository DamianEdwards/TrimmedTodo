var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.AddConsole();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await app.StartApp();
