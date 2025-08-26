/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Middleware API Key
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using bifeldy_sd3_lib_60.Exceptions;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Repositories;
using bifeldy_sd3_lib_60.Services;

namespace bifeldy_sd3_lib_60.Middlewares {

    public sealed class AutoCheckKunciKodeMultiDcMiddleware {

        private readonly EnvVar _env;

        private readonly RequestDelegate _next;
        private readonly ILogger<AutoCheckKunciKodeMultiDcMiddleware> _logger;
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;

        public AutoCheckKunciKodeMultiDcMiddleware(
            RequestDelegate next,
            IOptions<EnvVar> env,
            ILogger<AutoCheckKunciKodeMultiDcMiddleware> logger,
            IApplicationService app,
            IGlobalService gs
        ) {
            this._next = next;
            this._env = env.Value;
            this._logger = logger;
            this._app = app;
            this._gs = gs;
        }

        public async Task Invoke(HttpContext context, IServerConfigRepository scr) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            try {
                if (this._app.DebugMode) {
                    string proxyPath = this._env.DEV_PATH_BASE;

                    if (!string.IsNullOrEmpty(proxyPath)) {
                        if (!proxyPath.StartsWith("/")) {
                            proxyPath = $"/{proxyPath}";
                        }

                        if (context.Request.Headers.ContainsKey(Bifeldy.NGINX_PATH_NAME)) {
                            _ = context.Request.Headers.Remove(Bifeldy.NGINX_PATH_NAME);
                        }

                        context.Request.Headers.Add(Bifeldy.NGINX_PATH_NAME, proxyPath);
                    }
                }

                context.Items["KunciKodeDc"] = scr.CurrentLoadedKodeServerKunciDc();

                int shortCircuit = 0;
                object res = null;

                if (context.Request.Path.Value.Equals("/server-config", StringComparison.InvariantCultureIgnoreCase)) {
                    try {
                        if (context.Request.Method == "GET") {
                            List<ServerConfigKunci> config = await scr.GetKodeServerKunciDc();

                            res = new ResponseJsonMulti<ServerConfigKunci>() {
                                info = "200 - Kunci Kode DC",
                                results = config,
                                count = config.Count,
                                pages = 1
                            };

                            shortCircuit = StatusCodes.Status200OK;
                        }
                        else {
                            ServerConfigKunciAddEditDelete reqBody = await this._gs.GetHttpRequestBody<ServerConfigKunciAddEditDelete>(context.Request);

                            if (reqBody == null || string.IsNullOrEmpty(reqBody?.password)) {
                                throw new TidakMemenuhiException("Data Tidak Lengkap!");
                            }

                            string info = null;
                            string message = null;

                            if (!reqBody.password.Equals("5p1nd0m@r3T", StringComparison.InvariantCultureIgnoreCase)) {
                                info = "401 - Kunci Kode DC";
                                message = "Password Salah";
                                shortCircuit = StatusCodes.Status401Unauthorized;
                            }
                            else if (context.Request.Method == "POST" && reqBody != null) {
                                if (reqBody.type.ToUpper() == "TAMBAH") {
                                    _ = await scr.AddKodeServerKunciDc(reqBody.kode_dc, reqBody.kunci_gxxx, reqBody.server_target);
                                    info = "201 - Kunci Kode DC";
                                    message = "Berhasil Menambah Kunci";
                                    shortCircuit = StatusCodes.Status201Created;
                                }
                                else if (reqBody.type.ToUpper() == "UBAH") {
                                    _ = await scr.EditKodeServerKunciDc(reqBody.kode_dc, reqBody.kunci_gxxx, reqBody.server_target);
                                    info = "202 - Kunci Kode DC";
                                    message = "Berhasil Mengubah Kunci";
                                    shortCircuit = StatusCodes.Status202Accepted;
                                }
                                else if (reqBody.type.ToUpper() == "HAPUS") {
                                    _ = await scr.RemoveKodeServerKunciDc(reqBody.kode_dc);
                                    info = "202 - Kunci Kode DC";
                                    message = "Berhasil Menghapus Kunci";
                                    shortCircuit = StatusCodes.Status202Accepted;
                                }

                                // TODO :: New Features ~
                            }

                            if (string.IsNullOrEmpty(info) || string.IsNullOrEmpty(message)) {
                                throw new TidakMemenuhiException("Data Tidak Lengkap!");
                            }

                            res = new ResponseJsonSingle<ResponseJsonMessage>() {
                                info = info,
                                result = new ResponseJsonMessage() {
                                    message = message
                                }
                            };
                        }
                    }
                    catch (TidakMemenuhiException e) {
                        shortCircuit = StatusCodes.Status400BadRequest;
                        res = new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = "400 - Kunci Kode DC",
                            result = new ResponseJsonMessage() {
                                message = e.Message
                            }
                        };
                    }
                    catch (Exception e) {
                        shortCircuit = StatusCodes.Status500InternalServerError;
                        res = new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = "500 - Whoops :: Terjadi Kesalahan",
                            result = new ResponseJsonMessage() {
                                message = this._app.DebugMode ? e.Message : "Gagal Melanjutkan Permintaan"
                            }
                        };
                    }
                }

                if (shortCircuit > 0 && res != null) {
                    context.Response.StatusCode = shortCircuit;
                    await context.Response.WriteAsJsonAsync(res);
                    return;
                }

                await this._next(context);
            }
            catch (KunciServerTidakTersediaException ex) {
                string redirectUrl = $"/server-config.html";

                if (context.Request.Path.ToUriComponent().Contains(redirectUrl, StringComparison.InvariantCultureIgnoreCase)) {
                    throw;
                }

                if (!this._app.DebugMode && context.Request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
                    string proxyPath = pathBase.Last();
                    if (!string.IsNullOrEmpty(proxyPath)) {
                        redirectUrl = $"{proxyPath}{redirectUrl}";
                    }
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                context.Response.Headers.Location = $"{redirectUrl}?errorInfo={ex.Message}";
            }
        }

    }

}
