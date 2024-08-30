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
        [SwaggerHideJsonProperty] public string secret { get; set; }
    }

    [ApiController]
    [Route("")]
    public class _Controller : ControllerBase {

        private readonly IApplicationService _app;
        private readonly IChiperService _chiper;
        private readonly IApiKeyRepository _apiKeyRepo;
        private readonly IApiTokenRepository _apiTokenRepo;
        private readonly IUserRepository _userRepo;
        private readonly IOraPg _orapg;

        protected UserApiSession UserTokenData => (UserApiSession) this.HttpContext.Items["user"];

        public _Controller(
            IApplicationService app,
            IChiperService chiper,
            IApiKeyRepository apiKeyRepo,
            IApiTokenRepository apiTokenRepo,
            IUserRepository userRepo,
            IOraPg orapg
        ) {
            this._app = app;
            this._chiper = chiper;
            this._apiKeyRepo = apiKeyRepo;
            this._apiTokenRepo = apiTokenRepo;
            this._userRepo = userRepo;
            this._orapg = orapg;
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login untuk generate token pengguna di luar / selain IT S/SD 03 (ex. BOT / Departemen lain)")]
        public async Task<IActionResult> Login([FromBody] LoginInfo formData) {
            try {
                string userName = formData?.user_name;
                string password = formData?.password;
                string secret = formData?.secret;

                if (string.IsNullOrEmpty(secret) && (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))) {
                    return this.BadRequest(new ResponseJsonSingle<ResponseJsonError>() {
                        info = $"400 - {this.GetType().Name} :: Login Gagal",
                        result = new ResponseJsonError() {
                            message = "Data tidak lengkap!"
                        }
                    });
                }

                UserApiSession userSession = null;

                if (string.IsNullOrEmpty(secret)) {
                    UserSessionRole userRole = default;

                    API_TOKEN_T apiTokenT = await this._apiTokenRepo.LoginBot(userName, password);
                    if (apiTokenT == null) {
                        DC_USER_T dcUserT = await _userRepo.GetByUserNameNikPassword(userName, password);
                        if (dcUserT == null) {
                            return this.BadRequest(new ResponseJsonSingle<ResponseJsonError>() {
                                info = $"400 - {this.GetType().Name} :: Login Gagal",
                                result = new ResponseJsonError() {
                                    message = "User name / password salah!"
                                }
                            });
                        }
                        else {
                            userRole = UserSessionRole.USER_SD_SSD_3;
                        }
                    }
                    else {
                        userRole = UserSessionRole.EXTERNAL_BOT;
                    }

                    userSession = new UserApiSession {
                        name = userName.ToUpper(),
                        role = userRole
                    };
                }
                else {
                    API_KEY_T apiKeyT = await this._apiKeyRepo.SecretLogin(secret);
                    if (apiKeyT == null) {
                        return this.BadRequest(new ResponseJsonSingle<ResponseJsonError>() {
                            info = $"400 - {this.GetType().Name} :: Login Gagal",
                            result = new ResponseJsonError() {
                                message = "Secret salah / tidak dikenali!"
                            }
                        });
                    }
                    else {
                        userSession = new UserApiSession {
                            name = this.HttpContext.Connection.RemoteIpAddress.ToString(),
                            role = UserSessionRole.PROGRAM_SERVICE
                        };
                    }
                }

                string token = this._chiper.EncodeJWT(userSession);

                return this.StatusCode(StatusCodes.Status201Created, new {
                    info = $"201 - {this.GetType().Name} :: Login Berhasil",
                    result = userSession,
                    token
                });
            }
            catch (Exception ex) {
                return this.BadRequest(new ResponseJsonSingle<ResponseJsonError>() {
                    info = $"400 - {this.GetType().Name} :: Login Gagal",
                    result = new ResponseJsonError() {
                        message = (this._app.DebugMode || this.UserTokenData?.role <= UserSessionRole.USER_SD_SSD_3)
                            ? ex.Message
                            : "Terjadi kesalahan saat proses data!"
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
                if (this.UserTokenData.role == UserSessionRole.EXTERNAL_BOT) {
                    API_TOKEN_T dcApiToken = await this._apiTokenRepo.GetByUserName(this.UserTokenData.name);
                    dcApiToken.TOKEN_SEKALI_PAKAI = null;
                    _ = this._orapg.Set<API_TOKEN_T>().Update(dcApiToken);
                    _ = await this._orapg.SaveChangesAsync();
                }

                return this.Accepted(new ResponseJsonSingle<UserApiSession>() {
                    info = $"202 - {this.GetType().Name} :: Logout Berhasil",
                    result = this.UserTokenData
                });
            }
            catch (Exception ex) {
                return this.StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<ResponseJsonError>() {
                    info = $"500 - {this.GetType().Name} :: Logout Gagal",
                    result = new ResponseJsonError() {
                        message = (this._app.DebugMode || this.UserTokenData?.role <= UserSessionRole.USER_SD_SSD_3)
                            ? ex.Message
                            : "Terjadi kesalahan saat proses data!"
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
                return this.Accepted(new ResponseJsonSingle<UserApiSession>() {
                    info = $"202 - {this.GetType().Name} :: Verifikasi Berhasil",
                    result = this.UserTokenData
                });
            }
            catch (Exception ex) {
                return this.StatusCode(StatusCodes.Status500InternalServerError, new ResponseJsonSingle<ResponseJsonError>() {
                    info = $"500 - {this.GetType().Name} :: Verifikasi Gagal",
                    result = new ResponseJsonError() {
                        message = (this._app.DebugMode || this.UserTokenData?.role <= UserSessionRole.USER_SD_SSD_3)
                            ? ex.Message
                            : "Terjadi kesalahan saat proses data!"
                    }
                });
            }
        }

    }

}
