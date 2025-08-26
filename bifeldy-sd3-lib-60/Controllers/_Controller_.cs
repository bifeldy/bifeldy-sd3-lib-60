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

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Swashbuckle.AspNetCore.Annotations;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Controllers {

    public sealed class LoginInfo {
        public string user_name { get; set; }
        public string password { get; set; }
        [SwaggerHideJsonProperty] public string secret { get; set; }
    }

    [ApiController]
    [Route("")]
    [ApiExplorerSettings(GroupName = "_")]
    public class _Controller : ControllerBase {

        private readonly IServer _server;

        private readonly EnvVar _envVar;

        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;
        private readonly IChiperService _chiper;
        private readonly IApiKeyRepository _apiKeyRepo;
        private readonly IApiTokenRepository _apiTokenRepo;
        private readonly IUserRepository _userRepo;
        private readonly IOraPg _orapg;

        protected UserApiSession UserTokenData => (UserApiSession) this.HttpContext.Items["user"];

        public _Controller(
            IServer server,
            IOptions<EnvVar> envVar,
            IApplicationService app,
            IGlobalService gs,
            IChiperService chiper,
            IApiKeyRepository apiKeyRepo,
            IApiTokenRepository apiTokenRepo,
            IUserRepository userRepo,
            IOraPg orapg
        ) {
            this._server = server;
            this._envVar = envVar.Value;
            this._app = app;
            this._gs = gs;
            this._chiper = chiper;
            this._apiKeyRepo = apiKeyRepo;
            this._apiTokenRepo = apiTokenRepo;
            this._userRepo = userRepo;
            this._orapg = orapg;
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login untuk generate token pengguna di luar / selain IT S/SD 03 (ex. BOT / Departemen lain)")]
        public async Task<IActionResult> Login([FromBody] LoginInfo formData) {
            string userName = formData?.user_name;
            string password = formData?.password;
            string secret = formData?.secret;

            if (string.IsNullOrEmpty(secret) && (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))) {
                return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {this.GetType().Name} :: Login Gagal",
                    result = new ResponseJsonMessage() {
                        message = "Data Tidak Lengkap!"
                    }
                });
            }

            UserApiSession userSession = null;

            if (string.IsNullOrEmpty(secret)) {
                UserSessionRole userRole = default;

                API_TOKEN_T apiTokenT = await this._apiTokenRepo.LoginBot(this._envVar.IS_USING_POSTGRES, this._orapg, userName, password);
                if (apiTokenT == null) {
                    DC_USER_T dcUserT = await this._userRepo.GetByUserNameNikPassword(this._envVar.IS_USING_POSTGRES, this._orapg, userName, password);
                    if (dcUserT == null) {
                        return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"400 - {this.GetType().Name} :: Login Gagal",
                            result = new ResponseJsonMessage() {
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
                API_KEY_T apiKeyT = await this._apiKeyRepo.SecretLogin(this._envVar.IS_USING_POSTGRES, this._orapg, secret);
                if (apiKeyT == null) {
                    return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                        info = $"400 - {this.GetType().Name} :: Login Gagal",
                        result = new ResponseJsonMessage() {
                            message = "Secret salah / tidak dikenali!"
                        }
                    });
                }
                else {
                    userSession = new UserApiSession {
                        name = this._gs.GetIpOriginData(this.HttpContext.Connection, this.HttpContext.Request, true, true),
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

        [HttpDelete("logout")]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        // [AllowedRoles(UserSessionRole.USER_SD_SSD_3, UserSessionRole.EXTERNAL_BOT)]
        [ApiExplorerSettings(IgnoreApi = false)]
        [SwaggerOperation(Summary = "Tidak wajib, hanya clean-up session saja")]
        public async Task<IActionResult> Logout() {
            if (this.UserTokenData.role == UserSessionRole.EXTERNAL_BOT) {
                API_TOKEN_T dcApiToken = await this._apiTokenRepo.GetByUserName(this._envVar.IS_USING_POSTGRES, this._orapg, this.UserTokenData.name);
                dcApiToken.TOKEN_SEKALI_PAKAI = null;
                _ = this._orapg.Set<API_TOKEN_T>().Update(dcApiToken);
                _ = await this._orapg.SaveChangesAsync();
            }

            return this.Accepted(new ResponseJsonSingle<UserApiSession>() {
                info = $"202 - {this.GetType().Name} :: Logout Berhasil",
                result = this.UserTokenData
            });
        }

        [HttpPatch("verify")]
        [MinRole(UserSessionRole.EXTERNAL_BOT)]
        // [AllowedRoles(UserSessionRole.USER_SD_SSD_3, UserSessionRole.EXTERNAL_BOT)]
        [SwaggerOperation(Summary = "Mengecek / validasi token untuk mendapatkan informasi sesi login")]
        public IActionResult Verify() {
            return this.Accepted(new ResponseJsonSingle<UserApiSession>() {
                info = $"202 - {this.GetType().Name} :: Verifikasi Berhasil",
                result = this.UserTokenData
            });
        }

        /* ** */

        [HttpGet("protobuf-net")]
        [SwaggerOperation(Summary = "Informasi Protobuf-NET.Grpc")]
        public IActionResult GrpcInfo() {
            IServerAddressesFeature saf = this._server.Features.Get<IServerAddressesFeature>();

            int apiPort = this._envVar.API_PORT;
            int grpcPort = this._envVar.GRPC_PORT;

            foreach (string address in saf.Addresses) {
                var uri = new Uri(address);
                int port = uri.Port;
                if (port == 80 || port == 8145 || port == apiPort) {
                    apiPort = port;
                }
                else if (port == grpcPort) {
                    grpcPort = port;
                }
                else {
                    throw new Exception("Gagal Mendapatkan Konfigurasi Port");
                }
            }

            string folderPath = Path.Combine(this._app.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "protobuf-net");
            IEnumerable<string> fileNames = Directory.GetFiles(folderPath).Select(p => Path.GetFileName(p)).Where(p => !p.Contains("Services.TarikData.DbLink") && !p.Contains("Services.ProsesData.DbLink"));

            return this.Ok(new {
                info = $"200 - {this.GetType().Name} :: Informasi GRPC",
                grpc_port = grpcPort,
                grpc_services = fileNames
            });
        }

        [HttpGet("protobuf-net/{fileName}")]
        [SwaggerOperation(Summary = "Informasi Service Class Data Type Protobuf-NET.Grpc")]
        public IActionResult GrpcProtoFile(string fileName) {
            string filePath = Path.Combine(this._app.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "protobuf-net", fileName);
            if (!filePath.EndsWith(".proto")) {
                filePath += ".proto";
            }

            if (!System.IO.File.Exists(filePath)) {
                throw new Exception("File Tidak Ditemukan!");
            }

            return this.PhysicalFile(filePath, "text/plain");
        }

        [HttpGet("all-dc")]
        [SwaggerOperation(Summary = "Informasi Daftar Kode & Nama DC")]
        [RouteExcludeAllDc]
        public async Task<IActionResult> GetAllDc() {
            List<DC_TABEL_DC_T> ls = await this._orapg.GetListAsync<DC_TABEL_DC_T>($@"
                SELECT
                    tbl_dc_kode,
                    tbl_dc_nama
                FROM
                    dc_tabel_dc_t
                ORDER BY
                    tbl_dc_kode ASC
            ");

            return this.Ok(new {
                info = $"200 - {this.GetType().Name} :: All DC",
                results = ls.Select(l => new {
                    kode_dc = l.TBL_DC_KODE,
                    nama_dc = l.TBL_DC_NAMA
                })
            });
        }

    }

}
