using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace be.Infrastructure;
public sealed class AnonymousSwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // This OpenAPI serializer omits an empty list; an empty requirement survives
        // serialization and overrides the document's default Bearer requirement.
        if (context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()) operation.Security = new List<OpenApiSecurityRequirement> { new() };
        if (context.ApiDescription.RelativePath == "api/v1/payments/datatrans/webhook")
        {
            operation.Description = "Datatrans sandbox callback. JWT is not required. The exact raw JSON body must have a valid, fresh Datatrans HMAC signature; browser return parameters cannot confirm a payment.";
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter { Name = "Datatrans-Signature", In = ParameterLocation.Header, Required = true, Description = "t=Unix time in milliseconds,s0=hex HMAC-SHA256(timestamp UTF-8 followed by exact raw request bytes)", Schema = new OpenApiSchema { Type = "string" } });
            operation.RequestBody = new OpenApiRequestBody { Required = true, Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Schema = new OpenApiSchema { Type = "object", AdditionalPropertiesAllowed = true } } } };
        }
        if (context.ApiDescription.RelativePath == "api/v1/payments/zahls/webhook")
        {
            operation.Description = "Zahls TEST transaction callback. JWT is not required. The exact raw JSON body must have a valid X-Webhook-Signature; browser return parameters cannot confirm a payment.";
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter { Name = "X-Webhook-Signature", In = ParameterLocation.Header, Required = true, Description = "Lowercase hex HMAC-SHA256 over exact raw request-body bytes, keyed with the UTF-8 webhook signing key", Schema = new OpenApiSchema { Type = "string" } });
            operation.RequestBody = new OpenApiRequestBody { Required = true, Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Schema = new OpenApiSchema { Type = "object", AdditionalPropertiesAllowed = true } } } };
        }
    }
}
