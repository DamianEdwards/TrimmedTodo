using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Builder;

public static class JwtConfigHelper
{
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
        logger.LogInformation("JwtBearerAuthentication options configuration: {JwtOptions}",
            JsonSerializer.Serialize(relevantOptions, ProgramJsonSerializerContext.Default.JwtOptionsSummary));

        if ((string.IsNullOrEmpty(relevantOptions.Audience) && relevantOptions.Audiences?.Any() != true)
            || (relevantOptions.ClaimsIssuer is null && relevantOptions.Issuers?.Any() != true)
            || (relevantOptions.IssuerSigningKey is null && relevantOptions.IssuerSigningKeys?.Any() != true))
        {
            return false;
        }

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
