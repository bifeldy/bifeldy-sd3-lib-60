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

namespace bifeldy_sd3_lib_60.Controllers {

    [ApiController]
    [Route("ping-pong")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PingPongController : ControllerBase {

        public PingPongController() {
            //
        }

        [HttpHead]
        [SwaggerOperation(Summary = "Untuk test Ping-Pong saja")]
        public IActionResult PingPong() => this.Ok();

    }

}
