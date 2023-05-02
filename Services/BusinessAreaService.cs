using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace WebApi.Services
{
    public interface IBusinessAreaService
    {
        IEnumerable<BusinessArea> GetAll();
    }

    public class BusinessAreaService : IBusinessAreaService, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        private DataContext _context;

        public BusinessAreaService(DataContext context)
        {
            _context = context;
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

        private void UpdateToDB(IEnumerable<BusinessArea> businessAreas)
        {
            try
            {
                _context.BusinessAreas.UpdateRange(businessAreas);
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


        private void AddtoDB(BusinessArea businessArea)
        {
            try
            {
                _context.BusinessAreas.Add(businessArea);
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

        private void CommittoDB(IEnumerable<BusinessArea> businessAreas)
        {
            try
            {
                _context.BusinessAreas.AddRange(businessAreas);
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

        public IEnumerable<BusinessArea> GetAll()
        {
            return _context.Set<BusinessArea>().AsNoTracking().OrderBy(c => c.BusinessAreaID);
        }
    }
}