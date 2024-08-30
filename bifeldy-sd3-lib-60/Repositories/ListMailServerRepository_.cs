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
using bifeldy_sd3_lib_60.Databases;
using bifeldy_sd3_lib_60.Models;
using bifeldy_sd3_lib_60.Services;
using bifeldy_sd3_lib_60.Tables;

namespace bifeldy_sd3_lib_60.Repositories {

    public interface IListMailServerRepository {
        Task<bool> Create(DC_LISTMAILSERVER_T apiKey);
        Task<List<DC_LISTMAILSERVER_T>> GetAll(string dckode = null);
        Task<DC_LISTMAILSERVER_T> GetByDcKode(string dckode);
        Task<bool> Delete(string dckode);
        MailAddress CreateEmailAddress(string address, string displayName = null);
        List<MailAddress> CreateEmailAddress(string[] address);
        Attachment CreateEmailAttachment(string filePath);
        List<Attachment> CreateEmailAttachment(string[] filePath);
        MailMessage CreateEmailMessage(string subject, string body, MailAddress from, List<MailAddress> to, List<MailAddress> cc = null, List<MailAddress> bcc = null, List<Attachment> attachments = null);
        Task SendEmailMessage(MailMessage mailMessage);
        Task CreateAndSend(string subject, string body, MailAddress from, List<MailAddress> to, List<MailAddress> cc = null, List<MailAddress> bcc = null, List<Attachment> attachments = null);
    }

    public sealed class CListMailServerRepository : CRepository, IListMailServerRepository {

        private readonly EnvVar _envVar;
        private readonly ILogger<CListMailServerRepository> _logger;

        private readonly IOraPg _orapg;

        public CListMailServerRepository(
            IOptions<EnvVar> envVar,
            ILogger<CListMailServerRepository> logger,
            IApplicationService @as,
            IOraPg orapg,
            IMsSQL mssql
        ) : base(envVar, @as, orapg, mssql) {
            this._envVar = envVar.Value;
            this._logger = logger;
            this._orapg = orapg;
        }

        public async Task<bool> Create(DC_LISTMAILSERVER_T apiKey) {
            _ = this._orapg.Set<DC_LISTMAILSERVER_T>().Add(apiKey);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_LISTMAILSERVER_T>> GetAll(string dckode = null) {
            DbSet<DC_LISTMAILSERVER_T> dbSet = this._orapg.Set<DC_LISTMAILSERVER_T>();
            IQueryable<DC_LISTMAILSERVER_T> query = null;
            if (!string.IsNullOrEmpty(dckode)) {
                _ = dbSet.Where(ms => ms.MAIL_DCKODE.ToUpper() == dckode.ToUpper());
            }

            return await (query ?? dbSet).ToListAsync();
        }

        public async Task<DC_LISTMAILSERVER_T> GetByDcKode(string dckode) {
            return await this._orapg.Set<DC_LISTMAILSERVER_T>()
                .Where(ms => ms.MAIL_DCKODE.ToUpper() == dckode.ToUpper())
                .SingleOrDefaultAsync();
        }

        public async Task<bool> Delete(string dckode) {
            DC_LISTMAILSERVER_T apiKey = await this.GetByDcKode(dckode);
            _ = this._orapg.Set<DC_LISTMAILSERVER_T>().Remove(apiKey);
            return await this._orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        private async Task<SmtpClient> CreateSmtpClient() {
            string dcKode = await this.GetKodeDc();
            DC_LISTMAILSERVER_T mailServer = await this.GetByDcKode(dcKode);
            int port = int.Parse(mailServer.MAIL_PORT);
            return new SmtpClient() {
                Host = mailServer.MAIL_HOSTNAME ?? this._envVar.SMTP_SERVER_IP_DOMAIN,
                Port = (port > 0) ? port : this._envVar.SMTP_SERVER_PORT,
                Credentials = new NetworkCredential(
                    mailServer.MAIL_USERNAME ?? this._envVar.SMTP_SERVER_USERNAME,
                    mailServer.MAIL_PASSWORD ?? this._envVar.SMTP_SERVER_PASSWORD
                )
            };
        }

        public MailAddress CreateEmailAddress(string address, string displayName = null) {
            return string.IsNullOrEmpty(displayName) ? new MailAddress(address) : new MailAddress(address, displayName, Encoding.UTF8);
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

        public MailMessage CreateEmailMessage(
            string subject,
            string body,
            MailAddress from,
            List<MailAddress> to,
            List<MailAddress> cc = null,
            List<MailAddress> bcc = null,
            List<Attachment> attachments = null
        ) {
            var mailMessage = new MailMessage() {
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                From = from,
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

        public async Task SendEmailMessage(MailMessage mailMessage) {
            SmtpClient smtpClient = await this.CreateSmtpClient();
            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task CreateAndSend(
            string subject,
            string body,
            MailAddress from,
            List<MailAddress> to,
            List<MailAddress> cc = null,
            List<MailAddress> bcc = null,
            List<Attachment> attachments = null
        ) {
            Exception e = null;
            try {
                await this.SendEmailMessage(
                    this.CreateEmailMessage(
                        subject,
                        body,
                        from,
                        to,
                        cc,
                        bcc,
                        attachments
                    )
                );
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
