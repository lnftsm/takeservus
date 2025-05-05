using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace TakeServus.Api.Swagger;

public class SwaggerFileUploadFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Detect if method uses IFormFile
        var fileParams = context.MethodInfo
            .GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .ToList();

        if (!fileParams.Any()) return;

        // Clear existing parameters (Swagger tries to infer them incorrectly)
        operation.Parameters.Clear();

        // Re-add [FromRoute] parameters like jobId
        foreach (var param in context.MethodInfo.GetParameters())
        {
            if (param.ParameterType != typeof(IFormFile))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = param.Name!,
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" }
                });
            }
        }

        // Setup file upload schema
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>()
        };

        foreach (var param in fileParams)
        {
            schema.Properties[param.Name!] = new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            };
            schema.Required.Add(param.Name!);
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = schema
                }
            }
        };
    }
}