/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 *              :: Tidak Bisa Pakai Insert, Update, Delete Jika Tidak Ada Key / Keyless
 * 
 */

using System.ComponentModel.DataAnnotations;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using bifeldy_sd3_lib_60.Abstractions;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class ModelBuilderExtensions {

        public static void RegisterAllEntities<BaseModel>(this ModelBuilder modelBuilder, params Assembly[] assemblies) {
            IEnumerable<Type> types = assemblies.SelectMany(a => a.GetTypes())
                .Where(c => c.IsClass && !c.IsAbstract && c.IsPublic && typeof(EntityTableView).IsAssignableFrom(c));

            foreach (Type type in types) {
                string[] orderedKeys = type.GetProperties()
                    .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute)))
                    // .OrderBy(p => p.CustomAttributes
                    //     .Single(x => x.AttributeType == typeof(ColumnAttribute))?
                    //     .NamedArguments?
                    //     .Single(y => y.MemberName == nameof(ColumnAttribute.Order))
                    //     .TypedValue.Value ?? 0
                    // )
                    .Select(x => x.Name)
                    .ToArray();
                EntityTypeBuilder entity = modelBuilder.Entity(type);
                if (orderedKeys.Length > 0) {
                    _ = entity.HasKey(orderedKeys);
                }
            }
        }

    }

}
