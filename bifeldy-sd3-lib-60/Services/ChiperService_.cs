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
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Ionic.Crc;

using bifeldy_sd3_lib_60.Models;
using System.Security.Claims;

namespace bifeldy_sd3_lib_60.Services {

    public interface IChiperService {
        string EncryptText(string plainText, string passPhrase = null);
        string DecryptText(string cipherText, string passPhrase = null);
        string CalculateMD5(string filePath);
        string CalculateCRC32(string filePath);
        string CalculateSHA1(string filePath);
        string GetMimeFromFile(string filename);
        string HashText(string text);
        string EncodeJWT(UserApiSession userSession, ulong expiredNextMilliSeconds = 60 * 60 * 1000 * 1);
        string DecodeJWT(string token, string claimType = ClaimTypes.Name);
    }

    public sealed class CChiperService : IChiperService {

        private readonly EnvVar _envVar;
        private readonly IApplicationService _app;

        private readonly IStreamService _stream;

        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        public CChiperService(IOptions<EnvVar> envVar, IApplicationService app, IStreamService stream) {
            _envVar = envVar.Value;
            _app = app;
            _stream = stream;
        }

        private byte[] Generate256BitsOfRandomEntropy() {
            byte[] randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (RandomNumberGenerator rngCsp = RandomNumberGenerator.Create()) {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        public string EncryptText(string plainText, string passPhrase = null) {
            if (string.IsNullOrEmpty(passPhrase)) {
                passPhrase = _app.AppName;
            }
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            byte[] saltStringBytes = Generate256BitsOfRandomEntropy();
            byte[] ivStringBytes = Generate256BitsOfRandomEntropy();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations)) {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged()) {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes)) {
                        using (MemoryStream memoryStream = new MemoryStream()) {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)) {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                byte[] cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public string DecryptText(string cipherText, string passPhrase = null) {
            if (string.IsNullOrEmpty(passPhrase)) {
                passPhrase = _app.AppName;
            }
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            byte[] saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            byte[] ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();
            using (Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations)) {
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                using (RijndaelManaged symmetricKey = new RijndaelManaged()) {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes)) {
                        using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes)) {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)) {
                                using (StreamReader streamReader = new StreamReader(cryptoStream, Encoding.UTF8)) {
                                    return streamReader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
        }

        public string CalculateMD5(string filePath) {
            using (MD5 md5 = MD5.Create()) {
                using (MemoryStream ms = _stream.ReadFileAsBinaryStream(filePath)) {
                    byte[] hash = md5.ComputeHash(ms.ToArray());
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public string CalculateCRC32(string filePath) {
            CRC32 crc32 = new CRC32();
            using (MemoryStream ms = _stream.ReadFileAsBinaryStream(filePath)) {
                byte[] data = ms.ToArray();
                crc32.SlurpBlock(data, 0, data.Length);
                return crc32.Crc32Result.ToString("x");
            }
        }

        public string CalculateSHA1(string filePath) {
            using (SHA1 sha1 = SHA1.Create()) {
                using (MemoryStream ms = _stream.ReadFileAsBinaryStream(filePath)) {
                    byte[] hash = sha1.ComputeHash(ms.ToArray());
                    StringBuilder sb = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash) {
                        sb.Append(b.ToString("x"));
                    }
                    return sb.ToString();
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

        public string GetMimeFromFile(string filename) {
            if (!File.Exists(filename)) {
                throw new FileNotFoundException(filename + " Not Found");
            }
            const int maxContent = 256;
            byte[] buffer = new byte[maxContent];
            using (FileStream fs = new FileStream(filename, FileMode.Open)) {
                if (fs.Length >= maxContent) {
                    fs.Read(buffer, 0, maxContent);
                }
                else {
                    fs.Read(buffer, 0, (int)fs.Length);
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
                return "unknown/unknown";
            }
        }

        public string HashText(string text) {
            using (SHA1Managed sha1 = new SHA1Managed()) {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(text));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public string EncodeJWT(UserApiSession userSession, ulong expiredNextMilliSeconds = 60 * 60 * 1000 * 1) {
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(HashText(_envVar.JWT_SECRET)));
            SigningCredentials credetial = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            List<Claim> userClaim = new List<Claim> {
                new Claim(ClaimTypes.Name, userSession.name),
                new Claim(ClaimTypes.Role, userSession.role.ToString())
            };
            JwtSecurityToken token = new JwtSecurityToken(
                _app.AppName,
                _envVar.JWT_AUDIENCE,
                userClaim,
                expires: DateTime.Now.AddMilliseconds(expiredNextMilliSeconds),
                signingCredentials: credetial
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string DecodeJWT(string token, string claimType = ClaimTypes.Name) {
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(HashText(_envVar.JWT_SECRET)));
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = _app.AppName,
                ValidAudience = _envVar.JWT_AUDIENCE
            }, out SecurityToken validateToken);

            JwtSecurityToken jwtToken = (JwtSecurityToken) validateToken;
            return jwtToken.Claims.Where(c => c.Type == claimType).First().Value;
        }

    }

}
