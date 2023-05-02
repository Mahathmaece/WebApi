using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Helpers;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using WebApi.Models.Messaging;
using System.Linq;

namespace WebApi.Services
{
    public enum MailLabels : int
    {
        ASSET_LOAN = 0,
        ASSET_VERIFICATION = 1,
        ASSET_SERVICING = 2,
        ASSET_LOST_DAMAGED = 3,
        ASSET_DONATED = 4,
        ASSET_TRANSFER = 5,
        OTHERS = 6
    };

    public interface IMailService
    {
        Mail CreateResendMail(Mail originMail, List<MailAttachment> originMailAttachments);
        MailsPageObj GetMailsByFolder(int userId, int paramFolderId, int pageNumber, int rowsOfPage);
    }

    public class MailService : IMailService, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        private DataContext _context;
        private readonly AppSettings _appSettings;
        private ILogger _log;

        public MailService(DataContext context, IOptions<AppSettings> appSettings, ILogger<MailService> log)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _log = log;
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
                _context.Dispose();
            }

            disposed = true;
        }

        public Mail CreateResendMail(Mail originMail, List<MailAttachment> originMailAttachments)
        {
            Mail newMail = new Mail();
            List<MailAttachment> newMailAttachments = new List<MailAttachment>();

            newMail._appSettings = originMail._appSettings;
            newMail._log = originMail._log;

            newMail.OriginMailID = originMail.Id;
            newMail.ReceivingUser = originMail.ReceivingUser;
            newMail.SendingUser = originMail.SendingUser;

            newMail.Label = originMail.Label;
            newMail.SentTime = DateTime.Now;
            newMail.SendingUserID = originMail.SendingUserID;
            newMail.ReceivingUserID = originMail.ReceivingUserID;
            newMail.Subject = originMail.Subject;
            newMail.Message = originMail.Message;
            newMail.HasAttachments = originMail.HasAttachments;


            // using var transaction = _context.Database.BeginTransaction();
            // try
            // { 
            AddtoDB(newMail);
            CommittoDB(newMail);

            if (newMail.HasAttachments)
            {
                foreach (var a in originMailAttachments)
                {
                    string filePath = a.SavedPath;
                    string fileName = a.Filename;
                    Attachment attachment = new Attachment(filePath);
                    attachment.Name = fileName;
                    newMail.attachments.Add(attachment);

                    MailAttachment mailAttachment = new MailAttachment();
                    mailAttachment.MailID = newMail.Id;
                    mailAttachment.Filename = fileName;
                    mailAttachment.SavedPath = filePath;
                    newMailAttachments.Add(mailAttachment);
                }
            }

            CommittoDB(newMailAttachments);
                // transaction.Commit();
            // }
            // catch (Exception ex)
            // {
            //     transaction.Rollback();
            // }

            return newMail;
        }

        private void AddtoDB(Mail newMail, DataContext _context)
        {
            try
            {
                _context.Mail.Add(newMail);
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        private void AddtoDB(Mail newMail)
        {
            try
            {
                _context.Mail.Add(newMail);
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        private void CommittoDB(IEnumerable<MailAttachment> newMailAttachments)
        {
            try
            {
                _context.MailAttachments.AddRange(newMailAttachments);
                _context.SaveChanges();
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        private void AddtoDB(MailAttachment newMailAttachment)
        {
            try
            {
                _context.MailAttachments.Add(newMailAttachment);
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        private void CommittoDB(Object obj)
        {
            try
            {
                _context.Entry(obj).State = EntityState.Added;
                _context.SaveChanges();
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        // Zack DB-Refactor
        private void DbTracking(object obj, DataContext _context)
        {
            try 
            {
                _context.Entry(obj).State = EntityState.Added;
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        //Get Email by folder
        public MailsPageObj GetMailsByFolder(int userId, int paramFolderId, int pageNumber, int rowsOfPage)
        {           

            string sql = $"SELECT M.[Id] ,SU.[StaffName] AS SendingStaffName ,SU.[StaffEmail] AS SendingStaffEmail	,RU.[StaffName] AS ReceivingStaffName ,RU.[StaffEmail] AS ReceivingStaffEmail ,M.[Subject] ,M.[Message] ,M.[SentTime] ,M.[Read] ,M.[Starred] ,M.[Important] ,M.[HasAttachments] ,M.[Label] ,M.[Folder] ,M.[OriginMailID] ,M.[ErrorMessage] ,M.[SentSuccessToSMTPServer] FROM [test_db].[dbo].[Mail] AS M LEFT JOIN [Users] SU on M.SendingUserID = SU.UserID LEFT JOIN [Users] RU on M.ReceivingUserID = RU.UserID WHERE M.[Folder] = {paramFolderId} AND M.[SendingUserID]= {userId} ORDER BY [SentTime] DESC OFFSET {(pageNumber - 1) * rowsOfPage} ROWS FETCH NEXT {rowsOfPage} ROWS ONLY";
            string countSql = $"SELECT M.[Id] FROM [test_db].[dbo].[Mail] AS M LEFT JOIN [Users] SU on M.SendingUserID = SU.UserID LEFT JOIN [Users] RU on M.ReceivingUserID = RU.UserID WHERE M.[Folder] = {paramFolderId} AND M.[SendingUserID]= {userId}";
            var results = _context.MailModel.FromSqlRaw(sql).ToList();
            var count = _context.MailModel.FromSqlRaw(countSql).Count();

            MailsPageObj mailsPageObj = new MailsPageObj();
            mailsPageObj.results = results;
            mailsPageObj.pageNumber = pageNumber;
            mailsPageObj.totalRows = count;
            mailsPageObj.rowsOfPage = rowsOfPage;

            return mailsPageObj;
        }
    }
}