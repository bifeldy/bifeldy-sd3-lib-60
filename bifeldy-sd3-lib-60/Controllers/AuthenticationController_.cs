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

using Swashbuckle.AspNetCore.Annotations;

using bifeldy_sd3_lib_60.AttributeFilterDecorator;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Controllers {

    public sealed class LoginInfo {
        public string user_name { get; set; }
        public string password { get; set; }
    }

    [ApiController]
    [Route("")]
    public class AuthenticationController : ControllerBase {

        private readonly IHttpContextAccessor _hca;
        private readonly IApplicationService _app;
        private readonly IChiperService _chiper;
        private readonly IApiTokenRepository _apiTokenRepo;
        private readonly IOraPg _orapg;

        public AuthenticationController(
            IHttpContextAccessor hca,
            IApplicationService app,
            IChiperService chiper,
            IApiTokenRepository apiTokenRepo,
            IOraPg orapg
        ) {
            _hca = hca;
            _app = app;
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
                    return BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"🙄 400 - {GetType().Name} :: Login Gagal 😪",
                        result = new {
                            message = "Data tidak lengkap!"
                        }
                    });
                }

                API_TOKEN_T dcApiToken = await _apiTokenRepo.LoginBot(userName, password);
                if (dcApiToken == null) {
                    return BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"🙄 400 - {GetType().Name} :: Login Gagal 😪",
                        result = new {
                            message = "User name / password salah!"
                        }
                    });
                }

                UserApiSession userSession = new UserApiSession {
                    name = dcApiToken.USER_NAME,
                    role = UserSessionRole.EXTERNAL_BOT,
                    // dc_api_token_t = dcApiToken
                };
                string token = _chiper.EncodeJWT(userSession);

                return StatusCode(StatusCodes.Status201Created, new {
                    info = $"😅 201 - {GetType().Name} :: Login Berhasil 🤣",
                    result = userSession,
                    token
                });
            }
            catch (Exception ex) {
                return BadRequest(new ResponseJsonSingle<dynamic> {
                    info = $"🙄 400 - {GetType().Name} :: Login Gagal 😪",
                    result = new {
                        message = _app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
                    }
                });
            }
        }

        [HttpDelete("logout")]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        // [AllowedRoles(UserSessionRole.USER_SD_SSD_3, UserSessionRole.EXTERNAL_BOT)]
        [ApiExplorerSettings(IgnoreApi = false)]
        [SwaggerOperation(Summary = "Tidak wajib, hanya clean-up session saja")]
        public async Task<IActionResult> Logout() {
            try {
                UserApiSession userSession = (UserApiSession) _hca.HttpContext.Items["user"];

                API_TOKEN_T dcApiToken = await _apiTokenRepo.GetByUserName(userSession.name);
                dcApiToken.TOKEN_SEKALI_PAKAI = null;
                _orapg.Set<API_TOKEN_T>().Update(dcApiToken);
                await _orapg.SaveChangesAsync();

                return Accepted(new ResponseJsonSingle<dynamic> {
                    info = $"😅 204 - {GetType().Name} :: Logout Berhasil 🤣",
                    result = userSession
                });
            }
            catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<dynamic> {
                    info = $"🙄 500 - {GetType().Name} :: Logout Gagal 😪",
                    result = new {
                        message = _app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
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

                return Accepted(new ResponseJsonSingle<dynamic> {
                    info = $"😅 202 - {GetType().Name} :: Verifikasi Berhasil 🤣",
                    result = userSession
                });
            }
            catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<dynamic> {
                    info = $"🙄 500 - {GetType().Name} :: Verifikasi Gagal 😪",
                    result = new {
                        message = _app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
                    }
                });
            }
        }

    }

}
