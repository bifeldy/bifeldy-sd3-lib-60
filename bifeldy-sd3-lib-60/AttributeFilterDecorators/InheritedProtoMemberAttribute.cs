﻿/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Bikin Base Class Property (Kelas Parent) Di Inherited Derived Class (Kelas Child)
 *              :: https://gist.github.com/johncrim/06d8c832ec292cfbf0d3773ece247f27
 * 
 */

using System.Reflection;

using ProtoBuf;
using ProtoBuf.Meta;

namespace bifeldy_sd3_lib_60.AttributeFilterDecorators {

    [AttributeUsage(AttributeTargets.Property)] //  | AttributeTargets.Field)]
    public class InheritedProtoMemberAttribute : ProtoMemberAttribute {

        public InheritedProtoMemberAttribute(int tag) : base(tag) { }

        public static void AddInheritedMembersIn(RuntimeTypeModel protoModel = null) {
            protoModel ??= RuntimeTypeModel.Default;
            protoModel.AfterApplyDefaultBehaviour += (_, e) => {
                AddInheritedMembers(e.Type.BaseType, e.MetaType);
            };
        }

        private static void AddInheritedMembers(Type? type, MetaType metaType) {
            if ((type == null) || (type == typeof(object))) {
                return;
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (PropertyInfo property in properties) {
                InheritedProtoMemberAttribute protoMemberAttribute = property.GetCustomAttribute<InheritedProtoMemberAttribute>();
                if (protoMemberAttribute != null) {
                    ValueMember valueMember = metaType.AddField(protoMemberAttribute.Tag, property.Name);
                    valueMember.DataFormat = protoMemberAttribute.DataFormat;
                }
            }

            // Recursively Process Base Class
            AddInheritedMembers(type.BaseType, metaType);
        }
    }

}