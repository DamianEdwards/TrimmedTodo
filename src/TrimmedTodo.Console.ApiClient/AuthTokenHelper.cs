using System.Diagnostics;

namespace TrimmedTodo.ApiClient;

internal static class AuthTokenHelper
{
    public static string GetAuthToken()
    {
        var tokenFileName = ".authtoken";
        var tokenFilePath = Path.Combine(AppContext.BaseDirectory, tokenFileName);
        if (!File.Exists(tokenFilePath))
        {
            throw new InvalidOperationException(
                $"File '{tokenFileName}' not found. Run 'dotnet user-jwts create --role admin' " +
                $"in the API project directory to create an auth token and save it in a file named '{tokenFileName}' " +
                $"in the {Process.GetCurrentProcess().ProcessName} project directory.");
        }
        var token = File.ReadAllText(tokenFilePath).Trim();
        return token;
    }
}
