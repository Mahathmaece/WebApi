using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using WebApi.Helpers;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebApi.Services;
using WebApi.Entities;
using WebApi.Models.Users;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Autofac.Util;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ModulePagesController : ControllerBase, IDisposable
    {
        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        readonly Disposable _disposable;
        private IModulePageService _modulePageService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;
        private DataContext _context;

        public ModulePagesController(
            IModulePageService modulePageService,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            DataContext context)
        {
            _modulePageService = modulePageService;
            _mapper = mapper;
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

        //[AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);
            // var costCenters = _costCenterService.GetAll();
            // var model = _mapper.Map<IList<CostCenter>>(costCenters);
            // return Ok(model);
            var results = _context.ModulePages.FromSqlRaw("SELECT * FROM ModulePages").AsNoTracking().ToList();
            return Ok(results);
        }

        //[AllowAnonymous]
        [HttpGet("GetModulePagesByUserId/{userid}")]
        public IActionResult GetModulePagesByUserId(int userid)
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);
            var result = _modulePageService.GetModulePagesByUserId(userid);

            return Ok(result);
        }
    }
}