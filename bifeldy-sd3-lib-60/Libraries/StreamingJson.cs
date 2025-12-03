/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: application/json
* 
*/

using System.Text.Json;

namespace bifeldy_sd3_lib_60.Libraries {

    public sealed class StreamingJson : Stream {

        private readonly IAsyncEnumerable<object> _stream;
        private readonly JsonSerializerOptions _jsonOpt;
        private readonly MemoryStream _internalBuffer;

        private IAsyncEnumerator<object> _enumerator;

        private Utf8JsonWriter _writer;

        private bool _started = false;
        private bool _ended = false;

        public StreamingJson(IAsyncEnumerable<object> stream, JsonSerializerOptions jsonOpt) {
            this._stream = stream;
            this._jsonOpt = jsonOpt;

            this._internalBuffer = new MemoryStream();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => this._internalBuffer.Length;

        public override long Position {
            get => _internalBuffer.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() {
            this._internalBuffer.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return this._internalBuffer.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            this._enumerator ??= this._stream.GetAsyncEnumerator(cancellationToken);

            this._internalBuffer.SetLength(0);
            this._internalBuffer.Position = 0;

            if (!this._started) {
                this._writer = new Utf8JsonWriter(this._internalBuffer);
                this._writer.WriteStartArray();

                await this._writer.FlushAsync(cancellationToken);

                this._started = true;
            }

            if (!this._ended) {
                if (await this._enumerator.MoveNextAsync()) {
                    JsonSerializer.Serialize(this._writer, this._enumerator.Current, this._jsonOpt);
                    await this._writer.FlushAsync(cancellationToken);
                }
                else {
                    this._writer.WriteEndArray();

                    await this._writer.FlushAsync(cancellationToken);

                    this._ended = true;
                }
            }

            this._internalBuffer.Position = 0;

            return await this._internalBuffer.ReadAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing) {
            this._writer?.Dispose();
            this._enumerator?.DisposeAsync().AsTask().Wait();
            this._internalBuffer?.Dispose();

            base.Dispose(disposing);
        }

    }

}
