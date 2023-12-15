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
        Task<List<DC_LISTMAILSERVER_T>> GetAll();
        Task<DC_LISTMAILSERVER_T> GetByDcKode(string dckode);
        Task<bool> Update(string dckode, DC_LISTMAILSERVER_T newListMailServer);
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

        private readonly IGlobalService _gs;

        private readonly IOraPg _orapg;

        public CListMailServerRepository(
            IOptions<EnvVar> envVar,
            ILogger<CListMailServerRepository> logger,
            IApplicationService @as,
            IGlobalService gs,
            IOraPg orapg
        ) : base(envVar, @as, orapg) {
            _envVar = envVar.Value;
            _logger = logger;
            _gs = gs;
            _orapg = orapg;
        }

        public async Task<bool> Create(DC_LISTMAILSERVER_T apiKey) {
            _orapg.Set<DC_LISTMAILSERVER_T>().Add(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<List<DC_LISTMAILSERVER_T>> GetAll() {
            return await _orapg.Set<DC_LISTMAILSERVER_T>().ToListAsync();
        }

        public async Task<DC_LISTMAILSERVER_T> GetByDcKode(string dckode) {
            return await _orapg.Set<DC_LISTMAILSERVER_T>().Where(m => m.MAIL_DCKODE == dckode).FirstOrDefaultAsync();
        }

        public async Task<bool> Update(string dckode, DC_LISTMAILSERVER_T newListMailServer) {
            DC_LISTMAILSERVER_T oldListMailServer = await _orapg.Set<DC_LISTMAILSERVER_T>().Where(m => m.MAIL_DCKODE == dckode).FirstOrDefaultAsync();
            oldListMailServer.MAIL_DCNAME = newListMailServer.MAIL_DCNAME;
            oldListMailServer.MAIL_IP = newListMailServer.MAIL_IP;
            oldListMailServer.MAIL_HOSTNAME = newListMailServer.MAIL_HOSTNAME;
            oldListMailServer.MAIL_PORT = newListMailServer.MAIL_PORT;
            oldListMailServer.MAIL_USERNAME = newListMailServer.MAIL_USERNAME;
            oldListMailServer.MAIL_PASSWORD = newListMailServer.MAIL_PASSWORD;
            oldListMailServer.MAIL_SENDER = newListMailServer.MAIL_SENDER;
            return await _orapg.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string dckode) {
            DC_LISTMAILSERVER_T apiKey = await _orapg.Set<DC_LISTMAILSERVER_T>().Where(m => m.MAIL_DCKODE == dckode).FirstOrDefaultAsync();
            _orapg.Set<DC_LISTMAILSERVER_T>().Remove(apiKey);
            return await _orapg.SaveChangesAsync() > 0;
        }

        /* ** */

        private async Task<SmtpClient> CreateSmtpClient() {
            string dcKode = await GetKodeDc();
            DC_LISTMAILSERVER_T mailServer = await GetByDcKode(dcKode);
            int port = int.Parse(mailServer.MAIL_PORT);
            return new SmtpClient() {
                Host = mailServer.MAIL_HOSTNAME ?? _envVar.SMTP_SERVER_IP_DOMAIN,
                Port = (port > 0) ? port : _envVar.SMTP_SERVER_PORT,
                Credentials = new NetworkCredential(
                    mailServer.MAIL_USERNAME ?? _envVar.SMTP_SERVER_USERNAME,
                    mailServer.MAIL_PASSWORD ?? _envVar.SMTP_SERVER_PASSWORD
                )
            };
        }

        public MailAddress CreateEmailAddress(string address, string displayName = null) {
            if (string.IsNullOrEmpty(displayName)) {
                return new MailAddress(address);
            }
            return new MailAddress(address, displayName, Encoding.UTF8);
        }

        public List<MailAddress> CreateEmailAddress(string[] address) {
            List<MailAddress> addresses = new List<MailAddress>();
            foreach (string a in address) {
                addresses.Add(CreateEmailAddress(a));
            }
            return addresses;
        }

        public Attachment CreateEmailAttachment(string filePath) {
            return new Attachment(filePath);
        }

        public List<Attachment> CreateEmailAttachment(string[] filePath) {
            List<Attachment> attachments = new List<Attachment>();
            foreach (string path in filePath) {
                attachments.Add(CreateEmailAttachment(path));
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
            MailMessage mailMessage = new MailMessage() {
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
            SmtpClient smtpClient = await CreateSmtpClient();
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
                await SendEmailMessage(
                    CreateEmailMessage(
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
                _logger.LogError($"[SUREL_CREATE_AND_SEND] {ex.Message}");
                e = ex;
            }
            if (e != null) {
                throw e;
            }
        }

    }

}
