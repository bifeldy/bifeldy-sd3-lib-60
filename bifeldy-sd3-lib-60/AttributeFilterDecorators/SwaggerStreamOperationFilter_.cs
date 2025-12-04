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

            (string contentType, OpenApiSchema schema) = this.BuildSchema(attr.Format);

            operation.RequestBody = new OpenApiRequestBody() {
                Description = schema.Description,
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>() {
                    {
                        contentType,
                        new OpenApiMediaType() {
                            Schema = schema,
                            Example = new OpenApiString("{ \"key\": \"1\" }\n{ \"key\": \"2\" }", true)
                        }
                    }
                },
                Extensions = {
                    ["x-bodyName"] = new OpenApiString("stream")
                }
            };
        }

        private (string, OpenApiSchema) BuildSchema(SwaggerStreamFormat format) {
            return format switch {
                SwaggerStreamFormat.Ndjson => (
                    "application/x-ndjson",
                    new OpenApiSchema {
                        Type = "string",
                        Description = "X-NDJSON (Newline Delimited JSON) streamed line-by-line"
                    }
                ),

                SwaggerStreamFormat.JsonArray => (
                    "application/json",
                    new OpenApiSchema {
                        Type = "array",
                        Description = "Streaming JSON array of objects",
                        Items = new OpenApiSchema {
                            Type = "object",
                            AdditionalPropertiesAllowed = true
                        }
                    }
                ),

                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

    }

}
