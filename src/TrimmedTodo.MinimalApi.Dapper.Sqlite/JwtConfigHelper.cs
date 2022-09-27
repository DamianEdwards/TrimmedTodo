using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Builder;

public static class JwtConfigHelper
{
    /// <summary>
    /// Configures JWT Bearer to load the signing key from environment variable when not running in Development.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown when the signing key is not found in non-Development environments.</exception>
    public static Action<JwtBearerOptions> ConfigureJwtBearer(WebApplicationBuilder builder)
    {
        return o =>
        {
            if (!builder.Environment.IsDevelopment())
            {
                // When not running in development configure the JWT signing key from environment variable
                var jwtKeyMaterialValue = builder.Configuration["JWT_SIGNING_KEY"];

                if (string.IsNullOrEmpty(jwtKeyMaterialValue))
                    throw new InvalidOperationException("JWT signing key not found!");

                var jwtKeyMaterial = Convert.FromBase64String(jwtKeyMaterialValue);
                var jwtSigningKey = new SymmetricSecurityKey(jwtKeyMaterial);
                o.TokenValidationParameters.IssuerSigningKey = jwtSigningKey;
            }
        };
    }

    private const string JwtOptionsLogMessage = "JwtBearerAuthentication options configuration: {JwtOptions}";

    /// <summary>
    /// Validates that JWT Bearer authentication has been configured correctly.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="loggerFactory"></param>
    /// <returns><c>true</c> if required JWT Bearer settings are loaded, otherwise <c>false</c>.</returns>
    public static bool ValidateJwtOptions(JwtBearerOptions options, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
    {
        var relevantOptions = new JwtOptionsSummary
        {
            Audience = options.Audience,
            ClaimsIssuer = options.ClaimsIssuer,
            Audiences = options.TokenValidationParameters?.ValidAudiences,
            Issuers = options.TokenValidationParameters?.ValidIssuers,
            IssuerSigningKey = options.TokenValidationParameters?.IssuerSigningKey,
            IssuerSigningKeys = options.TokenValidationParameters?.IssuerSigningKeys
        };

        var logger = loggerFactory.CreateLogger(hostEnvironment.ApplicationName ?? nameof(Program));
        var jwtOptionsJson = JsonSerializer.Serialize(relevantOptions, ProgramJsonSerializerContext.Default.JwtOptionsSummary);

        if ((string.IsNullOrEmpty(relevantOptions.Audience) && relevantOptions.Audiences?.Any() != true)
            || (relevantOptions.ClaimsIssuer is null && relevantOptions.Issuers?.Any() != true)
            || (relevantOptions.IssuerSigningKey is null && relevantOptions.IssuerSigningKeys?.Any() != true))
        {
            logger.LogError(JwtOptionsLogMessage, jwtOptionsJson);
            return false;
        }

        logger.LogInformation(JwtOptionsLogMessage, jwtOptionsJson);
        return true;
    }
}


internal class JwtOptionsSummary
{
    public string? Audience { get; set; }
    public string? ClaimsIssuer { get; set; }
    public IEnumerable<string>? Audiences { get; set; }
    public IEnumerable<string>? Issuers { get; set; }
    public SecurityKey? IssuerSigningKey { get; set; }
    public IEnumerable<SecurityKey>? IssuerSigningKeys { get; set; }
}

[JsonSerializable(typeof(JwtOptionsSummary))]
internal partial class ProgramJsonSerializerContext : JsonSerializerContext
{
}
