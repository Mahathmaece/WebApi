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
using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;
using Autofac.Util;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserRolesController : ControllerBase, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        readonly Disposable _disposable;
        private IUserRoleService _userRoleService;
        private IUserService _userService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;
        private DataContext _context;

        public UserRolesController(
            IUserRoleService userRoleService,
            IUserService userService,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            DataContext context)
        {
            _userRoleService = userRoleService;
            _userService = userService;
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
            var results = _context.UserRoles.FromSqlRaw("SELECT * FROM UserRoles").AsNoTracking().ToList();
            return Ok(results);
        }

        //[AllowAnonymous]
        [HttpGet("currentuserroles{UserID}")]
        public IActionResult GetCurrent(int UserID)
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);

            // var results = _context.UserRoles.FromSqlRaw("SELECT * FROM UserRoles WHERE UserID = @UserID", userID).AsNoTracking().ToList();
            var results = _userService.GetUserRolesObj(UserID);
            return Ok(results);
        }
    }
}
