using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mealie.Api.Filters;

/// <summary>
/// Automatically adds HTTP 422 Unprocessable Entity response schema to all
/// POST, PUT, and PATCH operations, matching Python Pydantic validation error format.
/// </summary>
public class PydanticValidationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        if (method is not ("POST" or "PUT" or "PATCH")) return;

        operation.Responses.TryAdd("422", new OpenApiResponse
        {
            Description = "Validation Error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = "HTTPValidationError"
                        }
                    }
                }
            }
        });
    }
}
