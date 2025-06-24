/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Text;

using Renci.SshNet;

using bifeldy_sd3_lib_60.Models;

namespace bifeldy_sd3_lib_60.Extensions {

    public static class SshCommandExtensions {

        private static async Task CheckOutputAndReportProgress(SshCommand sshCommand, TextReader stdoutStreamReader, TextReader stderrStreamReader, CancellationToken cancellationToken, IProgress<CScriptOutputLine> progress = null) {
            if (cancellationToken.IsCancellationRequested) {
                sshCommand.CancelAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();

            await CheckStdoutAndReportProgressAsync(stdoutStreamReader, progress);
            await CheckStderrAndReportProgressAsync(stderrStreamReader, progress);
        }

        private static async Task CheckStdoutAndReportProgressAsync(TextReader stdoutStreamReader, IProgress<CScriptOutputLine> stdoutProgress = null) {
            string stdoutLine = await stdoutStreamReader.ReadToEndAsync();

            if (stdoutProgress != null && !string.IsNullOrEmpty(stdoutLine)) {
                stdoutProgress.Report(new CScriptOutputLine(stdoutLine, false));
            }
        }

        private static async Task CheckStderrAndReportProgressAsync(TextReader stderrStreamReader, IProgress<CScriptOutputLine> stderrProgress = null) {
            string stderrLine = await stderrStreamReader.ReadToEndAsync();

            if (stderrProgress != null && !string.IsNullOrEmpty(stderrLine)) {
                stderrProgress.Report(new CScriptOutputLine(stderrLine, true));
            }
        }

        public static async Task ExecuteAsync(this SshCommand sshCommand, CancellationToken cancellationToken, IProgress<CScriptOutputLine> progress = null, Encoding encoding = null) {
            IAsyncResult asyncResult = sshCommand.BeginExecute();

            var stdoutStreamReader = new StreamReader(sshCommand.OutputStream, encoding ?? Encoding.UTF8, encoding == null);
            var stderrStreamReader = new StreamReader(sshCommand.ExtendedOutputStream, encoding ?? Encoding.UTF8, encoding == null);

            while (!asyncResult.IsCompleted) {
                await Task.Yield();
                await CheckOutputAndReportProgress(sshCommand, stdoutStreamReader, stderrStreamReader, cancellationToken, progress);
            }

            await CheckOutputAndReportProgress(sshCommand, stdoutStreamReader, stderrStreamReader, cancellationToken, progress);

            _ = sshCommand.EndExecute(asyncResult);
        }

    }

}
