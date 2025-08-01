﻿/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Template Kirim JSON & GRPCs
 *              :: Bisa Dipakai Untuk Inherit
 * 
 */

using System.Text.Json.Serialization;

using ProtoBuf;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Models {

    [ProtoContract]
    public class RequestJson {
        [ProtoMember(1)][InheritedProtoMember(1)][JsonPropertyOrder(0)][SwaggerHideJsonProperty] public string secret { get; set; }
        [ProtoMember(2)][InheritedProtoMember(2)][JsonPropertyOrder(1)][SwaggerHideJsonProperty] public string key { get; set; }
        [ProtoMember(3)][InheritedProtoMember(3)][JsonPropertyOrder(2)][SwaggerHideJsonProperty] public string token { get; set; }
    }

    [ProtoContract]
    public abstract class ResponseJson {
        [ProtoMember(1)][InheritedProtoMember(1)][JsonPropertyOrder(0)] public string info { get; set; }
    }

    [ProtoContract]
    public sealed class ResponseJsonSingle<T> : ResponseJson {
        [ProtoMember(2)][JsonPropertyOrder(1)] public T result { get; set; }
    }

    [ProtoContract]
    public sealed class ResponseJsonMulti<T> : ResponseJson {
        [ProtoMember(2)][JsonPropertyOrder(1)] public IEnumerable<T> results { get; set; }
        [ProtoMember(3)][JsonPropertyOrder(2)] public decimal? pages { get; set; }
        [ProtoMember(4)][JsonPropertyOrder(3)] public decimal? count { get; set; }
    }

    [ProtoContract]
    public class ResponseJsonMessage {
        [ProtoMember(1)][InheritedProtoMember(1)][JsonPropertyOrder(0)] public string message { get; set; }
    }

    [ProtoContract]
    public sealed class ResponseJsonErrorApiKeyIpOrigin : ResponseJsonMessage {
        [ProtoMember(2)][JsonPropertyOrder(1)][SwaggerHideJsonProperty] public string api_key { get; set; }
        [ProtoMember(3)][JsonPropertyOrder(2)][SwaggerHideJsonProperty] public string ip_origin { get; set; }
    }

    // Untuk Turunan
    // Kosongan Bisa Buat Kirim JWT Via Body (POST, PUT, PATCH)
    [ProtoContract]
    public class InputJson : RequestJson { }

    [ProtoContract]
    public class InputJsonHoDataSingle<T> : InputJson {
        [ProtoMember(4)][InheritedProtoMember(4)] public T data { get; set; }
    }

    [ProtoContract]
    public class InputJsonHoDataMulti<T> : InputJson {
        [ProtoMember(4)][InheritedProtoMember(4)] public T[] data { get; set; }
    }

    [ProtoContract]
    public class InputJsonKonsolidasiCbnDataSingle<T> : InputJsonHoDataSingle<T> { }

    [ProtoContract]
    public class InputJsonKonsolidasiCbnDataMulti<T> : InputJsonHoDataMulti<T> { }

    [ProtoContract]
    public class InputJsonDc : InputJson {
        [ProtoMember(4)][InheritedProtoMember(4)] public string kode_dc { get; set; }
    }

    [ProtoContract]
    public class InputJsonDcDataSingle<T> : InputJsonDc {
        [ProtoMember(5)][InheritedProtoMember(5)] public T data { get; set; }
    }

    [ProtoContract]
    public class InputJsonDcDataMulti<T> : InputJsonDc {
        [ProtoMember(5)][InheritedProtoMember(5)] public T[] data { get; set; }
    }

}
