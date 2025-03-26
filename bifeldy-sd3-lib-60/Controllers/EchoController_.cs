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
using Microsoft.Extensions.Primitives;

using Swashbuckle.AspNetCore.Annotations;

namespace bifeldy_sd3_lib_60.Controllers {

    [ApiController]
    [Route("echo")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class EchoController : ControllerBase {

        public IActionResult ReturnData(dynamic json = null) {
            var query = new Dictionary<string, dynamic>(StringComparer.InvariantCultureIgnoreCase);
            foreach (KeyValuePair<string, StringValues> data in this.Request.Query) {
                if (data.Value.Count > 1) {
                    query.Add(data.Key, data.Value.ToArray());
                }
                else {
                    query.Add(data.Key, string.Join("; ", data.Value));
                }
            }

            IDictionary<string, string> headers = this.Request.Headers.Where(d => !d.Key.ToUpper().StartsWith("COOKIE")).ToDictionary(a => a.Key, a => string.Join("; ", a.Value));
            IDictionary<string, string> cookies = this.Request.Cookies.ToDictionary(a => a.Key, a => string.Join("; ", a.Value));

            return this.Ok(new {
                info = $"200 - {this.GetType().Name}",
                method = this.Request.Method,
                query,
                headers,
                cookies,
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
