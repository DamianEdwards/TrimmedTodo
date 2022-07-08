using Microsoft.EntityFrameworkCore;

namespace TrimmedTodo.WebApi.EfCore.Sqlite.Data;

public class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options)
    {
        Todos = Set<Todo>();
    }

    public DbSet<Todo> Todos { get; }
}
