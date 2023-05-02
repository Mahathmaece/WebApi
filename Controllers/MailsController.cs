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
            var userIdParam = new SqlParameter("@UserID", userId);
            var labelIdParam = new SqlParameter("LabelID", paramLabelId);
            var results = _context.MailModel.FromSqlRaw(@"
                select m.Id, su.StaffName as SendingStaffName, su.StaffEmail as SendingStaffEmail, ru.StaffName as ReceivingStaffName, ru.StaffEmail as ReceivingStaffEmail, m.Subject, m.Message, m.SentTime, m.SentSuccessToSMTPServer, m.[Read], m.Starred, m.Important, m.HasAttachments, m.[Label], m.Folder 
                from Mail m
                left join Users su on su.UserID = m.SendingUserID
                left join Users ru on ru.UserID = m.ReceivingUserID
                where (m.SendingUserID = @UserID OR m.ReceivingUserID = @UserID)
                and m.Label = @LabelID
                order by m.SentTime desc", parameters: new[] { userIdParam, labelIdParam }).ToList();
            return Ok(results);
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
                MailsPageObj mailsPageObj = _mailService.GetMailsByFolder(userId, paramFolderId, pageNumber, rowsOfPage);


                //string sql = $"SELECT M.[Id] ,SU.[StaffName] AS SendingStaffName ,SU.[StaffEmail] AS SendingStaffEmail	,RU.[StaffName] AS ReceivingStaffName ,RU.[StaffEmail] AS ReceivingStaffEmail ,M.[Subject] ,M.[Message] ,M.[SentTime] ,M.[Read] ,M.[Starred] ,M.[Important] ,M.[HasAttachments] ,M.[Label] ,M.[Folder] ,M.[OriginMailID] ,M.[ErrorMessage] ,M.[SentSuccessToSMTPServer] FROM [test_db].[dbo].[Mail] AS M LEFT JOIN [Users] SU on M.SendingUserID = SU.UserID LEFT JOIN [Users] RU on M.ReceivingUserID = RU.UserID WHERE M.[Folder] = {paramFolderId} AND M.[SendingUserID]= {userId} ORDER BY [SentTime] DESC OFFSET {(pageNumber - 1) * rowsOfPage} ROWS FETCH NEXT {rowsOfPage} ROWS ONLY";
                //string countSql = $"SELECT M.[Id] FROM [test_db].[dbo].[Mail] AS M LEFT JOIN [Users] SU on M.SendingUserID = SU.UserID LEFT JOIN [Users] RU on M.ReceivingUserID = RU.UserID WHERE M.[Folder] = {paramFolderId} AND M.[SendingUserID]= {userId}";
                //var results = _context.MailModel.FromSqlRaw(sql).ToList();
                //var count = _context.MailModel.FromSqlRaw(countSql).Count();

                //MailsPageObj mailsPageObj = new MailsPageObj();
                //mailsPageObj.results = results;
                //mailsPageObj.pageNumber = pageNumber;
                //mailsPageObj.totalRows = count;
                //mailsPageObj.rowsOfPage = rowsOfPage;
                return Ok(mailsPageObj);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }
    }
}
