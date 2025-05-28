using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public enum ModulePaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public class ModulePayment
    {
        public int ModulePaymentId { get; set; }
        public string IdentityUserId { get; set; }
        public int ModuleId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }

        [Column(TypeName = "nvarchar(24)")]
        public ModulePaymentStatus Status { get; set; }

        public Module Module { get; set; }
        public IdentityUser IdentityUser { get; set; }
    }
}
