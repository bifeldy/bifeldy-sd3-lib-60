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

        private static readonly BindingFlags bf = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

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
            var result = new Dictionary<string, object>();

            foreach (DictionaryEntry entry in dict) {
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
            var result = new Dictionary<string, object>();

            foreach (PropertyInfo prop in type.GetProperties(bf)) {
                if (!prop.CanRead) {
                    continue;
                }

                if (prop.GetIndexParameters().Any()) {
                    continue; // skip indexers
                }

                object value = prop.GetValue(obj);
                result[prop.Name] = ConvertObject(value);
            }

            result["IsCollection"] = false;

            return result;
        }

        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            return ConvertObject(instanceToConvert) as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

    }

}
