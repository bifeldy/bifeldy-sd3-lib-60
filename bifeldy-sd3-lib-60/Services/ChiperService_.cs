/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Encrypt / Decrypt String
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Ionic.Crc;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Extensions;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IChiperService {
        string EncryptText(string plainText, string passPhrase = null);
        string DecryptText(string cipherText, string passPhrase = null);
        string CalculateMD5File(string filePath);
        string CalculateCRC32File(string filePath);
        string CalculateSHA1File(string filePath);
        string GetMimeFile(string filePath);
        string HashByte(byte[] data);
        string HashText(string textMessage);
        string EncodeJWT(UserApiSession userSession, ulong expiredNextMilliSeconds = 60 * 60 * 1000 * 1);
        IEnumerable<Claim> DecodeJWT(string token);
        Task<string> SignFile(string filePath);
        Task<string> SignByte(byte[] data);
        Task<string> SignText(string textMessage);
        Task<bool> VerifyFile(string signature, string filePath);
        Task<bool> VerifyByte(string signature, byte[] data);
        Task<bool> VerifyText(string signature, string textMessage);
    }

    [SingletonServiceRegistration]
    public sealed class CChiperService : IChiperService {

        private readonly EnvVar _envVar;
        private readonly IApplicationService _app;

        private readonly IConverterService _converter;

        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int Keysize = 128;
        private const int Blocksize = 128;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        private string pubKeyPath { get; }
        private string privKeyPath { get; }

        public CChiperService(
            IOptions<EnvVar> envVar,
            IApplicationService app,
            IStreamService stream,
            IConverterService converter
        ) {
            this._envVar = envVar.Value;
            this._app = app;
            this._converter = converter;
            //
            this.pubKeyPath = Path.Combine(this._app.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "public.key");
            this.privKeyPath = Path.Combine(this._app.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "private.key");
        }

        private byte[] Generate128BitsOfRandomEntropy() {
            byte[] randomBytes = new byte[16]; // 16 Bytes will give us 128 bits.
            using (var rngCsp = RandomNumberGenerator.Create()) {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }

            return randomBytes;
        }

        public string EncryptText(string plainText, string passPhrase = null) {
            if (string.IsNullOrEmpty(passPhrase) || passPhrase?.Length < 8) {
                passPhrase = this.HashText(this._app.AppName);
            }
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            byte[] saltStringBytes = this.Generate128BitsOfRandomEntropy();
            byte[] ivStringBytes = this.Generate128BitsOfRandomEntropy();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations)) {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = Aes.Create()) {
                    symmetricKey.BlockSize = Blocksize;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes)) {
                        using (var memoryStream = new MemoryStream()) {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)) {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                byte[] cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public string DecryptText(string cipherText, string passPhrase = null) {
            if (string.IsNullOrEmpty(passPhrase) || passPhrase?.Length < 8) {
                passPhrase = this.HashText(this._app.AppName);
            }
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            byte[] saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            byte[] ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8 * 2).Take(cipherTextBytesWithSaltAndIv.Length - (Keysize / 8 * 2)).ToArray();
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations)) {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = Aes.Create()) {
                    symmetricKey.BlockSize = Blocksize;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes)) {
                        using (var memoryStream = new MemoryStream(cipherTextBytes)) {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)) {
                                using (var streamReader = new StreamReader(cryptoStream, Encoding.UTF8)) {
                                    return streamReader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
        }

        public string CalculateMD5File(string filePath) {
            using (var md5 = MD5.Create()) {
                using (FileStream stream = File.OpenRead(filePath)) {
                    return md5.ComputeHash(stream).ToStringHex();
                }
            }
        }

        public string CalculateCRC32File(string filePath) {
            using (FileStream stream = File.OpenRead(filePath)) {
                return new CRC32().GetCrc32(stream).ToString("x");
            }
        }

        public string CalculateSHA1File(string filePath) {
            using (var sha1 = SHA1.Create()) {
                using (FileStream stream = File.OpenRead(filePath)) {
                    return sha1.ComputeHash(stream).ToStringHex();
                }
            }
        }

        [SupportedOSPlatform("windows")]
        [DllImport("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
        private static extern int FindMimeFromData(
            IntPtr pBC,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeParamIndex = 3)] byte[] pBuffer,
            int cbSize,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzMimeProposed,
            int dwMimeFlags,
            out IntPtr ppwzMimeOut,
            int dwReserved
        );

        public string GetMimeFile(string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException(filePath + " Not Found");
            }

            const int maxContent = 256;
            byte[] buffer = new byte[maxContent];
            using (var fs = new FileStream(filePath, FileMode.Open)) {
                if (fs.Length >= maxContent) {
                    _ = fs.Read(buffer, 0, maxContent);
                }
                else {
                    _ = fs.Read(buffer, 0, (int) fs.Length);
                }
            }

            IntPtr mimeTypePtr = IntPtr.Zero;
            try {
                int result = FindMimeFromData(IntPtr.Zero, null, buffer, maxContent, null, 0, out mimeTypePtr, 0);
                if (result != 0) {
                    Marshal.FreeCoTaskMem(mimeTypePtr);
                    throw Marshal.GetExceptionForHR(result);
                }

                string mime = Marshal.PtrToStringUni(mimeTypePtr);
                Marshal.FreeCoTaskMem(mimeTypePtr);
                return mime;
            }
            catch {
                if (mimeTypePtr != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(mimeTypePtr);
                }

                return "application/octet-stream";
            }
        }

        public string HashByte(byte[] data) {
            using (var sha1 = SHA1.Create()) {
                byte[] hash = sha1.ComputeHash(data);
                return hash.ToStringHex();
            }
        }

        public string HashText(string textMessage) {
            byte[] data = Encoding.UTF8.GetBytes(textMessage);
            return this.HashByte(data);
        }

        public string EncodeJWT(UserApiSession userSession, ulong expiredNextMilliSeconds = 60 * 60 * 1000 * 1) {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.HashText(this._envVar.JWT_SECRET)));
            var credetial = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var userClaim = new List<Claim>() {
                new(ClaimTypes.Name, userSession.name),
                new(ClaimTypes.Role, userSession.role.ToString())
            };
            var token = new JwtSecurityToken(
                this._app.AppName,
                this._envVar.JWT_AUDIENCE,
                userClaim,
                expires: DateTime.Now.AddMilliseconds(expiredNextMilliSeconds),
                signingCredentials: credetial
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public IEnumerable<Claim> DecodeJWT(string token) {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.HashText(this._envVar.JWT_SECRET)));
            var tokenHandler = new JwtSecurityTokenHandler();
            _ = tokenHandler.ValidateToken(token, new TokenValidationParameters {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = this._app.AppName,
                ValidAudience = this._envVar.JWT_AUDIENCE
            }, out SecurityToken validateToken);

            var jwtToken = (JwtSecurityToken) validateToken;
            return jwtToken.Claims;
        }

        private async Task<RSA> GenerateAndLoadRsa() {
            var rsa = RSA.Create();
            rsa.KeySize = 4096;

            if (!File.Exists(this.privKeyPath)) {
                string privateKey = rsa.ToXmlString(true);
                await File.WriteAllTextAsync(this.privKeyPath, privateKey);

                string publicKey = rsa.ToXmlString(false);
                await File.WriteAllTextAsync(this.pubKeyPath, publicKey);

                return rsa;
            }

            string privateKeyString = await File.ReadAllTextAsync(this.privKeyPath);
            rsa.FromXmlString(privateKeyString);

            return rsa;
        }

        private async Task<string> RsaSign(Func<SHA256, RSAPKCS1SignatureFormatter, Task<string>> callback) {
            using (var alg = SHA256.Create()) {
                using (RSA rsa = await this.GenerateAndLoadRsa()) {
                    var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                    rsaFormatter.SetHashAlgorithm(nameof(SHA256));
                    return await callback(alg, rsaFormatter);
                }
            }
        }

        public async Task<string> SignFile(string filePath) {
            return await this.RsaSign(async (alg, rsaFormatter) => {
                using (FileStream stream = File.OpenRead(filePath)) {
                    byte[] hash = await alg.ComputeHashAsync(stream);
                    byte[] signHash = rsaFormatter.CreateSignature(hash);
                    return signHash.ToStringHex();
                }
            });
        }

        public async Task<string> SignByte(byte[] data) {
            return await this.RsaSign(async (alg, rsaFormatter) => {
                byte[] hash = alg.ComputeHash(data);
                byte[] signHash = rsaFormatter.CreateSignature(hash);
                string signedHash = signHash.ToStringHex();
                return await Task.FromResult(signedHash);
            });
        }

        public async Task<string> SignText(string textMessage) {
            byte[] data = Encoding.UTF8.GetBytes(textMessage);
            return await this.SignByte(data);
        }

        private async Task<bool> RsaVerify(Func<SHA256, RSAPKCS1SignatureDeformatter, Task<bool>> callback) {
            using (var alg = SHA256.Create()) {
                using (RSA rsa = await this.GenerateAndLoadRsa()) {
                    var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                    rsaDeformatter.SetHashAlgorithm(nameof(SHA256));
                    return await callback(alg, rsaDeformatter);
                }
            }
        }

        public async Task<bool> VerifyFile(string signature, string filePath) {
            return await this.RsaVerify(async (alg, rsaDeformatter) => {
                using (FileStream stream = File.OpenRead(filePath)) {
                    byte[] hash = await alg.ComputeHashAsync(stream);
                    byte[] signHash = signature.ParseHexTextToByte();
                    return rsaDeformatter.VerifySignature(hash, signHash);
                }
            });
        }

        public async Task<bool> VerifyByte(string signature, byte[] data) {
            return await this.RsaVerify(async (alg, rsaDeformatter) => {
                byte[] hash = alg.ComputeHash(data);
                byte[] signHash = signature.ParseHexTextToByte();
                bool isVerified = rsaDeformatter.VerifySignature(hash, signHash);
                return await Task.FromResult(isVerified);
            });
        }

        public async Task<bool> VerifyText(string signature, string textMessage) {
            byte[] data = Encoding.UTF8.GetBytes(textMessage);
            return await this.VerifyByte(signature, data);
        }

    }

}
