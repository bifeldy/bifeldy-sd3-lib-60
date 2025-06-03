/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Transaksi Database Untuk Surat Elektronik
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System.Net;
using System.Net.Mail;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using bifeldy_sd3_lib_60.Abstractions;
using bifeldy_sd3_lib_60.AttributeFilterDecorators;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.TableView;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IListMailServerRepository {
        Task<bool> Create(bool isPg, IDatabase db, DC_LISTMAILSERVER_T apiKey);
        Task<List<DC_LISTMAILSERVER_T>> GetAll(bool isPg, IDatabase db, string dckode = null);
        Task<DC_LISTMAILSERVER_T> GetByDcKode(bool isPg, IDatabase db, string dckode);
        Task<bool> Delete(bool isPg, IDatabase db, string dckode);
        MailAddress CreateEmailAddress(string address, string displayName = null, Encoding encoding = null);
        List<MailAddress> CreateEmailAddress(string[] address);
        Attachment CreateEmailAttachment(string filePath);
        List<Attachment> CreateEmailAttachment(string[] filePath);
        MailMessage CreateEmailMessage(string subject, string body, List<MailAddress> to, List<MailAddress> cc = null, List<MailAddress> bcc = null, List<Attachment> attachments = null, MailAddress from = null, Encoding encoding = null);
        Task SendEmailMessage(bool isPg, IDatabase db, MailMessage mailMessage, bool paksaDariHo = false);
        MailAddress GetDefaultBotSenderFromAddress();
        Task CreateAndSend(bool isPg, IDatabase db, string subject, string body, List<MailAddress> to, List<MailAddress> cc = null, List<MailAddress> bcc = null, List<Attachment> attachments = null, MailAddress from = null);
    }

    [ScopedServiceRegistration]
    public sealed class CListMailServerRepository : CRepository, IListMailServerRepository {

        private readonly EnvVar _envVar;
        private readonly ILogger<CListMailServerRepository> _logger;

        private readonly IApplicationService _as;

        public CListMailServerRepository(
            IOptions<EnvVar> envVar,
            ILogger<CListMailServerRepository> logger,
            IApplicationService @as
        ) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._as = @as;
        }

        public async Task<bool> Create(bool isPg, IDatabase db, DC_LISTMAILSERVER_T apiKey) {
            _ = db.Set<DC_LISTMAILSERVER_T>().Add(apiKey);
            return await db.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_LISTMAILSERVER_T>> GetAll(bool isPg, IDatabase db, string dckode = null) {
            DbSet<DC_LISTMAILSERVER_T> dbSet = db.Set<DC_LISTMAILSERVER_T>();
            IQueryable<DC_LISTMAILSERVER_T> query = null;
            if (!string.IsNullOrEmpty(dckode)) {
                _ = dbSet.Where(ms => ms.MAIL_DCKODE.ToUpper() == dckode.ToUpper());
            }

            return await (query ?? dbSet).ToListAsync();
        }

        public async Task<DC_LISTMAILSERVER_T> GetByDcKode(bool isPg, IDatabase db, string dckode) {
            return await db.Set<DC_LISTMAILSERVER_T>()
                .Where(ms => ms.MAIL_DCKODE.ToUpper() == dckode.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(bool isPg, IDatabase db, string dckode) {
            DC_LISTMAILSERVER_T apiKey = await this.GetByDcKode(isPg, db, dckode);
            _ = db.Set<DC_LISTMAILSERVER_T>().Remove(apiKey);
            return await db.SaveChangesAsync() > 0;
        }

        /* ** */

        private async Task<SmtpClient> CreateSmtpClient(bool isPg, IDatabase db, bool paksaDariHo = false) {
            string host = null;
            int port = 0;
            string uname = null;
            string upass = null;

            if (paksaDariHo) {
                host = this._envVar.SMTP_SERVER_IP_DOMAIN;
                port = this._envVar.SMTP_SERVER_PORT;
                uname = this._envVar.SMTP_SERVER_USERNAME;
                upass = this._envVar.SMTP_SERVER_PASSWORD;
            }
            else {
                string dcKode = await this.GetKodeDc(isPg, db);
                DC_LISTMAILSERVER_T mailServer = await this.GetByDcKode(isPg, db, dcKode);

                if (mailServer == null) {
                    return await this.CreateSmtpClient(isPg, db, true);
                }

                host = mailServer.MAIL_HOSTNAME;
                string _port = mailServer.MAIL_PORT;
                port = string.IsNullOrEmpty(_port) ? 0 : int.Parse(_port);
                uname = mailServer.MAIL_USERNAME;
                upass = mailServer.MAIL_PASSWORD;
            }

            if (string.IsNullOrEmpty(host) || port <= 0 || string.IsNullOrEmpty(uname) || string.IsNullOrEmpty(upass)) {
                throw new Exception("Gagal Mengatur Informasi SMTP!");
            }

            return new SmtpClient() {
                Host = host,
                Port = port,
                Credentials = new NetworkCredential(uname, upass)
            };
        }

        public MailAddress CreateEmailAddress(string address, string displayName = null, Encoding encoding = null) {
            return string.IsNullOrEmpty(displayName) ? new MailAddress(address) : new MailAddress(address, displayName, encoding ?? Encoding.Default);
        }

        public List<MailAddress> CreateEmailAddress(string[] address) {
            var addresses = new List<MailAddress>();
            foreach (string a in address) {
                addresses.Add(this.CreateEmailAddress(a));
            }

            return addresses;
        }

        public Attachment CreateEmailAttachment(string filePath) => new(filePath);

        public List<Attachment> CreateEmailAttachment(string[] filePath) {
            var attachments = new List<Attachment>();
            foreach (string path in filePath) {
                attachments.Add(this.CreateEmailAttachment(path));
            }

            return attachments;
        }

        public MailAddress GetDefaultBotSenderFromAddress() {
            return this.CreateEmailAddress("sd3@indomaret.co.id", $"[SD3_BOT] 📧 {this._as.AppName} v{this._as.AppVersion}");
        }

        public MailMessage CreateEmailMessage(
            string subject,
            string body,
            List<MailAddress> to,
            List<MailAddress> cc = null,
            List<MailAddress> bcc = null,
            List<Attachment> attachments = null,
            MailAddress from = null,
            Encoding encoding = null
        ) {
            encoding ??= Encoding.Default;

            var mailMessage = new MailMessage() {
                Subject = subject,
                SubjectEncoding = encoding,
                Body = body,
                BodyEncoding = encoding,
                From = from ?? this.GetDefaultBotSenderFromAddress(),
                IsBodyHtml = true
            };
            foreach (MailAddress t in to) {
                mailMessage.To.Add(t);
            }

            if (cc != null) {
                foreach (MailAddress c in cc) {
                    mailMessage.CC.Add(c);
                }
            }

            if (bcc != null) {
                foreach (MailAddress b in bcc) {
                    mailMessage.Bcc.Add(b);
                }
            }

            if (attachments != null) {
                foreach (Attachment a in attachments) {
                    mailMessage.Attachments.Add(a);
                }
            }

            return mailMessage;
        }

        public async Task SendEmailMessage(bool isPg, IDatabase db, MailMessage mailMessage, bool paksaDariHo = false) {
            SmtpClient smtpClient = await this.CreateSmtpClient(isPg, db, paksaDariHo);
            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task CreateAndSend(
            bool isPg, IDatabase db,
            string subject,
            string body,
            List<MailAddress> to,
            List<MailAddress> cc = null,
            List<MailAddress> bcc = null,
            List<Attachment> attachments = null,
            MailAddress from = null
        ) {
            Exception e = null;
            try {
                MailMessage mail = this.CreateEmailMessage(
                    subject, body, to, cc, bcc, attachments,
                    from ?? this.GetDefaultBotSenderFromAddress()
                );
                try {
                    // Pakai Regional
                    await this.SendEmailMessage(isPg, db, mail);
                }
                catch {
                    // Via DCHO
                    await this.SendEmailMessage(isPg, db, mail, true);
                }
            }
            catch (Exception ex) {
                this._logger.LogError("[SUREL_CREATE_AND_SEND] {ex}", ex.Message);
                e = ex;
            }

            if (e != null) {
                throw e;
            }
        }

    }

}
