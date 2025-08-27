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

using System.Net;

using Microsoft.AspNetCore.Http;
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
        private readonly IApplicationService _app;
        private readonly IGlobalService _gs;

        public static string HTML = @"
            <!DOCTYPE html>
            <html dir=""ltr"" lang=""id"">

                <head>

                    <base href=""./"" />

                    <meta charset=""utf-8"" />
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"" />

                    <title>Server Config</title>

                    <link rel=""icon"" type=""image/x-icon"" href=""./favicon.ico"" />
                    <link rel=""stylesheet"" href=""./bootstrap.min.css"" />

                    <script src=""./bootstrap.bundle.min.js""></script>

                </head>

                <body>

                    <div class=""d-flex justify-content-center align-items-center min-vh-100"">
                        <div class=""text-center"">
                            <img src=""./images/logo_indomaret.png"" class=""p-1"" style=""width: 384px"" />
                            <div role=""alert"" class=""alert alert-danger alert-dismissible fade show"" id=""alertError"">
                                <button type=""button"" class=""btn-close"" data-bs-dismiss=""alert"" aria-label=""Close""></button>
                                <h5 class=""p-1 alert-heading"">Maaf -- Ada Error Nih 🔥</h5>
                                <p id=""errorInfo"">Terjadi Kesalahan Saat Sedang Memproses Permintaan Kamu ~</p>
                                <hr />
                                <p class=""mb-0"">~ (｡&gt;﹏&lt;｡) ~ Kemungkinan Server Kuncinya Bermasalah / Alamatnya Belum Di Atur 🗿</p>
                            </div>
                            <div role=""alert"" class=""alert alert-info"" id=""alertInfo"" style=""display: none;""></div>
                            <h3 class=""p-1"" id=""ipServer""></h3>
                            <div class=""d-flex justify-content-center flex-wrap p-3"" id=""listDc""></div>
                        </div>
                    </div>

                    <form class=""modal fade was-validated"" id=""addEditListDc"" aria-hidden=""true"" aria-labelledby=""addEditListDcTitle"">
                        <div class=""modal-dialog modal-dialog-centered"">
                            <div class=""modal-content"">
                                <div class=""modal-header"">
                                    <h1 class=""modal-title fs-5"">
                                        <span id=""addEditListDcTitle""></span> Kode DC
                                    </h1>
                                    <button type=""button"" class=""btn-close"" data-bs-dismiss=""modal"" aria-label=""Close""></button>
                                </div>
                                <div class=""modal-body"">
                                    <div class=""row align-items-center m-2"">
                                        <div class=""p-2"">
                                            <label for=""txtKodeDc"" class=""form-label"">Kode DC</label>
                                            <input type=""text"" class=""form-control"" id=""txtKodeDc"" placeholder=""g###"" required />
                                            <div class=""invalid-feedback"">Tidak Boleh Kosong</div>
                                        </div>
                                        <div class=""p-2"">
                                            <label for=""txtKunci"" class=""form-label"">Alamat Kunci</label>
                                            <input type=""text"" class=""form-control"" id=""txtKunci"" placeholder=""kuncig###"" required />
                                            <div class=""invalid-feedback"">Harap Di Isi Sesuai Dengan Server Kunci Yang Tersedia => `kunci.yml`</div>
                                            <div id=""txtKunciOk"" class=""valid-feedback""></div>
                                        </div>
                                        <div class=""p-2"">
                                            <label for=""txtServer"" class=""form-label"">NginX Path</label>
                                            <input type=""text"" class=""form-control"" id=""txtServer"" placeholder=""/app-g###"" required />
                                            <div class=""invalid-feedback"">Harap Di Isi Mengikuti Konfigurasi NginX => `default.conf`</div>
                                            <div id=""txtServerOk"" class=""valid-feedback""></div>
                                        </div>
                                        <div class=""p-2"">
                                            <label for=""txtPassword"" class=""form-label"">5p.........</label>
                                            <input type=""password"" class=""form-control"" id=""txtPassword"" required />
                                            <div class=""invalid-feedback"">Tidak Boleh Kosong</div>
                                        </div>
                                    </div>
                                </div>
                                <div class=""modal-footer"">
                                    <button type=""button"" class=""btn btn-warning me-auto"" data-bs-target=""#previewNginxConfig"" data-bs-toggle=""modal"" data-bs-dismiss=""modal"" id=""btnPreview"">Contoh Preview</button>
                                    <button type=""button"" class=""btn btn-danger"" style=""display: none;"" id=""btnDelete"" data-bs-toggle=""modal"" data-bs-dismiss=""modal"">Hapus</button>
                                    <button type=""submit"" class=""btn btn-primary"" id=""btnSave"" data-bs-toggle=""modal"" data-bs-dismiss=""modal"">Simpan</button>
                                </div>
                            </div>
                        </div>
                    </form>

                    <div class=""modal fade"" id=""previewNginxConfig"" aria-hidden=""true"" aria-labelledby=""previewNginxConfigTitle"" data-bs-keyboard=""false"" data-bs-backdrop=""static"">
                        <div class=""modal-dialog modal-dialog-centered modal-lg"">
                            <div class=""modal-content"">
                                <div class=""modal-header"">
                                    <h1 class=""modal-title fs-5"" id=""previewNginxConfigTitle""></h1>
                                </div>
                                <div class=""modal-body"">
                                    <textarea class=""form-control"" id=""txtConfigNginx"" rows=""10"" readonly></textarea>
                                </div>
                                <div class=""modal-footer"">
                                    <button class=""btn btn-secondary"" data-bs-target=""#addEditListDc"" data-bs-toggle=""modal"" data-bs-dismiss=""modal"">Kembali</button>
                                </div>
                            </div>
                        </div>
                    </div>

                    <style>
                        .card .card-img-top {
                            height: 128px;
                            width: 128px;
                        }

                        .card-glow {
                            transition: box-shadow 0.3s ease-in-out, border-color 0.3s ease-in-out;
                            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
                        }

                        .card-glow:hover {
                            border-color: rgba(0, 123, 255, 0.7);
                        }
                    </style>

                    <script defer>
                        const queryString = window.location.search;
                        const urlParams = new URLSearchParams(queryString);

                        const listDcEl = document.getElementById('listDc');

                        const ipServerEl = document.getElementById('ipServer');
                        const alertErrorEl = document.getElementById('alertError');
                        const alertInfoEl = document.getElementById('alertInfo');

                        const errorInfoEl = document.getElementById('errorInfo');
                        const errorInfo = urlParams.get('errorInfo');

                        const addEditListDcEl = document.getElementById('addEditListDc');
                        const txtKunciOkEl = document.getElementById('txtKunciOk');
                        const txtServerOkEl = document.getElementById('txtServerOk');

                        const modalAddEditListDcTitleEl = document.getElementById('addEditListDcTitle');
                        const modalPreviewNginxConfigTitleEl = document.getElementById('previewNginxConfigTitle');

                        const modalBtnPreviewEl = document.getElementById('btnPreview');
                        const modalBtnDeleteEl = document.getElementById('btnDelete');
                        const modalBtnSaveEl = document.getElementById('btnSave');

                        const txtKodeDcEl = document.getElementById('txtKodeDc');
                        const txtKunciEl = document.getElementById('txtKunci');
                        const txtServerEl = document.getElementById('txtServer');
                        const txtConfigNginxEl = document.getElementById('txtConfigNginx');
                        const txtPasswordEl = document.getElementById('txtPassword');

                        const eventInputTrigger = document.createEvent('Event');
                        eventInputTrigger.initEvent('input', true, false);

                        ipServerEl.innerHTML = `Informasi Kunci Server Kode DC Lainnya :: ${document.location.host}`;
                        errorInfoEl.innerHTML = errorInfo;

                        if (!errorInfo) {
                            alertErrorEl.style.display = 'none';
                        }
                        else {
                            urlParams.delete('errorInfo');
                            const newUrl = window.location.pathname + (urlParams.toString() ? '?' + urlParams.toString() : '');
                            window.history.replaceState({}, document.title, newUrl);
                        }

                        let listDc = {
                            dcho: {
                                server_target: '/datadcho',
                                kunci: 'cloudproxykuncidcho'
                            },
                            whho: {
                                server_target: '/datawhho',
                                kunci: 'cloudproxykunciwhho'
                            },
                            pgcbn: {
                                server_target: '/datapgcbn',
                                kunci: 'kuncipgcbn'
                            },
                            g000: {
                                server_target: '/datadcg000',
                                kunci: 'kuncig000'
                            },
                            gxxx: {
                                server_target: '/datadcgxxx',
                                kunci: 'kuncigxxx'
                            },
                            g999: {
                                server_target: '/datadcg999',
                                kunci: 'kuncig999'
                            }
                        };

                        async function checkErrorXhr(xhr) {
                            let ok = true;

                            if (!xhr.ok) {
                                ok = false;
                                alertInfoEl.style.display = 'block';

                                try {
                                    const res = await xhr.json();
                                    alertInfoEl.innerHTML = `<string>[API]</string> ${res.result.message} 😭`;
                                }
                                catch (e) {
                                    alertInfoEl.innerHTML = `<string>[API]</string> Gagal Memproses Permintaan, Silahkan Coba Lagi Nanti :: ${e} 😭`;
                                }
                            }

                            return ok;
                        }

                        async function loadListDc() {
                            listDc = {};

                            alertInfoEl.style.display = 'block';
                            alertInfoEl.innerHTML = `<string>[API]</string> Memuat Data... 🤣`;

                            const xhr = await fetch('./server-config');

                            if (!await checkErrorXhr(xhr)) {
                                return;
                            }

                            alertInfoEl.style.display = 'none';

                            const res = await xhr.json();

                            for (const r of res.results) {
                                if (!listDc[r.kode_dc]) {
                                    listDc[r.kode_dc] = {};
                                }

                                listDc[r.kode_dc].server_target = r.server_target;
                                listDc[r.kode_dc].kunci = r.kunci_gxxx;
                            }

                            listDcEl.innerHTML = ``;

                            for (const key in listDc) {
                                const dc = listDc[key];

                                listDcEl.innerHTML += `
                                    <div class=""card p-2 m-2 card-glow"">
                                        <img class=""rounded-circle card-img-top img-thumbnail"" src=""./images/domar.gif"" />
                                        <div class=""card-body p-2"">
                                            <h5 class=""card-title m-0"">${key.toUpperCase()}</h5>
                                            <a
                                                class=""text-decoration-none"" onclick=editListDc('${key}')
                                                style=""cursor: pointer;""
                                                data-bs-target=""#addEditListDc""
                                                data-bs-toggle=""modal""
                                            >
                                                Edit
                                            </a>
                                            |
                                            <a href=""${dc.server_target}"" class=""text-decoration-none"">Buka</a>
                                        </div>
                                    </div>
                                `;
                            }

                            listDcEl.innerHTML += `
                                <div class=""card p-2 m-2 card-glow"">
                                    <img class=""rounded-circle card-img-top img-thumbnail"" src=""./favicon.ico"" />
                                    <div class=""card-body p-2"">
                                        <h5 class=""card-title m-0"">?????</h5>
                                        <a
                                            class=""text-decoration-none stretched-link""
                                            onclick=""addListDc()""
                                            style=""cursor: pointer;""
                                            data-bs-target=""#addEditListDc""
                                            data-bs-toggle=""modal""
                                        >
                                            Tambah Baru
                                        </a>
                                    </div>
                                </div>
                            `;
                        }

                        async function addListDc() {
                            modalAddEditListDcTitleEl.innerHTML = 'TAMBAH';
                            modalBtnDeleteEl.style.display = 'none';
                            txtKodeDcEl.value = null;
                            txtKodeDcEl.readOnly = false;
                            txtKunciEl.value = null;
                            txtKunciEl.dispatchEvent(eventInputTrigger);
                            txtServerEl.value = null;
                            txtServerEl.dispatchEvent(eventInputTrigger);
                        }

                        async function editListDc(kodeDc) {
                            modalAddEditListDcTitleEl.innerHTML = 'UBAH';
                            modalBtnDeleteEl.style.display = 'block';
                            txtKodeDcEl.value = kodeDc;
                            txtKodeDcEl.readOnly = true;
                            txtKunciEl.value = listDc[kodeDc].kunci;
                            txtKunciEl.dispatchEvent(eventInputTrigger);
                            txtServerEl.value = listDc[kodeDc].server_target;
                            txtServerEl.dispatchEvent(eventInputTrigger);
                        }

                        txtKodeDcEl.addEventListener('input', ev => {
                            ev.target.value = ev.target.value.toLowerCase();
                            txtKunciEl.value = `kunci${ev.target.value}`;
                            txtKunciEl.dispatchEvent(eventInputTrigger);
                            txtServerEl.value = `/app-${ev.target.value}`;
                            txtServerEl.dispatchEvent(eventInputTrigger);
                        });

                        txtKunciEl.addEventListener('input', ev => {
                            ev.target.value = ev.target.value.toLowerCase();
                            txtKunciOkEl.innerHTML = `Akan Menyambung Ke ${document.location.protocol}//${document.location.host}/${ev.target.value}`;
                        });

                        txtServerEl.addEventListener('input', ev => {
                            ev.target.value = ev.target.value.toLowerCase();
                            if (ev.target.value && !ev.target.value.startsWith('/')) {
                                ev.target.value = `/${ev.target.value}`;
                            }
                            txtServerOkEl.innerHTML = `Dapat Di Akses Dengan ${document.location.protocol}//${document.location.host}${ev.target.value}`;
                        });

                        modalBtnPreviewEl.addEventListener('click', async (ev) => {
                            ev.preventDefault();
                            ev.stopPropagation();

                            modalPreviewNginxConfigTitleEl.innerHTML = `Pratinjau Konfigurasi NginX :: ${txtKodeDcEl.value}`;

                            let path = txtServerEl.value;
                            if (path.startsWith('/')) {
                                path = path.substring(1);
                            }

                            txtConfigNginxEl.value = `
                                location ~* ^/(${path})/(.*)$ {
                                    proxy_http_version 1.1;
                                    proxy_set_header Host $host;
                                    proxy_set_header Upgrade $http_upgrade;
                                    proxy_set_header Connection Upgrade;
                                    proxy_set_header X-Forwarded-Host $host;
                                    proxy_set_header CF-Connecting-IP $proxy_protocol_addr;
                                    proxy_set_header X-Real-IP $remote_addr;
                                    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                                    proxy_set_header X-Forwarded-Proto $scheme;
                                    proxy_set_header X-Forwarded-Prefix /$1;

                                    # Ganti Target Dengan IP & Port Aplikasinya
                                    # nama-docker-container:port / localhost:port
                                    # Jangan Lupa Di Akhiri Slash
                                    set $upstream http://api-bla***bla-1:80/;

                                    proxy_pass $upstream$2$is_args$args;
                                }
                            `.replaceAll('                    ', '').trim();
                        });

                        modalBtnDeleteEl.addEventListener('click', async (ev) => {
                            ev.preventDefault();
                            ev.stopPropagation();

                            alertInfoEl.style.display = 'block';
                            alertInfoEl.innerHTML = `<string>[API]</string> Menghapus Data ... 🤐`;

                            const xhr = await fetch(
                                './server-config',
                                {
                                    method: 'POST',
                                    headers: {
                                        'content-type': 'application/json'
                                    },
                                    body: JSON.stringify({
                                        kode_dc: txtKodeDcEl.value?.trim(),
                                        kunci_gxxx: txtKunciEl.value?.trim(),
                                        server_target: txtServerEl.value?.trim(),
                                        password: txtPasswordEl.value?.trim(),
                                        type: 'HAPUS'
                                    })
                                }
                            );

                            if (!await checkErrorXhr(xhr)) {
                                return;
                            }

                            alertInfoEl.style.display = 'none';

                            await loadListDc();
                        });

                        modalBtnSaveEl.addEventListener('click', async (ev) => {
                            ev.preventDefault();
                            ev.stopPropagation();

                            alertInfoEl.style.display = 'block';
                            alertInfoEl.innerHTML = `<string>[API]</string> Menyimpan Data ... 🥰`;

                            const xhr = await fetch(
                                './server-config',
                                {
                                    method: 'POST',
                                    headers: {
                                        'content-type': 'application/json'
                                    },
                                    body: JSON.stringify({
                                        kode_dc: txtKodeDcEl.value?.trim(),
                                        kunci_gxxx: txtKunciEl.value?.trim(),
                                        server_target: txtServerEl.value?.trim(),
                                        password: txtPasswordEl.value?.trim(),
                                        type: modalAddEditListDcTitleEl.innerText?.trim()
                                    })
                                }
                            );

                            if (!await checkErrorXhr(xhr)) {
                                return;
                            }

                            alertInfoEl.style.display = 'none';

                            await loadListDc();
                        });

                        loadListDc();
                    </script>

                </body>

            </html>
        ";

        public AutoCheckKunciKodeMultiDcMiddleware(
            RequestDelegate next,
            IOptions<EnvVar> env,
            IApplicationService app,
            IGlobalService gs
        ) {
            this._next = next;
            this._env = env.Value;
            this._app = app;
            this._gs = gs;
        }

        public async Task Invoke(HttpContext context, IServerConfigRepository scr) {
            ConnectionInfo connection = context.Connection;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            try {
                if (context.Request.Path.Value.StartsWith("/server-config.html", StringComparison.InvariantCultureIgnoreCase)) {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(HTML);
                    return;
                }

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

                await this._next(context);
            }
            catch (KunciServerTidakTersediaException ex) {
                string redirectUrl = "/server-config.html";
                string encodedString = WebUtility.UrlEncode(ex.Message);

                if (!this._app.DebugMode && context.Request.Headers.TryGetValue(Bifeldy.NGINX_PATH_NAME, out StringValues pathBase)) {
                    string proxyPath = pathBase.Last();
                    if (!string.IsNullOrEmpty(proxyPath)) {
                        redirectUrl = $"{proxyPath}{redirectUrl}";
                    }
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                context.Response.Headers.Location = $"{redirectUrl}?errorInfo={encodedString}";
            }
        }

    }

}
