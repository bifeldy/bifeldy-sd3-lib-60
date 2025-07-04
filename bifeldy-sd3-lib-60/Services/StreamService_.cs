﻿/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Stream Tools
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.IO.Compression;
using System.Text;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;

namespace bifeldy_sd3_lib_60.Services {

    public interface IStreamService {
        void CopyTo(Stream src, Stream dest);
        string GZipDecompressString(byte[] byteData);
        byte[] GZipCompressString(string text);
        Task<List<byte[]>> ReadFileAsBinaryChunk(string filePath, int maxChunk = 1024, CancellationToken token = default);
        Task<MemoryStream> ReadFileAsBinaryStream(string filePath, int maxChunk = 1024, CancellationToken token = default);
    }

    [SingletonServiceRegistration]
    public sealed class CStreamService : IStreamService {

        public CStreamService() {
            //
        }

        public void CopyTo(Stream src, Stream dest) {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
                dest.Write(bytes, 0, cnt);
            }
        }

        public string GZipDecompressString(byte[] byteData) {
            using (var msi = new MemoryStream(byteData)) {
                using (var mso = new MemoryStream()) {
                    using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                        this.CopyTo(gs, mso);
                    }

                    return Encoding.UTF8.GetString(mso.ToArray());
                }
            }
        }

        public byte[] GZipCompressString(string text) {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (var msi = new MemoryStream(bytes)) {
                using (var mso = new MemoryStream()) {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                        this.CopyTo(msi, gs);
                    }

                    return mso.ToArray();
                }
            }
        }

        public async Task<List<byte[]>> ReadFileAsBinaryChunk(string filePath, int maxChunk = 1024, CancellationToken token = default) {
            var res = new List<byte[]>();
            using (MemoryStream ms = await this.ReadFileAsBinaryStream(filePath, maxChunk, token)) {
                byte[] data = ms.ToArray();
                foreach (byte[] d in data.Split(maxChunk)) {
                    res.Add(d);
                }
            }

            return res;
        }

        public async Task<MemoryStream> ReadFileAsBinaryStream(string filePath, int maxChunk = 1024, CancellationToken token = default) {
            var dest = new MemoryStream();
            using (Stream source = File.OpenRead(filePath)) {
                byte[] buffer = new byte[maxChunk];
                int bytesRead = 0;
                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, token)) > 0) {
                    await dest.WriteAsync(buffer, 0, bytesRead);
                }
            }

            return dest;
        }

    }

}
