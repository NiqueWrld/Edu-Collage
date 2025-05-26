using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public string? Link { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public NotificationType Type { get; set; }

        // Navigation property
        public IdentityUser User { get; set; }
    }

    public enum NotificationType
    {
        General,
        Assignment,
        Quiz,
        Grade,
        System
    }
}