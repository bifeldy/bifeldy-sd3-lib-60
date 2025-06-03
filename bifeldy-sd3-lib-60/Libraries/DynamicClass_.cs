/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Buat Bikin Kelas Secara Dinamis
* 
*/

using System.Dynamic;

namespace bifeldy_sd3_lib_60.Libraries {

    public sealed class CDynamicClassProperty {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
    }

    public sealed class CDynamicClass : DynamicObject {

        private readonly Dictionary<string, KeyValuePair<Type, object>> _fields;

        public CDynamicClass(List<CDynamicClassProperty> fields) {
            this._fields = new Dictionary<string, KeyValuePair<Type, object>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (CDynamicClassProperty field in fields) {
                var type = Type.GetType(field.DataType);
                object obj = null;

                if (field.IsNullable) {
                    type = typeof(Nullable<>).MakeGenericType(type);
                }
                else if (type.IsValueType) {
                    obj = Activator.CreateInstance(type);
                }

                this._fields.Add(field.ColumnName, new KeyValuePair<Type, object>(type, obj));
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value) {
            if (this._fields.ContainsKey(binder.Name)) {
                Type valueType = value.GetType();

                Type _type = this._fields[binder.Name].Key;
                Type targetType = Nullable.GetUnderlyingType(_type) ?? _type;

                if (valueType == targetType) {
                    this._fields[binder.Name] = new KeyValuePair<Type, object>(_type, value);
                    return true;
                }
                else {
                    throw new Exception($"Value {value} Is Not {targetType.FullName}");
                }
            }

            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            Type _type = this._fields[binder.Name].Key;
            Type targetType = Nullable.GetUnderlyingType(_type);

            result = targetType == null ? Activator.CreateInstance(_type) : this._fields[binder.Name].Value;

            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
            result = null;

            foreach(object idx in indexes) {
                string keyName = idx.ToString();
                if (this._fields.ContainsKey(keyName)) {
                    if (result == null) {
                        result = this._fields[keyName].Value;
                    }
                    else {
                        result = ((dynamic)result).Value;
                    }
                }
                else {
                    throw new Exception($"Key {keyName} Is Exists In {result.GetType().Name}");
                }
            }

            return true;
        }

    }

}
