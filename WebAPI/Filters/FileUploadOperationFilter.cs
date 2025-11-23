using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebAPI.Filters;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || 
                       p.ParameterType == typeof(List<IFormFile>) ||
                       p.ParameterType == typeof(IFormFile[]))
            .ToArray();

        if (!fileParameters.Any())
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>(),
                        Required = new HashSet<string>()
                    }
                }
            }
        };

        var schema = operation.RequestBody.Content["multipart/form-data"].Schema;

        foreach (var param in context.MethodInfo.GetParameters())
        {
            if (param.ParameterType == typeof(IFormFile))
            {
                schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }
            else if (param.ParameterType == typeof(List<IFormFile>) || param.ParameterType == typeof(IFormFile[]))
            {
                schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                };
            }
            else if (param.GetCustomAttribute<FromFormAttribute>() != null)
            {
                schema.Properties[param.Name] = new OpenApiSchema
                {
                    Type = GetOpenApiType(param.ParameterType)
                };
            }
        }

        var parametersToRemove = operation.Parameters?
            .Where(p => fileParameters.Any(fp => fp.Name == p.Name))
            .ToList();

        if (parametersToRemove != null && operation.Parameters != null)
        {
            foreach (var param in parametersToRemove)
            {
                operation.Parameters.Remove(param);
            }
        }
    }

    private static string GetOpenApiType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long))
            return "integer";
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return "number";
        
        return "string";
    }
}