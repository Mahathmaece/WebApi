using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using WebApi.Entities;
using System;

namespace WebApi.Models.Users
{
    public class UserCustodianModel
    {
        [Key]
        public int UserID { get; set; }
        public int EmployeeNumber { get; set; }
        public string StaffName { get; set; }
        public string StaffEmail { get; set; }
        public string OfficeContactNo { get; set; }
        public string LoginName { get; set; }
        public int CostCenterID { get; set; }
        public string CostCenter { get; set; }
        public int CompanyID { get; set; }
        public string Company { get; set; }
        public bool IsLock { get; set; }
        // Hide is to soft delete users
        public bool Hide { get; set; }
        // UserRolesID is a stringified list
        public string UserRolesID { get; set; }
        // public string UserRolesDescription { get; set; }
        // UserRightsID is a stringified list
        public string UserRightsID { get; set; }
        public string CostCenterCode { get; set; }
        public string CompanyCode { get; set; }
        public bool? IsPasswordExpired { get; set; }
        public bool? IsWrongPassword { get; set; }
        public string StaffCombinedDesc { get; set; }   // format is StaffName (StaffEmail)
    }
}