using Microsoft.AspNetCore.Http;

using Serilog.Core;
using Serilog.Events;

namespace bifeldy_sd3_lib_60.Libraries {

    public class SerilogKunciGxxxPropertyEnricher : ILogEventEnricher {

        private readonly IHttpContextAccessor _hca;

        public SerilogKunciGxxxPropertyEnricher(IHttpContextAccessor hca) {
            this._hca = hca;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
            string kunciGxxx = null;

            if (this._hca.HttpContext != null) {
                kunciGxxx = this._hca.HttpContext.Items["kunci_gxxx"]?.ToString();
            }

            LogEventProperty prop = propertyFactory.CreateProperty("KunciGxxx", kunciGxxx);
            logEvent.AddPropertyIfAbsent(prop);
        }

    }

}
