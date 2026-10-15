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

using System.Collections;
using System.Reflection;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class ObjectExtensions {

        // Perbaikan 1: Gunakan properti Instance saja untuk pemetaan kamus objek standar, 
        // hindari penggabungan Static secara liar yang bisa memicu tabrakan properti instans.
        private static readonly BindingFlags bf = BindingFlags.Public | BindingFlags.Instance;

        private static readonly HashSet<Type> ExtraSimpleTypes = new() {
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(Guid)
        };

        private static bool IsSimpleType(Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type.IsEnum || ExtraSimpleTypes.Contains(type);
        }

        private static object ConvertObject(object obj) {
            if (obj == null) {
                return null;
            }

            if (obj is Type) {
                return obj;
            }

            // Simple values (includes string)
            Type type = obj.GetType();
            if (IsSimpleType(type)) {
                return obj;
            }

            // Perbaikan 2: Amankan tipe biner (byte[] / byte?[]) agar tidak ikut terpecah di blok IEnumerable umum
            if (type.IsArray && (type.GetElementType() == typeof(byte) || Nullable.GetUnderlyingType(type.GetElementType()) == typeof(byte))) {
                return obj; // Pertahankan biner sebagai array byte utuh
            }

            if (obj is IDictionary dict) {
                return ConvertDictionary(dict);
            }

            // ICollection / IEnumerable but NOT string
            if (obj is IEnumerable enumerable) {
                return ConvertEnumerable(enumerable);
            }

            return ConvertComplexObject(obj);
        }

        private static Dictionary<string, object> ConvertDictionary(IDictionary dict) {
            // Perbaikan 3: Seragamkan penggunaan StringComparer agar konsisten di seluruh hierarki konversi kamus
            var result = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            foreach (DictionaryEntry entry in dict) {
                if (entry.Key == null) {
                    continue;
                }

                string key = entry.Key.ToString();
                result[key] = ConvertObject(entry.Value);
            }

            result["IsCollection"] = true;

            return result;
        }

        private static Dictionary<string, object> ConvertEnumerable(IEnumerable enumerable) {
            var result = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            int index = 0;
            foreach (object item in enumerable) {
                result[index.ToString()] = ConvertObject(item);
                index++;
            }

            result["IsCollection"] = true;
            result["Count"] = index;

            return result;
        }

        private static Dictionary<string, object> ConvertComplexObject(object obj) {
            Type type = obj.GetType();
            var result = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            foreach (PropertyInfo prop in type.GetProperties(bf)) {
                if (!prop.CanRead) {
                    continue;
                }

                if (prop.GetIndexParameters().Any()) {
                    continue; // skip indexers
                }

                object value = prop.GetValue(obj);

                // Amankan dari potensi duplikasi kunci properti ilegal
                result[prop.Name] = ConvertObject(value);
            }

            result["IsCollection"] = false;

            return result;
        }

        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            // Memastikan hasil balik utama selalu mengalir ke Dictionary root secara aman
            if (instanceToConvert == null) {
                return new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            }

            object converted = ConvertObject(instanceToConvert);
            if (converted is Dictionary<string, object> finalDict) {
                return finalDict;
            }

            // Jika input berupa tipe data primitif tunggal/string, bungkus ke dalam satu entry kamus standard
            var fallback = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase) {
                ["Value"] = converted,
                ["IsCollection"] = false
            };
            return fallback;
        }

    }

}
