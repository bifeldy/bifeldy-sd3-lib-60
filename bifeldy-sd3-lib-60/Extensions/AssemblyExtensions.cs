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

using System.Globalization;
using System.Reflection;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class AssemblyExtensions {

        //
        // .csproj
        //
        // <PropertyGroup>
        //     <SourceRevisionId>build$([System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss:fffZ"))</SourceRevisionId>
        // </PropertyGroup>

        private const string BUILD_VERSION_METADATA_PREFIX = "+build";

        public static DateTime? GetLinkerBuildTime(this Assembly assembly) {

            AssemblyInformationalVersionAttribute attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null) {
                string value = attribute.InformationalVersion;
                int index = value.IndexOf(BUILD_VERSION_METADATA_PREFIX);
                if (index > 0) {
                    value = value[(index + BUILD_VERSION_METADATA_PREFIX.Length)..];
                    return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture);
                }
            }

            return null;
        }

    }

}
