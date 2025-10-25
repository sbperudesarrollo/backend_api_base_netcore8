using System;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace backend_api_base_netcore8.Infrastructure.Swagger;

/// <summary>
/// Adds request/response examples for the auth login endpoint.
/// </summary>
public class AuthLoginOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!IsAuthLoginEndpoint(context))
        {
            return;
        }

        operation.Summary = "Autentica un usuario y emite un JWT.";

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Example = new OpenApiObject
                    {
                        ["username"] = new OpenApiString("admin"),
                        ["password"] = new OpenApiString("P@ssw0rd!")
                    }
                }
            }
        };

        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Login exitoso",
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Example = new OpenApiObject
                    {
                        ["token"] = new OpenApiString("<jwt>"),
                        ["expiresIn"] = new OpenApiInteger(3600),
                        //["user"] = new OpenApiObject
                        //{
                        //    ["id"] = new OpenApiInteger(1),
                        //    ["roleId"] = new OpenApiInteger(2),
                        //    ["name"] = new OpenApiString("Doe"),
                        //    ["firstName"] = new OpenApiString("John"),
                        //    ["email"] = new OpenApiString("john@acme.com"),
                        //    ["degreeId"] = new OpenApiInteger(3),
                        //    ["phone"] = new OpenApiLong(9999999999),
                        //    ["cip"] = new OpenApiLong(12345678)
                        //}
                    }
                }
            }
        };

        //operation.Responses["401"] = new OpenApiResponse
        //{
        //    Description = "Credenciales invalidas",
        //    Content =
        //    {
        //        ["application/json"] = new OpenApiMediaType
        //        {
        //            Example = new OpenApiObject
        //            {
        //                ["error"] = new OpenApiString("Invalid credentials")
        //            }
        //        }
        //    }
        //};
    }

    private static bool IsAuthLoginEndpoint(OperationFilterContext context)
    {
        var httpMethod = context.ApiDescription.HttpMethod;
        if (!string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relativePath = context.ApiDescription.RelativePath;
        return string.Equals(relativePath, "api/auth/login", StringComparison.OrdinalIgnoreCase);
    }
}
