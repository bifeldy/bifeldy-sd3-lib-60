/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Reflection;

using Microsoft.EntityFrameworkCore;

using bifeldy_sd3_lib_60.Abstractions;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class ModelBuilderExtensions {

        public static void RegisterAllEntities<BaseModel>(this ModelBuilder modelBuilder, params Assembly[] assemblies) {
            IEnumerable<Type> types = assemblies.SelectMany(a => a.GetExportedTypes()).Where(c => c.IsClass && !c.IsAbstract && c.IsPublic && typeof(EntityTable).IsAssignableFrom(c));
            foreach (Type type in types) {
                modelBuilder.Entity(type);
            }
        }

    }

}
