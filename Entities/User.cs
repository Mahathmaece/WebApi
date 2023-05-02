using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebApi.Models.Users;

namespace WebApi.Entities
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public int EmployeeNumber { get; set; }
        [Required]
        [StringLength(50)]
        public string LoginName { get; set; }
        [StringLength(255)]
        public string StaffEmail { get; set; }
        [Required]
        [StringLength(100)]
        public string StaffName { get; set; }
        [StringLength(50)]
        public string OfficeContactNo { get; set; } 
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }
        // [Required]
        [StringLength(255)]
        public string PasswordSalt { get; set; }
        public int CompanyID { get; set; }
        public int? BusinessAreaID { get; set; }
        public int CostCenterID { get; set; }
        public int EmploymentStatus { get; set; }
        public bool IsLock { get; set; }
        public bool Hide { get; set; }
        public int ModifiedBy { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime ModifiedOn { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime PasswordLastModifiedOn { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime PasswordExpiredOn { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime? LastSuccessfulLoginOn { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime? LastFailedLoginOn { get; set; }

        // field that is updated by SAP pulling interface app only (for Users trigger to determine if need to insert into Audit Log)
        public bool? IsLastModifiedBySAPInterface { get; set; }

        public UserModel getVewUser() {
            UserModel viewUser = new UserModel();
            viewUser.UserID = this.UserID;
            viewUser.EmployeeNumber = this.EmployeeNumber;
            viewUser.StaffName = this.StaffName;
            viewUser.StaffEmail = this.StaffEmail;
            viewUser.OfficeContactNo = this.OfficeContactNo;
            viewUser.LoginName = this.LoginName;
            viewUser.CostCenterID = this.CostCenterID;
            viewUser.CompanyID = this.CompanyID;
            viewUser.IsLock = this.IsLock;
            viewUser.Hide = this.Hide;
            viewUser.BusinessAreaID = this.BusinessAreaID;
            // viewUser.UserRoles = new List<UserRole>();
            // viewUser.UserRights = new List<UserRight>();

            return viewUser;
        }
    }
}