using Microsoft.AspNetCore.Http;

using Serilog.Core;
using Serilog.Events;

namespace bifeldy_sd3_lib_60.Libraries {

    public class SerilogKunciKodeDcPropertyEnricher : ILogEventEnricher {

        private readonly IHttpContextAccessor _hca;

        public SerilogKunciKodeDcPropertyEnricher(IHttpContextAccessor hca) {
            this._hca = hca;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
            string kunciKodeDc = null;

            if (this._hca.HttpContext != null) {
                kunciKodeDc = this._hca.HttpContext.Items["KunciKodeDc"]?.ToString();
            }

            LogEventProperty prop = propertyFactory.CreateProperty("KunciKodeDc", kunciKodeDc);
            logEvent.AddPropertyIfAbsent(prop);
        }

    }

}
