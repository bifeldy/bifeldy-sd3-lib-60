using System.Text;
using System.Text.Json;

namespace bifeldy_sd3_lib_60.Libraries {

    public sealed class StreamingNdjson : Stream {

        private readonly IAsyncEnumerable<object> _stream;
        private readonly JsonSerializerOptions _jsonOpt;
        private readonly MemoryStream _buffer;

        private IAsyncEnumerator<object> _enumerator;

        public StreamingNdjson(IAsyncEnumerable<object> stream, JsonSerializerOptions jsonOpt) {
            this._stream = stream;
            this._jsonOpt = jsonOpt;

            this._buffer = new MemoryStream();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => this._buffer.Length;

        public override long Position {
            get => this._buffer.Position;
            set => throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        public override void Flush() {
            this._buffer.Flush();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return this._buffer.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            this._enumerator ??= this._stream.GetAsyncEnumerator(cancellationToken);

            this._buffer.SetLength(0);
            this._buffer.Position = 0;

            if (await this._enumerator.MoveNextAsync()) {
                string line = JsonSerializer.Serialize(this._enumerator.Current, this._jsonOpt) + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(line);

                await this._buffer.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

                this._buffer.Position = 0;

                return await this._buffer.ReadAsync(buffer, offset, count, cancellationToken);
            }

            return 0;
        }

        protected override void Dispose(bool disposing) {
            this._enumerator?.DisposeAsync().AsTask().Wait();
            this._buffer?.Dispose();

            base.Dispose(disposing);
        }

    }

}
