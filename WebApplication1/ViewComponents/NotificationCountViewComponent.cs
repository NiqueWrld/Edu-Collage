using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Services;

namespace WebApplication1.ViewComponents
{
    public class NotificationCountViewComponent : ViewComponent
    {
        private readonly NotificationService _notificationService;

        public NotificationCountViewComponent(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return View(0);

            var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
            return View(count);
        }
    }
}