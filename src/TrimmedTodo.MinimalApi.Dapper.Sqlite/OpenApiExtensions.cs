using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.Routing;

public static class OpenApiExtensions
{
    public static OpenApiOperation WithSecurityRequirementReference(this OpenApiOperation operation, string referenceId)
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new()
        {
            {
                new()
                {
                    Reference = new() { Type = ReferenceType.SecurityScheme, Id = referenceId }
                },
                Array.Empty<string>()
            }
        });
        return operation;
    }

    public static void ConfigureSwaggerGen(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new()
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme."
        });
    }
}
