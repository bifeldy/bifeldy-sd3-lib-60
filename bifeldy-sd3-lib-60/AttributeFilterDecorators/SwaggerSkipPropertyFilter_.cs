/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Filter Terima Content Type
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Reflection;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    public sealed class SwaggerHideJsonPropertyFilter : ISchemaFilter {

        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            if (schema?.Properties == null) {
                return;
            }

            IEnumerable<PropertyInfo> skipProperties = context.Type.GetProperties().Where(t => t.GetCustomAttribute<SwaggerHideJsonPropertyAttribute>() != null);

            foreach (PropertyInfo skipProperty in skipProperties) {
                string propertyToSkip = schema.Properties.Keys.SingleOrDefault(x => x.ToUpper() == skipProperty.Name.ToUpper());

                if (propertyToSkip != null) {
                    _ = schema.Properties.Remove(propertyToSkip);
                }
            }
        }

    }

}
