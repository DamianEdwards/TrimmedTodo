using System.ComponentModel.DataAnnotations;

namespace TrimmedTodo.WebApi.EfCore.Sqlite.Data;

public class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}
