using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using WebApi.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using WebApi.Services;
using WebApi.Entities;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Autofac.Util;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using WebApi.Models.Messaging;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MailsController : ControllerBase, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        readonly Disposable _disposable;
        private IMailService _mailService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;
        private ILogger _log;
        private DataContext _context;

        public MailsController(
            IMailService mailService,
            IMapper mapper,
            ILogger<MailsController> log,
            IOptions<AppSettings> appSettings,
            DataContext context)
        {
            _mailService = mailService;
            _mapper = mapper;
            _log = log;
            _appSettings = appSettings.Value;
            _context = context;
            _disposable = new Disposable();
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
            }

            disposed = true;
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            return null;
        }

        // [AllowAnonymous]
        [HttpGet("folder/{paramFolderId}")]
        public IActionResult GetFolder(int paramFolderId)
        {
            HttpContext.Response.RegisterForDispose(_disposable);
            var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);
            var userIdParam = new SqlParameter("@UserID", userId);
            var folderIdParam = new SqlParameter("FolderID", paramFolderId);
            var results = _context.MailModel.FromSqlRaw(@"
                IF @FolderID = 0
                    select m.Id, su.StaffName as SendingStaffName, su.StaffEmail as SendingStaffEmail, ru.StaffName as ReceivingStaffName,
                    ru.StaffEmail as ReceivingStaffEmail, m.Subject, m.Message, m.SentTime, m.SentSuccessToSMTPServer, m.[Read], m.Starred, m.Important, m.HasAttachments, m.[Label], @FolderID as 'Folder'
                    --, m.Folder
                    from Mail m
                    left join Users su on m.SendingUserID = su.UserID
                    left join Users ru on m.ReceivingUserID = ru.UserID
                    where m.SendingUserID = @UserID
                    order by m.SentTime desc

                ELSE
                    select m.Id,
                    su.StaffName as SendingStaffName, su.StaffEmail as SendingStaffEmail, ru.StaffName as ReceivingStaffName, ru.StaffEmail as ReceivingStaffEmail,	m.Subject, m.Message, m.SentTime, m.SentSuccessToSMTPServer, m.[Read],	m.Starred, m.Important,	m.HasAttachments, m.[Label], @FolderID as 'Folder'
                    --, m.Folder
                    from Mail m
                    left join Users su on su.userID = m.SendingUserID
                    left join Users ru on ru.userID = m.ReceivingUserID
                    where m.ReceivingUserID = @UserID
                    order by m.SentTime desc", parameters: new[] { userIdParam, folderIdParam }).ToList();
            return Ok(results);
        }

        //[AllowAnonymous]
        [HttpGet("label/{paramLabelId}")]
        public IActionResult GetLabel(int paramLabelId)
        {
            HttpContext.Response.RegisterForDispose(_disposable);
            var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);
            //Need to Improve in App side params
            MailsPageObj MailsPageObj = _mailService.GetMails(userId, 0, 1, 10,paramLabelId);
            return Ok(MailsPageObj);
        }

        [HttpPost("m/{paramMailId}/resend")]
        public IActionResult ResendMail(String paramMailId)
        {
            HttpContext.Response.RegisterForDispose(_disposable);
            var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);

            // Begin transaction
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                Guid id = new Guid(paramMailId);
                List<MailAttachment> attachments = new List<MailAttachment>();

                var mailFound = _context.Mail.Include(g => g.SendingUser).Include(g => g.ReceivingUser).Where(g => g.Id == id && g.SendingUserID == userId).First();
                if (mailFound != null)
                {
                    mailFound._appSettings = _appSettings;
                    mailFound._log = _log;

                    if (mailFound.HasAttachments)
                    {
                        attachments = _context.MailAttachments.Where(x => x.MailID == mailFound.Id).ToList();
                    }

                    Mail mailToSend = _mailService.CreateResendMail(mailFound, attachments);

                    int mailStatus = mailToSend.send();

                    if (mailStatus == 0 || mailStatus == -1)
                    {
                        transaction.Rollback();
                        return BadRequest(new { message = "Failed to resend email." });
                    }

                    mailToSend.SentSuccessToSMTPServer = true;
                    _context.SaveChanges();
                    transaction.Commit();
                    return Ok();
                }
                else
                {
                    transaction.Rollback();
                    return BadRequest(new { message = "You are not authorised to resend the email." });
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("folder/{paramFolderId}/{pageNumber}/{rowsOfPage}")]
        public IActionResult GetMailsByFolder(int paramFolderId, int pageNumber, int rowsOfPage)
        {
            try
            {

                HttpContext.Response.RegisterForDispose(_disposable);
                int userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);
                MailsPageObj mailsPageObj = _mailService.GetMails(userId, paramFolderId, pageNumber, rowsOfPage);
                return Ok(mailsPageObj);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }
    }
}
