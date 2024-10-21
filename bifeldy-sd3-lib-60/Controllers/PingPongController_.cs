/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Default Ping Pong API Endpoint
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Repositories;

namespace bifeldy_sd3_lib_60.Controllers {

    [ApiController]
    [Route("ping-pong")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PingPongController : ControllerBase {

        private readonly IGlobalService _gs;
        private readonly IOraPg _orapg;
        private readonly IGeneralRepository _generalRepo;

        public PingPongController(
            IGlobalService gs,
            IOraPg orapg,
            IGeneralRepository generalRepo
        ) {
            this._gs = gs;
            this._orapg = orapg;
            this._generalRepo = generalRepo;
        }

        [HttpPut]
        [SwaggerOperation(Summary = "Untuk test Ping-Pong saja")]
        public async Task<IActionResult> PingPong(
            [FromBody, SwaggerParameter("JSON body yang berisi kode gudang", Required = false)] InputJsonDc fd
        ) {
            string ipOrigin = this._gs.CleanIpOrigin(
                this._gs.GetIpOriginData(
                    this.HttpContext.Connection,
                    this.HttpContext.Request
                )
            );

            string kodeDc = await this._generalRepo.GetKodeDc();
            if (!string.IsNullOrEmpty(fd?.kode_dc) && kodeDc == "DCHO") {
                _ = await _orapg.ExecQueryAsync($@"
                    INSERT INTO api_ping_t (dc_kode, ip_origin, last_online)
                    VALUES (:dc_kode, ip_origin, last_online)
                ", new List<CDbQueryParamBind>() {
                    new() { NAME = "dc_kode", VALUE = fd.kode_dc.ToUpper() },
                    new() { NAME = "ip_origin", VALUE = ipOrigin },
                    new() { NAME = "last_online", VALUE = DateTime.Now }
                });
            }

            return this.Ok(new ResponseJsonSingle<ResponseJsonError>() {
                info = $"200 - {this.GetType().Name}",
                result = new ResponseJsonError() {
                    message = ipOrigin
                }
            });
        }

    }

}
