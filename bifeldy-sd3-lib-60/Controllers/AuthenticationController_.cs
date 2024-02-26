/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Default Authentication API Endpoint
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using bifeldy_sd3_lib_60.AttributeFilterDecorator;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;
using Swashbuckle.AspNetCore.Annotations;

namespace bifeldy_sd3_lib_60.Controllers {

    public sealed class LoginInfo {
        public string user_name { get; set; }
        public string password { get; set; }
    }

    [ApiController]
    [Route("authentication")]
    public class AuthenticationController : ControllerBase {

        private readonly IHttpContextAccessor _hca;
        private readonly IChiperService _chiper;
        private readonly IApiTokenRepository _apiTokenRepo;
        private readonly IOraPg _orapg;

        public AuthenticationController(IHttpContextAccessor hca, IChiperService chiper, IApiTokenRepository apiTokenRepo, IOraPg orapg) {
            _hca = hca;
            _chiper = chiper;
            _apiTokenRepo = apiTokenRepo;
            _orapg = orapg;
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login untuk generate token pengguna di luar / selain IT S/SD 03 (ex. BOT / Departemen lain)")]
        public async Task<IActionResult> Login([FromBody] LoginInfo formData) {
            try {
                string userName = formData?.user_name;
                string password = formData?.password;

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) {
                    throw new Exception("Data Tidak Lengkap!");
                }

                API_TOKEN_T dcApiToken = await _apiTokenRepo.LoginBot(userName, password);
                if (dcApiToken == null) {
                    throw new Exception("User Name / Password Salah!");
                }

                UserApiSession userSession = new UserApiSession {
                    name = dcApiToken.USER_NAME,
                    dc_api_token_t = dcApiToken
                };
                string token = _chiper.EncodeJWT(userSession);

                return StatusCode(StatusCodes.Status201Created, new {
                    info = $"😅 201 - {GetType().Name} :: Login Berhasil 🤣",
                    result = userSession,
                    token
                });
            }
            catch (Exception ex) {
                return BadRequest(new {
                    info = $"🙄 400 - {GetType().Name} :: Login Gagal 😪",
                    result = new {
                        message = ex.Message
                    }
                });
            }
        }

        [HttpDelete("logout")]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        // [AllowedRoles(UserSessionRole.USER_SD_SSD_3, UserSessionRole.EXTERNAL_BOT)]
        [ApiExplorerSettings(IgnoreApi = true)]
        [SwaggerOperation(Summary = "Tidak wajib, hanya clean-up session saja")]
        public async Task<IActionResult> Logout() {
            try {
                UserApiSession userSession = (UserApiSession) _hca.HttpContext.Items["user"];

                API_TOKEN_T dcApiToken = await _apiTokenRepo.GetByUserName(userSession.name);
                dcApiToken.TOKEN_SEKALI_PAKAI = null;
                _orapg.Set<API_TOKEN_T>().Update(dcApiToken);
                await _orapg.SaveChangesAsync();

                return Accepted(new {
                    info = $"😅 204 - {GetType().Name} :: Logout Berhasil 🤣",
                    result = userSession
                });
            }
            catch (Exception ex) {
                return BadRequest(new {
                    info = $"🙄 400 - {GetType().Name} :: Logout Gagal 😪",
                    result = new {
                        message = ex.Message
                    }
                });
            }
        }

        [HttpPatch("verify")]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        // [AllowedRoles(UserSessionRole.USER_SD_SSD_3, UserSessionRole.EXTERNAL_BOT)]
        [SwaggerOperation(Summary = "Mengecek / validasi token untuk mendapatkan informasi sesi login")]
        public IActionResult Verify() {
            try {
                UserApiSession userSession = (UserApiSession) _hca.HttpContext.Items["user"];

                return Accepted(new {
                    info = $"😅 202 - {GetType().Name} :: Verifikasi Berhasil 🤣",
                    result = userSession
                });
            }
            catch (Exception ex) {
                return BadRequest(new {
                    info = $"🙄 400 - {GetType().Name} :: Verifikasi Gagal 😪",
                    result = new {
                        message = ex.Message
                    }
                });
            }
        }

    }

}
