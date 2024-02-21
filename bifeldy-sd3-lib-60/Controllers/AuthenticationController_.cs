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

using Microsoft.AspNetCore.Mvc;

using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Tables;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Controllers {

    public sealed class LoginInfo {
        public string user_name { get; set; }
        public string password { get; set; }
    }

    [ApiController]
    [Route("authentication")]
    public class AuthenticationController : ControllerBase {

        private readonly IChiperService _chiper;
        private readonly IAuthRepository _authRepo;

        public AuthenticationController(IChiperService chiper, IAuthRepository authRepo) {
            _chiper = chiper;
            _authRepo = authRepo;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginInfo formData) {
            try {
                string userName = formData?.user_name;
                string pass = formData?.password;

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(pass)) {
                    throw new Exception("Data Tidak Lengkap!");
                }

                DC_AUTH_T dcAuthT = await _authRepo.GetByUserNamePass(userName, pass);
                if (dcAuthT == null) {
                    throw new Exception("User Name / Password Salah!");
                }

                UserApiSession userSession = new UserApiSession {
                    name = dcAuthT.USERNAME,
                    dc_auth_t = dcAuthT
                };
                string token = _chiper.EncodeJWT(userSession);
                return Ok(new {
                    info = $"😅 200 - {GetType().Name} :: Login Berhasil 🤣",
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
    }

}
