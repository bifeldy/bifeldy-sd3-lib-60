/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Hash Files Untuk Check Update
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using Swashbuckle.AspNetCore.Annotations;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Databases;

namespace bifeldy_sd3_lib_60.Controllers {

    [ApiController]
    [Route("downloader")]
    [RouteExcludeAllDc]
    [MinRole(UserSessionRole.EXTERNAL_BOT)]
    [ApiExplorerSettings(GroupName = "_", IgnoreApi = true)]
    public sealed class DownloaderController : ControllerBase {

        private readonly IDistributedCache _cache;
        private readonly ISchedulerService _scheduler;

        private readonly IApplicationService _as;
        private readonly IGlobalService _gs;
        private readonly IChiperService _chiper;
        private readonly IConverterService _converter;
        private readonly IOraPg _orapg;
        private readonly IRdlcService _rdlc;

        public DownloaderController(
            IDistributedCache cache,
            ISchedulerService scheduler,
            IApplicationService @as,
            IGlobalService gs,
            IChiperService chiper,
            IConverterService converter,
            IOraPg orapg,
            IRdlcService rdlc
        ) {
            this._cache = cache;
            this._scheduler = scheduler;
            this._as = @as;
            this._gs = gs;
            this._chiper = chiper;
            this._converter = converter;
            this._orapg = orapg;
            this._rdlc = rdlc;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Untuk check Hash files")]
        public async Task<IActionResult> ListAllFiles(
            [FromQuery, SwaggerParameter("Nama file (ex. blablabla.csv)", Required = false)] string fileName = null,
            [FromQuery, SwaggerParameter("Tipe file (tersedia: 'csv', 'zip')", Required = false)] string fileType = null,
            [FromQuery, SwaggerParameter("Pastikan file sudah selesai ditulis dan bukan parsial", Required = false)] bool? completedOnly = false,
            [FromQuery, SwaggerParameter("Bandingkan file kalau berbeda akan dapat balikan unduhan baru", Required = false)] string compareMd5 = null
        ) {
            string cacheKey = this.HttpContext.Request.Path;

            try {
                var user = (UserApiSession)this.HttpContext.Items["user"];

                if (string.IsNullOrEmpty(fileName)) {
                    if (user.role > UserSessionRole.USER_SD_SSD_3) {
                        return this.StatusCode(StatusCodes.Status403Forbidden, new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"403 - {this.GetType().Name} :: Hash Files",
                            result = new ResponseJsonMessage() {
                                message = "Harap input nama file ?fileName=blablabla.ext"
                            }
                        });
                    }

                    Dictionary<string, string> fileHash = null;

                    string cache = await this._cache.GetStringAsync(cacheKey);
                    if (string.IsNullOrEmpty(cache)) {
                        fileHash = new Dictionary<string, string>();

                        IEnumerable<FileInfo> fileInfos = Directory.GetFiles(this._as.AppLocation, "*", SearchOption.AllDirectories)
                            .Where(p => {
                                string dataPath = Path.Combine(this._as.AppLocation, "_data");
                                return !p.Contains("appsettings.json") && !p.Contains(dataPath);
                            })
                            .Select(p => new FileInfo(p));
                            // .OrderByDescending(fi => fi.LastWriteTime);

                        foreach (FileInfo fi in fileInfos) {
                            string crc32 = this._chiper.CalculateCRC32File(fi.FullName);
                            if (crc32 != null) {
                                string key = fi.FullName.Replace(this._as.AppLocation, string.Empty);
                                fileHash[key] = crc32;
                            }
                        }

                        await this._cache.SetStringAsync(cacheKey, this._converter.ObjectToJson(fileHash), new DistributedCacheEntryOptions() {
                            SlidingExpiration = TimeSpan.FromMinutes(30)
                        });
                    }
                    else {
                        fileHash = this._converter.JsonToObject<Dictionary<string, string>>(cache);
                    }

                    if (fileHash.Count <= 0) {
                        return this.NotFound(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"404 - {this.GetType().Name} :: Hash Files",
                            result = new ResponseJsonMessage() {
                                message = "Tidak Tersedia Pembaharuan"
                            }
                        });
                    }

                    return this.Ok(new ResponseJsonSingle<dynamic>() {
                        info = $"200 - {this.GetType().Name} :: Hash Files",
                        result = fileHash
                    });
                }
                else {
                    while (fileName.StartsWith(".") || fileName.StartsWith("/") || fileName.StartsWith("\\") || fileName.StartsWith("~")) {
                        fileName = fileName[1..];
                    }

                    string dirPath = this._as.AppLocation;
                    string mimeType = null;

                    fileType = fileType?.ToUpper();
                    switch (fileType) {
                        case "CSV":
                            dirPath = this._gs.CsvFolderPath;
                            mimeType = "text/csv";
                            break;
                        case "ZIP":
                            dirPath = this._gs.ZipFolderPath;
                            mimeType = "application/zip";
                            break;
                        default:
                            bool isFound = false;

                            if (this._rdlc.FileType.ContainsKey(fileType)) {
                                isFound = true;
                                dirPath = this._gs.TempFolderPath;
                                mimeType = this._rdlc.FileType[fileType].contentType;
                            }

                            if (!isFound) {
                                if (user.role > UserSessionRole.USER_SD_SSD_3) {
                                    return this.StatusCode(StatusCodes.Status403Forbidden, new ResponseJsonSingle<ResponseJsonMessage>() {
                                        info = $"403 - {this.GetType().Name} :: Hash Files",
                                        result = new ResponseJsonMessage() {
                                            message = "Harap input tipe file ?fileType=csv / ?fileType=zip"
                                        }
                                    });
                                }

                                dirPath = this._as.AppLocation;
                                mimeType = null;
                            }

                            break;
                    }

                    string filePath = Path.Combine(dirPath, fileName);

                    if (!System.IO.File.Exists(filePath)) {
                        return this.NotFound(new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"404 - {this.GetType().Name} :: Hash Files",
                            result = new ResponseJsonMessage() {
                                message = "File Tidak Ditemukan"
                            }
                        });
                    }

                    var fi = new FileInfo(filePath);
                    bool isFileReady = false;

                    if (completedOnly.GetValueOrDefault()) {
                        string jobName = $"ExportFile___{fi.Name}";

                        bool isJobCompleted = await this._scheduler.CheckJobIsCompleted(
                            jobName,
                            this._orapg,
                            () => {
                                string ___filePath = fi.FullName;
                                bool ___additionalAndCheck = System.IO.File.Exists(___filePath);
                                return Task.FromResult(___additionalAndCheck);
                            }
                        );

                        if (isJobCompleted) {
                            isFileReady = true;
                        }
                    }
                    else {
                        if (System.IO.File.Exists(fi.FullName)) {
                            isFileReady = true;
                        }
                    }

                    if (!isFileReady) {
                        return this.StatusCode(StatusCodes.Status410Gone, new ResponseJsonSingle<ResponseJsonMessage>() {
                            info = $"404 - {this.GetType().Name} :: Hash Files",
                            result = new ResponseJsonMessage() {
                                message = "File Belum Tersedia"
                            }
                        });
                    }

                    string checksum = this._chiper.CalculateMD5File(fi.FullName);
                    this.Response.Headers.Add("md5", checksum);

                    if (string.IsNullOrEmpty(mimeType)) {
                        mimeType = this._chiper.GetMimeFile(fi.FullName);
                    }

                    if (!string.IsNullOrEmpty(compareMd5)) {
                        if (compareMd5 == checksum) {
                            return this.NoContent();
                        }
                    }

                    return this.PhysicalFile(fi.FullName, mimeType, fi.Name, true);
                }
            }
            catch {
                this._cache.Remove(cacheKey);

                return this.BadRequest(new ResponseJsonSingle<ResponseJsonMessage>() {
                    info = $"400 - {this.GetType().Name} :: Hash File",
                    result = new ResponseJsonMessage() {
                        message = "Terjadi kesalahan saat proses data!"
                    }
                });
            }
        }

    }

}
