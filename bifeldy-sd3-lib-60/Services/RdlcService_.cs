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
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Reporting.NETCore;

using DinkToPdf;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Services {

    public interface IRdlcService {
        IDictionary<string, dynamic> fileType { get; }
        LocalReport CreateLocalReport(string rdlcPath, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null);
        ReportDataSource CreateReportDataSource(string name, DataTable dt);
        ReportParameter[] CreateReportParameter(IDictionary<string, string> dict);
        HtmlToPdfDocument GenerateHtmlReport(RdlcReport reportModel);
        RdlcReport GeneratePdfWordExcelReport(string rdlcPath, DataTable dt, string dsName, IEnumerable<ReportParameter> param = null, string saveAs = "HTML5", MarginSettings margin = null, Orientation pageOrientation = Orientation.Portrait, PaperKind paperType = PaperKind.Custom);
    }

    public sealed class CRdlcService : IRdlcService {

        private readonly IWebHostEnvironment _he;
        private readonly IConverterService _converter;

        public IDictionary<string, dynamic> fileType { get; } = new Dictionary<string, dynamic> {
            {
                "PDF", new {
                    contentType = "application/pdf",
                    extension = "pdf"
                }
            },
            {
                "WORDOPENXML", new {
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    extension = "docx"
                }
            },
            {
                "EXCELOPENXML", new {
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    extension = "xlsx"
                }
            },
            {
                "HTML5", new {
                    contentType = "application/pdf",
                    extension = "pdf"
                }
            }
        };

        public CRdlcService(IWebHostEnvironment he, IConverterService converter) {
            _he = he;
            _converter = converter;
        }

        public LocalReport CreateLocalReport(string rdlcPath, ReportDataSource ds = null, IEnumerable<ReportParameter> param = null) {
            var report = new LocalReport {
                ReportPath = $"{_he.ContentRootPath}/wwwroot/rdlcs/{rdlcPath}"
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

        public ReportDataSource CreateReportDataSource(string name, DataTable dt) {
            return new ReportDataSource(name, dt);
        }

        public ReportParameter[] CreateReportParameter(IDictionary<string, string> dict) {
            var ls = new List<ReportParameter>();
            foreach (KeyValuePair<string, string> kvp in dict) {
                ls.Add(new ReportParameter(kvp.Key, kvp.Value));
            }
            return ls.ToArray();
        }

        public HtmlToPdfDocument GenerateHtmlReport(RdlcReport reportModel) {
            return new HtmlToPdfDocument {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = reportModel.PageOrientation,
                    Margins = reportModel.Margins,
                    DocumentTitle = reportModel.DisplayName,
                },
                Objects = {
                    new ObjectSettings {
                        HtmlContent = Encoding.UTF8.GetString(reportModel.Report),
                        WebSettings = {
                            DefaultEncoding = "utf-8"
                        }
                    }
                }
            };
        }

        public RdlcReport GeneratePdfWordExcelReport(
            string rdlcPath,
            DataTable dt,
            string dsName,
            IEnumerable<ReportParameter> param = null,
            string saveAs = "HTML5",
            MarginSettings margin = null,
            Orientation pageOrientation = Orientation.Portrait,
            PaperKind paperType = PaperKind.Custom
        ) {
            if (margin == null) {
                margin = new MarginSettings {
                    Top = 1,
                    Bottom = 1,
                    Left = 1,
                    Right = 1,
                    Unit = Unit.Centimeters
                };
            }
            var ds = CreateReportDataSource(dsName, dt);
            var report = CreateLocalReport(rdlcPath, ds, param);
            var model = new RdlcReport {
                DisplayName = report.DisplayName,
                Margins = margin,
                PageOrientation = pageOrientation,
                PaperType = paperType,
                RenderType = saveAs,
                Report = report.Render(saveAs)
            };
            if (model.RenderType == "HTML5") {
                model.HtmlContent = Encoding.UTF8.GetString(model.Report);
                model.Report = _converter.HtmlToPdf(GenerateHtmlReport(model));
            }
            return model;
        }

    }

}
