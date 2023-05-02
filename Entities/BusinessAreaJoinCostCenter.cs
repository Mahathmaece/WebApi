using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class BusinessAreaJoinCostCenter
    {
        public int BusinessAreaID { get; set; }
        public int CostCenterID { get; set; }
    }
}