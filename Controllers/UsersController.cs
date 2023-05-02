using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using WebApi.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
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
using BCryptNet = BCrypt.Net.BCrypt;

namespace WebApi.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        readonly Disposable _disposable;
        private IUserService _userService;
        private IMailService _mailService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;
        private DataContext _context;

        private const string LOGIN_SOURCE_WEB = "Web";

        private const string TOGGLECOLS_REPORTS_ASSETDETAILS = "Reports_AssetDetails";

        public UsersController(
            IUserService userService,
            IMailService mailService,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            DataContext context)
        {
            _userService = userService;
            _mailService = mailService;
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

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] AuthenticateModel model)
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);
            var user = new UserModel();
            bool isLDAPLogin = false;
            DateTime now = DateTime.Now;
            Guid sessionID;

            model.LoginName = Helpers.SecurityFunction.DecryptAES1(model.LoginName, "AAECAwQFBgcICQoLDA0ODw==");
            model.Password = Helpers.SecurityFunction.DecryptAES1(model.Password, "AAECAwQFBgcICQoLDA0ODw==");

            // Check if the LoginName is an email, if email, call the AMS authenticate service
            // If not email call the LDAP authenticate service
            if (model.LoginName.Contains('@'))
            {
                isLDAPLogin = false;
                user = _userService.Authenticate(model.LoginName, model.Password);
            }
            else
            {
                isLDAPLogin = true;
            }

            if (user == null)
            {
                return BadRequest(new { message = "Login Name or password is incorrect." });
            }
            else if (user.IsWrongPassword != null && (bool)user.IsWrongPassword)
            {
                _userService.UpdateLastLogin(user.UserID, false, now);
                return BadRequest(new { message = "Login Name or password is incorrect." });
            }
            else if (user.IsLock)
            {
                _userService.UpdateLastLogin(user.UserID, false, now);
                return Content("User is Locked, please get an administrator to unlock it.");
            }
            else if (user.IsPasswordExpired != null && (bool)user.IsPasswordExpired)
            {
                _userService.UpdateLastLogin(user.UserID, false, now);
                return Content("Password expired.");
            }
            else
            {
                // successful login, create session
                sessionID = _userService.CreateUserSession(user.UserID, isLDAPLogin, LOGIN_SOURCE_WEB, "", now);
                _userService.UpdateLastLogin(user.UserID, true, now);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserID.ToString()),
                    new Claim(CustomClaimTypes.AMSSessionID, sessionID.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // get saved toggle columns for various modules (for now used by Reports module only)
            IEnumerable<ToggleColsModel> allToggleCols = _userService.GetUserSavedToggleColumns(user.UserID);

            List<string> Reports_AssetDetailsSavedCols = new List<string>();

            if (allToggleCols != null)
            {
                foreach (ToggleColsModel col in allToggleCols)
                {
                    if (col.WebPage == TOGGLECOLS_REPORTS_ASSETDETAILS)
                    {
                        Reports_AssetDetailsSavedCols.Add(col.SelectedColumn);
                    }
                }
            }

            // if there is no saved toggle columns, use default setup
            if (Reports_AssetDetailsSavedCols.Count == 0)
            {
                Reports_AssetDetailsSavedCols = (List<string>)_userService.GetDefaultToggleColumns(TOGGLECOLS_REPORTS_ASSETDETAILS);
            }

            // get saved filters for various modules (for now used by Reports module only)
            IEnumerable<FiltersModel> allFilters = _userService.GetUserSavedFilters(user.UserID);

            string Reports_AssetDetailsSavedFilters = "";

            if (allFilters != null)
            {
                foreach (FiltersModel filter in allFilters)
                {
                    if (filter.WebPage == TOGGLECOLS_REPORTS_ASSETDETAILS)
                    {
                        Reports_AssetDetailsSavedFilters = filter.AllFilters;
                    }
                }
            }

            // return basic user info and authentication token
            return Ok(new
            {
                userID = user.UserID,
                loginName = user.LoginName,
                staffName = user.StaffName,
                staffEmail = user.StaffEmail,
                officeContactNo = user.OfficeContactNo,
                company = user.Company,
                companyCode = user.CompanyCode,
                companyID = user.CompanyID,
                costCenter = user.CostCenter,
                costCenterCode = user.CostCenterCode,
                costCenterID = user.CostCenterID,
                employeeNumber = user.EmployeeNumber,
                UserRoles = user.UserRolesID,
                UserRights = user.UserRightsID,
                Reports_AssetDetailsSavedCols = Reports_AssetDetailsSavedCols,

                Reports_AssetDetailsSavedFilters = Reports_AssetDetailsSavedFilters,

                Token = tokenString
            });
        }



        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Response.RegisterForDispose(_disposable);
            var creatorID = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);
            var sessionID = UserService.GetSessionIDFromToken(Request.Headers["Authorization"], _appSettings.Secret);
            try
            {
                _userService.Logout(creatorID, sessionID);
            }
            catch (Exception ex)
            {

            }

            // don't need to inform frontend if there are any errors
            return Ok();
        }

        //[AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);
            // var users = _userService.GetAll();
            // var model = _mapper.Map<IList<UserModel>>(users);
            // return Ok(model);
            var results = _context.UserModels.FromSqlRaw("Web_GetAllUsers").AsNoTracking().ToList();
            return Ok(results);
        }

        //[AllowAnonymous]
        [HttpGet("getall{UserID}")]
        public IActionResult GetAll(int UserID)
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);

            // check user rights
            //=> users with View Own RI Tasks can view all tasks within RI of the user
            //=> users with View Own Tasks can view tasks they are involved in (createdby, director, etc)
            //=> The rest see nothing
            var user = _context.Users.Find(UserID);
            if (user == null)
                return Ok(new List<UserModel>());

            bool isAdmin = (from rolesjoin in _context.UserJoinUserRoles
                            join roles in _context.UserRoles on rolesjoin.UserRoleID equals roles.UserRoleID
                            join rightsjoin in _context.UserRoleJoinUserRights on roles.UserRoleID equals rightsjoin.UserRoleID
                            join rights in _context.UserRights on rightsjoin.UserRightID equals rights.UserRightID
                            where rolesjoin.IsLock == false && rolesjoin.UserID == UserID && rights.PageModule == "User Management"
                            && rights.UserRightDescription == "Access Page and Search Users"
                            select rights.UserRightDescription).Count() > 0;
            if (isAdmin)
            {
                var results = _context.UserModels.FromSqlRaw("Web_GetAllUsers").AsNoTracking().ToList();
                return Ok(results);
            }
            else
            {
                return Ok(new List<UserModel>());
            }
        }

        //[AllowAnonymous]
        [HttpGet("{UserID}")]
        public IActionResult GetById(int UserID)
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);
            var userIdSqlParam = new SqlParameter("@UserID", UserID);
            var result = _context.UserModels.FromSqlRaw(@"SELECT 
                u.UserID
                ,u.StaffName
                ,u.StaffEmail
                ,u.LoginName
                ,u.EmployeeNumber
                ,cc.CostCenterID
                ,cc.CostCenterDescription 'CostCenter'
                ,cc.CostCenterCode
                ,c.CompanyID
                ,c.CompanyDescription 'Company'
                ,c.CompanyCode
                ,u.IsLock
                ,u.AccessRole

                FROM Users u
                LEFT JOIN CostCenters cc ON u.CostCenterID = cc.CostCenterID
                LEFT JOIN Companies c ON u.CompanyID = c.CompanyID
                WHERE UserID = @UserID", userIdSqlParam).AsNoTracking();
            return Ok(result);
        }

        [HttpGet("getuserdetails")]
        public IActionResult GetUserDetails()
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);
            var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);
            var userIdSqlParam = new SqlParameter("@UserID", userId);
            var result = _context.UserModels.FromSqlRaw(@"SELECT 
                u.UserID
                ,u.StaffName
                ,u.StaffEmail
                ,u.LoginName
                ,u.EmployeeNumber
                ,cc.CostCenterID
                ,cc.CostCenterDescription 'CostCenter'
                ,cc.CostCenterCode
                ,c.CompanyID
                ,c.CompanyDescription 'Company'
                ,c.CompanyCode
                ,u.IsLock
                ,u.AccessRole
                
                FROM Users u
                LEFT JOIN CostCenters cc ON u.CostCenterID = cc.CostCenterID
                LEFT JOIN Companies c ON u.CompanyID = c.CompanyID
                WHERE UserID = @UserID", userIdSqlParam).AsNoTracking();
            return Ok(result);
        }


        //[AllowAnonymous]
        [HttpPost("updateSelectedToggleCols")]
        public IActionResult Update([FromBody] UpdateUserToggleColsModel model)
        {
            // register the instance so that it is disposed when request ends
            HttpContext.Response.RegisterForDispose(_disposable);

            if (model == null || model.SelectedToggleColumns == null)
            {
                return BadRequest(new { message = "All fields cannot be blank." });
            }
            else if (model.WebPage != TOGGLECOLS_REPORTS_ASSETDETAILS)
            {
                return BadRequest(new { message = "Invalid WebPage." });
            }

            try
            {
                _userService.UpdateUserSavedToggleColumns(model);
                return Ok();
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("getusername")]
        public IActionResult GetUserName()
        {
            return Ok("mahath");

        }
    }
}
