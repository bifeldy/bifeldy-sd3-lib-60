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

        private readonly Dictionary<string, (Type Type, object Value)> _fields;

        public CDynamicClass(List<CDynamicClassProperty> fields) {
            _fields = new Dictionary<string, (Type, object)>(StringComparer.InvariantCultureIgnoreCase);

            foreach (CDynamicClassProperty field in fields) {
                var type = Type.GetType(field.DataType);

                if (type == null) {
                    throw new Exception($"Unknown data type '{field.DataType}'");
                }

                if (field.IsNullable && type.IsValueType) {
                    type = typeof(Nullable<>).MakeGenericType(type);
                }

                _fields[field.ColumnName] = (type, null);
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value) {
            if (!_fields.ContainsKey(binder.Name)) {
                return false;
            }

            (Type declaredType, object _) = _fields[binder.Name];

            Type underlying = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

            if (value == null) {
                if (Nullable.GetUnderlyingType(declaredType) != null) {
                    _fields[binder.Name] = (declaredType, null);
                    return true;
                }

                throw new Exception($"Cannot assign null to non-nullable field '{binder.Name}'");
            }

            try {
                object converted = Convert.ChangeType(value, underlying);
                _fields[binder.Name] = (declaredType, converted);
            }
            catch (Exception ex) {
                throw new Exception($"Cannot convert value '{value}' to type '{underlying.Name}'", ex);
            }

            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            if (_fields.TryGetValue(binder.Name, out (Type Type, object Value) field)) {
                if (field.Value != null) {
                    result = field.Value;
                }
                else if (field.Type.IsValueType) {
                    result = Activator.CreateInstance(field.Type);
                }
                else {
                    result = null;
                }

                return true;
            }

            result = null;

            return false;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
            if (indexes.Length != 1) {
                throw new ArgumentException("Only single index supported.");
            }

            string key = indexes[0].ToString();

            if (_fields.TryGetValue(key, out (Type Type, object Value) field)) {
                result = field.Value;
                return true;
            }

            throw new Exception($"Field '{key}' does not exist.");
        }

    }

}
