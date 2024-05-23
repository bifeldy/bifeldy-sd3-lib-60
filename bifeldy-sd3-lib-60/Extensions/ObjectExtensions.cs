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

        private static Dictionary<string, object> ConvertIEnumerableToDictionary(IEnumerable enumerable) {
            int index = 0;
            var items = new Dictionary<string, object>();
            foreach (object item in enumerable) {

                // If property is a string stop traversal
                if (item.GetType().IsPrimitive || item is string) {
                    items.Add(index.ToString(), item);
                }
                else if (item is IEnumerable enumerableItem) {
                    items.Add(index.ToString(), ConvertIEnumerableToDictionary(enumerableItem));
                }
                else {
                    var dictionary = item.ToDictionary();
                    items.Add(index.ToString(), dictionary);
                }

                index++;
            }

            items.Add("IsCollection", true);
            items.Add("Count", index);
            return items;
        }

        private static object ConvertPropertyToDictionary(PropertyInfo propertyInfo, object owner) {
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = propertyInfo.GetValue(owner);

            if (propertyValue is Type) {
                return propertyValue;
            }

            // If property is a collection don't traverse collection properties but the items instead
            if (!propertyType.Equals(typeof(string)) && typeof(IEnumerable).IsAssignableFrom(propertyType)) {
                return ConvertIEnumerableToDictionary((IEnumerable) propertyInfo.GetValue(owner));
            }

            // If property is a string stop traversal
            if (propertyType.IsPrimitive || propertyType.Equals(typeof(string))) {
                return propertyValue;
            }

            PropertyInfo[] properties = propertyType.GetProperties(bf);
            if (properties.Any()) {
                var resultDictionary = properties.ToDictionary(
                  subtypePropertyInfo => subtypePropertyInfo.Name,
                  subtypePropertyInfo => propertyValue == null ? null : ConvertPropertyToDictionary(subtypePropertyInfo, propertyValue)
                );
                resultDictionary.Add("IsCollection", false);
                return resultDictionary;
            }

            return propertyValue;
        }

        public static Dictionary<string, object> ToDictionary(this object instanceToConvert) {
            var resultDictionary = instanceToConvert.GetType()
              .GetProperties(bf)
                  .Where(propertyInfo => !propertyInfo.GetIndexParameters().Any())
                    .ToDictionary(
                        propertyInfo => propertyInfo.Name,
                        propertyInfo => ConvertPropertyToDictionary(propertyInfo, instanceToConvert)
                    );
            resultDictionary.Add("IsCollection", false);
            return resultDictionary;
        }

    }

}
