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

using System.Globalization;

using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Libraries {

    public sealed class DecimalSystemTextJsonConverter : System.Text.Json.Serialization.JsonConverter<decimal> {

        public override decimal Read(
            ref System.Text.Json.Utf8JsonReader reader,
            Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options
        ) {
            switch (reader.TokenType) {
                case System.Text.Json.JsonTokenType.Number:
                    return reader.GetDecimal().RemoveTrail();

                case System.Text.Json.JsonTokenType.String:
                    string s = reader.GetString();

                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) {
                        return d.RemoveTrail();
                    }

                    break;
            }

            throw new System.Text.Json.JsonException($"Unexpected token {reader.TokenType} when parsing decimal.");
        }

        public override void Write(
            System.Text.Json.Utf8JsonWriter writer,
            decimal value,
            System.Text.Json.JsonSerializerOptions options
        ) {
            writer.WriteNumberValue(value.RemoveTrail());
        }

    }

    public sealed class NullableDecimalSystemTextJsonConverter : System.Text.Json.Serialization.JsonConverter<decimal?> {

        public override decimal? Read(
            ref System.Text.Json.Utf8JsonReader reader,
            Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options
        ) {
            switch (reader.TokenType) {
                case System.Text.Json.JsonTokenType.Null:
                    return null;

                case System.Text.Json.JsonTokenType.Number:
                    return reader.GetDecimal().RemoveTrail();

                case System.Text.Json.JsonTokenType.String:
                    string s = reader.GetString();

                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) {
                        return d.RemoveTrail();
                    }

                    return null;
            }

            throw new System.Text.Json.JsonException($"Unexpected token {reader.TokenType} when parsing decimal?");
        }

        public override void Write(
            System.Text.Json.Utf8JsonWriter writer,
            decimal? value,
            System.Text.Json.JsonSerializerOptions options
        ) {
            if (value.HasValue) {
                writer.WriteNumberValue(value.Value.RemoveTrail());
            }
            else {
                writer.WriteNullValue();
            }
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
            if (
                reader.TokenType == Newtonsoft.Json.JsonToken.Integer ||
                reader.TokenType == Newtonsoft.Json.JsonToken.Float
            ) {
                return Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture).RemoveTrail();
            }

            if (reader.TokenType == Newtonsoft.Json.JsonToken.String) {
                string s = (string)reader.Value;

                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) {
                    return d.RemoveTrail();
                }
            }

            throw new Newtonsoft.Json.JsonSerializationException($"Unexpected token {reader.TokenType}");
        }

        public override void WriteJson(
            Newtonsoft.Json.JsonWriter writer,
            decimal value,
            Newtonsoft.Json.JsonSerializer serializer
        ) {
            writer.WriteRawValue(value.RemoveTrail().ToString(CultureInfo.InvariantCulture));
        }

    }

    public sealed class NullableDecimalNewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<decimal?> {

        public override decimal? ReadJson(
            Newtonsoft.Json.JsonReader reader,
            Type objectType,
            decimal? existingValue,
            bool hasExistingValue,
            Newtonsoft.Json.JsonSerializer serializer
        ) {
            if (reader.TokenType == Newtonsoft.Json.JsonToken.Null) {
                return null;
            }

            if (
                reader.TokenType == Newtonsoft.Json.JsonToken.Integer ||
                reader.TokenType == Newtonsoft.Json.JsonToken.Float
            ) {
                return Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture).RemoveTrail();
            }

            if (reader.TokenType == Newtonsoft.Json.JsonToken.String) {
                string s = (string)reader.Value;

                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) {
                    return d.RemoveTrail();
                }

                return null;
            }

            throw new Newtonsoft.Json.JsonSerializationException($"Unexpected token {reader.TokenType}");
        }

        public override void WriteJson(
            Newtonsoft.Json.JsonWriter writer,
            decimal? value,
            Newtonsoft.Json.JsonSerializer serializer
        ) {
            if (value == null) {
                writer.WriteNull();
            }
            else {
                writer.WriteRawValue(value.Value.RemoveTrail().ToString(CultureInfo.InvariantCulture));
            }
        }

    }

}
