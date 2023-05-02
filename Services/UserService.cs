using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.Users;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BCryptNet = BCrypt.Net.BCrypt;
using System.DirectoryServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace WebApi.Services
{
    public interface IUserService
    {
        UserModel Authenticate(string LoginName, string password);
        bool Logout(int userID, string sessionID);
        IEnumerable<User> GetAll();
        User GetById(int id);
        void UpdateLastLogin(int userID, bool isSuccess, DateTime now);
        Guid CreateUserSession(int userID, bool isLDAPLogin, string loginSource, string deviceIDOrIPAddress, DateTime now);
        bool CheckSessionValidity(string sessionID);
        IEnumerable<ToggleColsModel> GetUserSavedToggleColumns(int userID);
        IEnumerable<string> GetDefaultToggleColumns(string webPage);
        void UpdateUserSavedToggleColumns(UpdateUserToggleColsModel model);
        IEnumerable<FiltersModel> GetUserSavedFilters(int userID);
        List<int> GetUserRoles(int userID);
        List<UserRole>GetUserRolesObj(int userID);
        List<int> GetUserRights(int userID);
        UserModel GetUserRolesAndRights(UserModel userModel);
    }

    public class UserService : IUserService, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        private IUserRoleService _userRoleService;
        private IUserRightService _userRightService;
        private DataContext _context;
        private readonly AppSettings _appSettings;
        private ILogger _log;

        private const int SESSION_EXPIRED_PERIOD_MINTUES = 720; // 12 hours
        private const int RESET_TOKEN_EXPIRED_PERIOD_MINTUES = 15; // 15 minutes

        public UserService(IOptions<AppSettings> appSettings, ILogger<UserService> log, DataContext context, IUserRoleService userRoleService, IUserRightService userRightService)
        {
            _appSettings = appSettings.Value;
            _log = log;
            _context = context;
            _userRoleService = userRoleService;
            _userRightService = userRightService;
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

        public UserModel Authenticate(string LoginName, string password)
        {
            if (string.IsNullOrEmpty(LoginName) || string.IsNullOrEmpty(password))
                return null;

            // Zack: Only find users that are not "soft-deleted"
            var user = _context.Users.Where(y => y.Hide == false).SingleOrDefault(x => x.LoginName == LoginName);

            // check if LoginName exists
            if (user == null)
                return null;

            LogInfo("user is not null. user.UserID: " + user.UserID);

            // Verify password
            if (!BCryptNet.Verify(password, user.PasswordHash))
            {
                UserModel tempUserModel = new UserModel();
                tempUserModel.UserID = user.UserID;
                tempUserModel.IsWrongPassword = true;
                return tempUserModel;
            }

            // authentication successful
            UserModel um = user.getVewUser();

            um.CompanyCode = _context.Companies.Find(user.CompanyID).CompanyCode;
            um.Company = _context.Companies.Find(user.CompanyID).CompanyDescription;
            //um.BusinessAreaCode = _context.BusinessAreas.Find(user.BusinessAreaID).BusinessAreaCode;
            //um.BusinessArea = _context.BusinessAreas.Find(user.BusinessAreaID).BusinessAreaDescription;
            um.CostCenterCode = _context.CostCenters.Find(user.CostCenterID).CostCenterCode;
            um.CostCenter = _context.CostCenters.Find(user.CostCenterID).CostCenterDescription;
            // Get All UserRoles of a user
            um = GetUserRolesAndRights(um);

            // Checks against password expiry
            if (DateTime.Now >= user.PasswordExpiredOn)
                um.IsPasswordExpired = true;
            else
                um.IsPasswordExpired = false;

            return um;
        }

        public bool Logout(int userID, string sessionID)
        {
            try
            {
                var existingUserSession = _context.UserSessions.Find(new Guid(sessionID));

                if (existingUserSession != null)
                {
                    if (existingUserSession.CreatedBy == userID)
                    {
                        if (!existingUserSession.IsLogout)
                        {
                            existingUserSession.IsLogout = true;
                            existingUserSession.ModifiedBy = userID;
                            existingUserSession.ModifiedOn = DateTime.Now;
                            _context.UserSessions.Update(existingUserSession);
                            _context.SaveChanges();

                            LogInfo("Logout. sessionID " + sessionID + " userID: " + userID + " Session logout successfully.");
                            return true;
                        }
                        else
                        {
                            LogInfo("Logout. sessionID " + sessionID + " userID: " + userID + " Session already logged out in DB.");
                            return false;
                        }
                    }
                    else
                    {
                        LogInfo("Logout. sessionID " + sessionID + " userID: " + userID + " UserID does not match with Session in DB.");
                        return false;
                    }
                }
                else
                {
                    LogInfo("Logout. sessionID " + sessionID + " userID: " + userID + " SessionID does not exist in DB.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogInfo("Logout. sessionID " + sessionID + " userID: " + userID + " ex:" + ex.Message);
                return false;
            }

        }

        public IEnumerable<User> GetAll()
        {
            return _context.Set<User>().AsNoTracking().OrderBy(user => user.UserID);
        }

        public User GetById(int id)
        {
            return _context.Users.Find(id);
        }

        public void UpdateLastLogin(int userID, bool isSuccess, DateTime now)
        {
            var user = _context.Users.Find(userID);
            if (user != null)
            {
                if (isSuccess)
                {
                    user.LastSuccessfulLoginOn = now;
                    user.ModifiedBy = userID;
                    user.ModifiedOn = now;
                }
                else
                {
                    user.LastFailedLoginOn = now;
                    user.ModifiedBy = userID;
                    user.ModifiedOn = now;
                }
                _context.Users.Update(user);

                _context.SaveChanges();
            }
        }

        public Guid CreateUserSession(int userID, bool isLDAPLogin, string loginSource, string deviceIDOrIPAddress, DateTime now)
        {
            Guid newID;
            UserSession existingUserSession = null;

            // generate a guid that does not exist in DB
            do
            {
                newID = Guid.NewGuid();
                existingUserSession = _context.UserSessions.Find(newID);

            } while (existingUserSession != null);

            UserSession userSession = new UserSession();
            userSession.SessionID = newID;
            userSession.IsLDAPLogin = isLDAPLogin;
            userSession.LoginSource = loginSource;
            userSession.DeviceIDOrIPAddress = deviceIDOrIPAddress;
            userSession.ExpiredOn = now.AddMinutes(SESSION_EXPIRED_PERIOD_MINTUES);
            userSession.IsLogout = false;
            userSession.CreatedBy = userID;
            userSession.CreatedOn = now;

            AddtoDB(userSession);
            CommittoDB(userSession);

            return newID;
        }

        public bool CheckSessionValidity(string sessionID)
        {
            try
            {
                var existingUserSession = _context.UserSessions.Find(new Guid(sessionID));

                if (existingUserSession != null)
                {
                    if (existingUserSession.IsLogout || DateTime.Now >= existingUserSession.ExpiredOn)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static ClaimsPrincipal GetClaims(string token, byte[] key)
        {
            var handler = new JwtSecurityTokenHandler();
            var validations = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            return handler.ValidateToken(token, validations, out var tokenSecure);
        }
        public static Int32 GetUserIdFromToken(string authorizationHeaders, string appSecret)
        {
            string token = authorizationHeaders.Substring("Bearer ".Length).Trim();
            var key = Encoding.ASCII.GetBytes(appSecret);
            var tokenClaims = GetClaims(token, key);
            var userId = tokenClaims.Claims.Where(c => c.Type == ClaimTypes.Name).FirstOrDefault().Value;

            return Int32.Parse(userId);
        }

        public static string GetSessionIDFromToken(string authorizationHeaders, string appSecret)
        {
            string token = authorizationHeaders.Substring("Bearer ".Length).Trim();
            var key = Encoding.ASCII.GetBytes(appSecret);
            var tokenClaims = GetClaims(token, key);
            var sessionID = tokenClaims.Claims.Where(c => c.Type == CustomClaimTypes.AMSSessionID).FirstOrDefault().Value;

            return sessionID;
        }

        public IEnumerable<ToggleColsModel> GetUserSavedToggleColumns(int userID)
        {
            var toggleCols = (from toggleCol in _context.UserSavedToggleColumns
                              where toggleCol.UserID == userID
                              orderby toggleCol.UserSavedToggleColumnID ascending
                              select new ToggleColsModel
                              {
                                  WebPage = toggleCol.WebPage,
                                  SelectedColumn = toggleCol.SelectedColumn
                              }).ToArray();

            return toggleCols;
        }

        public IEnumerable<string> GetDefaultToggleColumns(string webPage)
        {
            var toggleCols = (from toggleCol in _context.DefaultToggleColumns
                              where toggleCol.WebPage == webPage
                              orderby toggleCol.DefaultToggleColumnID ascending
                              select toggleCol.SelectedColumn).ToList<string>();

            return toggleCols;
        }

        public void UpdateUserSavedToggleColumns(UpdateUserToggleColsModel model)
        {
            if (model.SelectedToggleColumns == null || model.SelectedToggleColumns.Count == 0)
                throw new AppException("At least 1 column must be specified.");

            var user = _context.Users.Find(model.UserID);
            if (user == null)
                throw new AppException("User not found");

            try
            {
                List<UserSavedToggleColumn> newCols = new List<UserSavedToggleColumn>();
                foreach (string colName in model.SelectedToggleColumns)
                {
                    UserSavedToggleColumn newCol = new UserSavedToggleColumn();
                    newCol.UserID = model.UserID;
                    newCol.WebPage = model.WebPage;
                    newCol.SelectedColumn = colName;
                    newCols.Add(newCol);
                }

                _context.UserSavedToggleColumns.RemoveRange(_context.UserSavedToggleColumns.Where(x => x.UserID == model.UserID));
                _context.UserSavedToggleColumns.AddRange(newCols);
                _context.SaveChanges();
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

        public IEnumerable<FiltersModel> GetUserSavedFilters(int userID)
        {
            var filters = (from filter in _context.UserSavedFilters
                           where filter.UserID == userID
                           orderby filter.UserSavedFilterID ascending
                           select new FiltersModel
                           {
                               WebPage = filter.WebPage,
                               AllFilters = filter.AllFilters
                           }).ToArray();

            return filters;
        }

        public UserModel GetUserRolesAndRights(UserModel userModel) 
        {
            // Get All UserRoles of a user
            userModel.UserRolesID = string.Join(",", this.GetUserRoles(userModel.UserID).ToArray());

            // Get All UserRights of a user
            // userModel.UserRightsID = this.GetUserRights(userModel.UserID).ToString();
            userModel.UserRightsID = string.Join(",", this.GetUserRights(userModel.UserID).ToArray());
            
            return userModel;
        }

        public List<int> GetUserRoles(int userID) {
            // Get All UserRoles of a user and update back to UserModel
            var userRolesObj = _context.UserJoinUserRoles.Where(u => u.UserID == userID && u.IsLock == false).ToList();
            var userRoles = 
                (from first in _userRoleService.GetAll()
                join second in userRolesObj on first.UserRoleID equals second.UserRoleID
                // select new UserRole { UserRoleID = first.UserRoleID, UserRoleDescription = first.UserRoleDescription }).ToList();
                select new { first.UserRoleID }).Select(x => x.UserRoleID).Distinct().OrderBy(x => x).ToList<int>();
            
            return userRoles;
        }
        public List<UserRole> GetUserRolesObj(int userID) {
            // Get All UserRoles of a user and update back to UserModel
            var userRolesObj = _context.UserJoinUserRoles.Where(u => u.UserID == userID && u.IsLock == false).ToList();
            var userRoles = 
                (from first in _userRoleService.GetAll()
                join second in userRolesObj on first.UserRoleID equals second.UserRoleID
                select new UserRole { UserRoleID = first.UserRoleID, UserRoleDescription = first.UserRoleDescription }).ToList();
                // select new { first.UserRoleID }).Select(x => x.UserRoleID).Distinct().OrderBy(x => x).ToList<int>();
            
            return userRoles;
        }

        public List<int> GetUserRights(int userID) {
            // Get All UserRights of a user and update back to UserModel
            var userRolesObj = _context.UserJoinUserRoles.Where(u => u.UserID == userID && u.IsLock == false).ToList();
            var userRights = 
                (from first in _userRoleService.GetAll()
                join second in userRolesObj on first.UserRoleID equals second.UserRoleID
                join third in _context.UserRoleJoinUserRights on first.UserRoleID equals third.UserRoleID
                join fourth in _userRightService.GetAll() on third.UserRightID equals fourth.UserRightID
                // select new UserRight { UserRightID = second.UserRightID, UserRightDescription = third.UserRightDescription }).ToList();
                select new { third.UserRightID }).Select(x => x.UserRightID).Distinct().OrderBy(x => x).ToList<int>();
            
            return userRights;
        }

      
        private void AddtoDB(User user)
        {
            try
            {
                _context.Users.Add(user);
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
        private void AddtoDB(UserJoinUserRole userRole)
        {
            try
            {
                _context.UserJoinUserRoles.Add(userRole);
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
        private void AddtoDB(UserSession userSession)
        {
            try
            {
                _context.UserSessions.Add(userSession);
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
            
        private void LogInfo(string message)
        {
            // INFO
            _log.LogInformation("AMSLog - " + message);
        }
        private void LogError(string message)
        {
            // FAIL
            _log.LogError("AMSLog - " + message);
        }
        private void LogDebug(string message)
        {
            // DBUG
            _log.LogDebug("AMSLog - " + message);
        }
        private void LogCritical(string message)
        {
            // CRIT
            _log.LogCritical("AMSLog - " + message);
        }
        private void LogWarning(string message)
        {
            // WARN
            _log.LogWarning("AMSLog - " + message);
        }
    }
}