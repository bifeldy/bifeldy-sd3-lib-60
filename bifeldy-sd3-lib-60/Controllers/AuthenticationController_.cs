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

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
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
            this._hca = hca;
            this._app = app;
            this._chiper = chiper;
            this._apiTokenRepo = apiTokenRepo;
            this._orapg = orapg;
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login untuk generate token pengguna di luar / selain IT S/SD 03 (ex. BOT / Departemen lain)")]
        public async Task<IActionResult> Login([FromBody] LoginInfo formData) {
            try {
                string userName = formData?.user_name;
                string password = formData?.password;

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) {
                    return this.BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"400 - {this.GetType().Name} :: Login Gagal",
                        result = new {
                            message = "Data tidak lengkap!"
                        }
                    });
                }

                API_TOKEN_T dcApiToken = await this._apiTokenRepo.LoginBot(userName, password);
                if (dcApiToken == null) {
                    return this.BadRequest(new ResponseJsonSingle<dynamic> {
                        info = $"400 - {this.GetType().Name} :: Login Gagal",
                        result = new {
                            message = "User name / password salah!"
                        }
                    });
                }

                var userSession = new UserApiSession {
                    name = dcApiToken.USER_NAME,
                    role = UserSessionRole.EXTERNAL_BOT,
                    // dc_api_token_t = dcApiToken
                };
                string token = this._chiper.EncodeJWT(userSession);

                return this.StatusCode(StatusCodes.Status201Created, new {
                    info = $"201 - {this.GetType().Name} :: Login Berhasil",
                    result = userSession,
                    token
                });
            }
            catch (Exception ex) {
                return this.BadRequest(new ResponseJsonSingle<dynamic> {
                    info = $"400 - {this.GetType().Name} :: Login Gagal",
                    result = new {
                        message = this._app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
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
                var userSession = (UserApiSession) this._hca.HttpContext.Items["user"];

                API_TOKEN_T dcApiToken = await this._apiTokenRepo.GetByUserName(userSession.name);
                dcApiToken.TOKEN_SEKALI_PAKAI = null;
                _ = this._orapg.Set<API_TOKEN_T>().Update(dcApiToken);
                _ = await this._orapg.SaveChangesAsync();

                return this.Accepted(new ResponseJsonSingle<dynamic> {
                    info = $"204 - {this.GetType().Name} :: Logout Berhasil",
                    result = userSession
                });
            }
            catch (Exception ex) {
                return this.StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<dynamic> {
                    info = $"500 - {this.GetType().Name} :: Logout Gagal",
                    result = new {
                        message = this._app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
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
                var userSession = (UserApiSession) this._hca.HttpContext.Items["user"];

                return this.Accepted(new ResponseJsonSingle<dynamic> {
                    info = $"202 - {this.GetType().Name} :: Verifikasi Berhasil",
                    result = userSession
                });
            }
            catch (Exception ex) {
                return this.StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<dynamic> {
                    info = $"500 - {this.GetType().Name} :: Verifikasi Gagal",
                    result = new {
                        message = this._app.DebugMode ? ex.Message : "Terjadi kesalahan saat proses data"
                    }
                });
            }
        }

    }

}
