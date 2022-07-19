using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrimmedTodo.ApiClient;

[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(List<Todo>))]
[JsonSerializable(typeof(int))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
    public static SourceGenerationContext Web { get; } = new(new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
