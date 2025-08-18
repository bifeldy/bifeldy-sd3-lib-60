/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: RDLC Report
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Data;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Reporting.NETCore;

using DinkToPdf;

using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IRdlcService {
        IDictionary<string, RdlcInfo> FileType { get; }
        LocalReport CreateLocalReport(string rdlcName, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null);
        ReportDataSource CreateReportDataSource(string name, DataTable dt);
        ReportDataSource CreateReportDataSource<T>(string name, List<T> dt);
        HtmlToPdfDocument GenerateHtmlReport(RdlcReport reportModel);
        ReportParameter[] CreateReportParameter(IDictionary<string, string> dict);
        RdlcReport GeneratePdfWordExcelHtmlReport(string rdlcName, DataTable dt, string dsName, IEnumerable<ReportParameter> param = null, string fileType = "HTML5", MarginSettings margin = null, Orientation pageOrientation = Orientation.Portrait, PaperKind paperType = PaperKind.Custom);
        RdlcReport GeneratePdfWordExcelHtmlReport<T>(string rdlcName, List<T> ls, string dsName, IEnumerable<ReportParameter> param = null, string fileType = "HTML5", MarginSettings margin = null, Orientation pageOrientation = Orientation.Portrait, PaperKind paperType = PaperKind.Custom);
    }

    [SingletonServiceRegistration]
    public sealed class CRdlcService : IRdlcService {

        private readonly IWebHostEnvironment _he;
        private readonly IApplicationService _app;
        private readonly IConverterService _converter;

        public IDictionary<string, RdlcInfo> FileType { get; } = new Dictionary<string, RdlcInfo>(StringComparer.InvariantCultureIgnoreCase) {
            {
                "PDF", new() {
                    contentType = "application/pdf",
                    saveType = "PDF"
                }
            },
            {
                "DOCX", new() {
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    saveType = "WORDOPENXML"
                }
            },
            {
                "XLSX", new() {
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    saveType = "EXCELOPENXML"
                }
            },
            {
                "HTML", new() {
                    contentType = "text/html",
                    saveType = "html5"
                }
            }
        };

        public CRdlcService(IWebHostEnvironment he, IApplicationService app, IConverterService converter) {
            this._he = he;
            this._app = app;
            this._converter = converter;
        }

        public LocalReport CreateLocalReport(string rdlcName, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null) {
            string rdlcPath = Path.Combine(this._app.AppLocation, "Rdlcs", rdlcName);

            if (!File.Exists(rdlcPath)) {
                rdlcPath = Path.Combine(this._he.ContentRootPath, "rdlcs", rdlcName);

                if (!File.Exists(rdlcPath)) {
                    throw new FileNotFoundException($"File RDLC {rdlcName} Tidak Ditemukan!", rdlcPath);
                }
            }

            var report = new LocalReport() {
                ReportPath = rdlcPath
            };

            if (ds != null) {
                report.DisplayName = ds.Name;
                report.DataSources.Add(ds);
            }

            if (param != null) {
                report.SetParameters(param);
            }

            return report;
        }

        public ReportDataSource CreateReportDataSource(string name, DataTable dt) => new(name, dt);

        public ReportDataSource CreateReportDataSource<T>(string name, List<T> ls) => new(name, ls);

        public HtmlToPdfDocument GenerateHtmlReport(RdlcReport reportModel) {
            return new HtmlToPdfDocument() {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = reportModel.PageOrientation,
                    Margins = reportModel.Margins,
                    DocumentTitle = reportModel.DisplayName,
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = reportModel.HtmlContent,
                        WebSettings = {
                            DefaultEncoding = "utf-8"
                        }
                    }
                }
            };
        }

        public ReportParameter[] CreateReportParameter(IDictionary<string, string> dict) {
            var ls = new List<ReportParameter>();
            foreach (KeyValuePair<string, string> kvp in dict) {
                ls.Add(new ReportParameter(kvp.Key, kvp.Value));
            }

            return ls.ToArray();
        }

        private MarginSettings SetupPage() {
            return new MarginSettings() {
                Top = 1,
                Bottom = 1,
                Left = 1,
                Right = 1,
                Unit = Unit.Centimeters
            };
        }

        private RdlcReport GenerateReport(
            string rdlcName,
            ReportDataSource rds,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML",
            MarginSettings margin = null,
            Orientation pageOrientation = Orientation.Portrait,
            PaperKind paperType = PaperKind.Custom
        ) {
            LocalReport report = this.CreateLocalReport(rdlcName, rds, param);

            var model = new RdlcReport() {
                DisplayName = report.DisplayName,
                Margins = margin,
                PageOrientation = pageOrientation,
                PaperType = paperType,
                RenderType = this.FileType[fileType].saveType
            };

            if (fileType == "PDF" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                model.RenderType = "HTML5";
                model.HtmlContent = Encoding.UTF8.GetString(report.Render(model.RenderType));
                model.Report = this._converter.HtmlToPdf(this.GenerateHtmlReport(model));
            }
            else {
                model.Report = report.Render(model.RenderType);
            }

            return model;
        }

        public RdlcReport GeneratePdfWordExcelHtmlReport(
            string rdlcName,
            DataTable dt,
            string dsName,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML",
            MarginSettings margin = null,
            Orientation pageOrientation = Orientation.Portrait,
            PaperKind paperType = PaperKind.Custom
        ) {
            margin ??= this.SetupPage();
            ReportDataSource rds = this.CreateReportDataSource(dsName, dt);
            return this.GenerateReport(rdlcName, rds, param, fileType, margin, pageOrientation, paperType);
        }

        public RdlcReport GeneratePdfWordExcelHtmlReport<T>(
            string rdlcName,
            List<T> ls,
            string dsName,
            IEnumerable<ReportParameter> param = null,
            string fileType = "HTML",
            MarginSettings margin = null,
            Orientation pageOrientation = Orientation.Portrait,
            PaperKind paperType = PaperKind.Custom
        ) {
            margin ??= this.SetupPage();
            ReportDataSource rds = this.CreateReportDataSource(dsName, ls);
            return this.GenerateReport(rdlcName, rds, param, fileType, margin, pageOrientation, paperType);
        }

    }

}
