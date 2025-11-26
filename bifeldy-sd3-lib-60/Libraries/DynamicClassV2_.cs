/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Bikin Kelas Secara Dinamis V2
* 
*/

using System.Collections;
using System.Dynamic;

namespace bifeldy_sd3_lib_60.Libraries {

    public sealed class CDynamicClassPropertyV2 {
        public string ColumnName { get; set; }
        public string TypeName { get; set; }
        public bool IsNullable { get; set; }
        public bool IsArray { get; set; }
        public bool IsList { get; set; }
        public bool IsDictionary { get; set; }
        public bool IsClass { get; set; }
    }

    public sealed class CDynamicClassV2 : DynamicObject {

        private readonly Dictionary<string, (Type Type, object Value, CDynamicClassPropertyV2 Meta)> _fields;

        public CDynamicClassV2(List<CDynamicClassPropertyV2> fields) {
            this._fields = new Dictionary<string, (Type, object, CDynamicClassPropertyV2)>(StringComparer.InvariantCultureIgnoreCase);

            foreach (CDynamicClassPropertyV2 f in fields) {
                Type type = Type.GetType(f.TypeName) ?? throw new Exception($"Unknown type '{f.TypeName}'");
                this._fields[f.ColumnName] = (type, null, f);
            }
        }

        private object ConvertValue(CDynamicClassPropertyV2 meta, Type type, object value) {
            if (value == null) {
                return null;
            }

            // Core type for Nullable<T>
            Type coreType = Nullable.GetUnderlyingType(type) ?? type;

            if (coreType.IsEnum) {
                return Enum.Parse(coreType, value.ToString(), ignoreCase: true);
            }

            if (meta.IsList) {
                var list = (IList)Activator.CreateInstance(type);
                Type innerType = type.GetGenericArguments()[0];

                if (value is IEnumerable source) {
                    foreach (object item in source) {
                        _ = list.Add(
                            this.ConvertValue(
                                new CDynamicClassPropertyV2() {
                                    TypeName = innerType.AssemblyQualifiedName
                                },
                                innerType,
                                item
                            )
                        );
                    }
                }

                return list;
            }

            if (meta.IsArray) {
                Type innerType = coreType.GetElementType();

                var items = ((IEnumerable)value).Cast<object>().ToList();
                var array = Array.CreateInstance(innerType, items.Count);

                for (int i = 0; i < items.Count; i++) {
                    array.SetValue(
                        this.ConvertValue(
                            new CDynamicClassPropertyV2() {
                                TypeName = innerType.AssemblyQualifiedName
                            },
                            innerType,
                            items[i]
                        ),
                        i
                    );
                }

                return array;
            }

            if (meta.IsDictionary) {
                Type keyType = type.GetGenericArguments()[0];
                Type valType = type.GetGenericArguments()[1];

                var dict = (IDictionary)Activator.CreateInstance(type);

                foreach (DictionaryEntry entry in (IDictionary)value) {
                    object key = this.ConvertValue(
                        new CDynamicClassPropertyV2 {
                            TypeName = keyType.AssemblyQualifiedName
                        },
                        keyType,
                        entry.Key
                    );

                    object val = this.ConvertValue(
                        new CDynamicClassPropertyV2 {
                            TypeName = valType.AssemblyQualifiedName
                        },
                        valType,
                        entry.Value
                    );

                    dict.Add(key, val);
                }

                return dict;
            }

            if (meta.IsClass && coreType != typeof(string)) {
                if (value is IDictionary<string, object> dict) {

                    var nestedMeta = dict.ToDictionary(
                        x => x.Key,
                        x => new CDynamicClassPropertyV2 {
                            ColumnName = x.Key,
                            TypeName = value.GetType().AssemblyQualifiedName,
                            IsClass = true
                        }
                    );

                    var nestedClass = new CDynamicClassV2(nestedMeta.Values.ToList());

                    foreach (KeyValuePair<string, object> kvp in dict) {
                        _ = nestedClass.TrySetMember(new SimpleSetBinder(kvp.Key), kvp.Value);
                    }

                    return nestedClass;
                }

                return value;
            }

            return Convert.ChangeType(value, coreType);
        }


        public override bool TrySetMember(SetMemberBinder binder, object value) {
            if (!this._fields.TryGetValue(binder.Name, out (Type Type, object Value, CDynamicClassPropertyV2 Meta) field)) {
                return false;
            }

            (Type type, object _, CDynamicClassPropertyV2 meta) = field;

            try {
                object converted = this.ConvertValue(meta, type, value);
                this._fields[binder.Name] = (type, converted, meta);
                return true;
            }
            catch (Exception ex) {
                throw new Exception($"Cannot set '{binder.Name}' with value '{value}': {ex.Message}", ex);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if (!this._fields.TryGetValue(binder.Name, out (Type Type, object Value, CDynamicClassPropertyV2 Meta) field)) {
                result = null;
                return false;
            }

            result = field.Value;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
            string key = indexes[0].ToString();

            if (this._fields.TryGetValue(key, out (Type Type, object Value, CDynamicClassPropertyV2 Meta) field)) {
                result = field.Value;
                return true;
            }

            throw new Exception($"Field '{key}' does not exist.");
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) {
            string key = indexes[0].ToString();

            if (!this._fields.ContainsKey(key)) {
                return false;
            }

            (Type type, object _, CDynamicClassPropertyV2 meta) = this._fields[key];
            object converted = this.ConvertValue(meta, type, value);

            this._fields[key] = (type, converted, meta);

            return true;
        }

        private class SimpleSetBinder : SetMemberBinder {
            public SimpleSetBinder(string name) : base(name, false) { }
            public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion) => null;
        }

    }

}
