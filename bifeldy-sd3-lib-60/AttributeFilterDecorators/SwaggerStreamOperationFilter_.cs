/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Filter Request & Response Stream
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    public sealed class SwaggerStreamOperationFilter : IOperationFilter {

        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            SwaggerStreamAttribute attr = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<SwaggerStreamAttribute>()
                .FirstOrDefault();

            if (attr == null) {
                return;
            }

            (string contentType, OpenApiSchema schema, IOpenApiAny example) = this.BuildSchema(attr.Format);

            operation.RequestBody = new OpenApiRequestBody() {
                Description = schema.Description,
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>() {
                    {
                        contentType,
                        new OpenApiMediaType() {
                            Schema = schema,
                            Example = example
                        }
                    }
                },
                Extensions = {
                    ["x-bodyName"] = new OpenApiString("stream")
                }
            };
        }

        private (string, OpenApiSchema, IOpenApiAny) BuildSchema(SwaggerStreamFormat format) {
            return format switch {
                SwaggerStreamFormat.Ndjson => (
                    "application/x-ndjson",
                    new OpenApiSchema {
                        Type = "string",
                        Description = "X-NDJSON (Newline Delimited JSON) Bisa di push line-by-line 1-1"
                    },
                    new OpenApiString("{ \"key\": \"1\" }\n{ \"key\": \"2\" }", true)
                ),

                SwaggerStreamFormat.JsonArray => (
                    "application/json",
                    new OpenApiSchema {
                        Type = "array",
                        Description = "Streaming JSON array of objects (Kirim seperti biasa)",
                        Items = new OpenApiSchema {
                            Type = "object",
                            AdditionalPropertiesAllowed = true
                        }
                    },
                    new OpenApiString("[\n  { \"key\": \"1\" }\n  { \"key\": \"2\" }\n]", true)
                ),

                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

    }

}
