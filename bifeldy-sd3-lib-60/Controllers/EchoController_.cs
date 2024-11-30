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
    [Route("echo")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class EchoController : ControllerBase {

        public IActionResult ReturnData(dynamic json = null) {
            IDictionary<string, string> headers = this.Request.Headers.ToDictionary(a => a.Key, a => string.Join(";", a.Value));
            // IDictionary<string, string> cookies = this.Request.Cookies.ToDictionary(a => a.Key, a => string.Join(";", a.Value));
            return this.Ok(new {
                info = $"200 - {this.GetType().Name}",
                method = this.Request.Method,
                headers,
                // cookies = cookies,
                body = json
            });
        }

        [HttpGet]
        [HttpDelete]
        [SwaggerOperation(Summary = "Untuk test Echo saja")]
        public IActionResult EchoNoData() {
            return this.ReturnData(null);
        }

        [HttpPost]
        [HttpPut]
        [HttpPatch]
        [SwaggerOperation(Summary = "Untuk test Echo saja")]
        public IActionResult EchoWithData(
            [FromBody, SwaggerParameter("JSON body", Required = false)] dynamic obj
        ) {
            return this.ReturnData(obj);
        }

    }

}
