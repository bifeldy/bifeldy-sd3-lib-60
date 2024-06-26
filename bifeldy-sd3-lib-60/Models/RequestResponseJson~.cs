﻿/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Template Kirim JSON
 *              :: Bisa Dipakai Untuk Inherit
 * 
 */

using System.Text.Json.Serialization;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;

namespace bifeldy_sd3_lib_60.Models {

    public class RequestJson {
        [JsonPropertyOrder(0)][SwaggerIgnore] public string secret { get; set; }
        [JsonPropertyOrder(1)][SwaggerIgnore] public string key { get; set; }
        [JsonPropertyOrder(2)][SwaggerIgnore] public string token { get; set; }
    }

    public abstract class ResponseJson<T> {
        [JsonPropertyOrder(0)] public string info { get; set; }
    }

    public class ResponseJsonSingle<T> : ResponseJson<T> {
        [JsonPropertyOrder(1)] public T result { get; set; }
    }

    public class ResponseJsonMulti<T> : ResponseJson<T> {
        [JsonPropertyOrder(2)] public IEnumerable<T> results { get; set; }
        [JsonPropertyOrder(3)] public decimal? pages { get; set; }
        [JsonPropertyOrder(4)] public decimal? count { get; set; }
    }

}
