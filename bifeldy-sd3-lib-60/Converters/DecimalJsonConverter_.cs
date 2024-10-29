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

using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Converters {

    public sealed class DecimalSystemTextJsonConverter : System.Text.Json.Serialization.JsonConverter<decimal> {
                
        public override decimal Read(
            ref System.Text.Json.Utf8JsonReader reader,
            Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options
        ) {
            return reader.GetDecimal().RemoveTrail();
        }

        public override void Write(
            System.Text.Json.Utf8JsonWriter writer,
            decimal value,
            System.Text.Json.JsonSerializerOptions options
        ) {
            writer.WriteNumberValue(value.RemoveTrail());
        }

    }

    public sealed class DecimalNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<decimal> {

        public override decimal ReadJson(
            Newtonsoft.Json.JsonReader reader,
            Type objectType,
            decimal existingValue,
            bool hasExistingValue,
            Newtonsoft.Json.JsonSerializer serializer
        ) {
            decimal? _val = reader.ReadAsDecimal();

            decimal val = 0;
            if (_val.HasValue) {
                val = _val.Value;
            }

            return val.RemoveTrail();
        }

        public override void WriteJson(
            Newtonsoft.Json.JsonWriter writer,
            decimal value,
            Newtonsoft.Json.JsonSerializer serializer
        ) {
            writer.WriteRawValue(value.ToString(true));
        }

    }

}
