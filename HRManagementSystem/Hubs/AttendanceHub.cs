using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HRManagementSystem.Hubs
{
    [Authorize]
    public class AttendanceHub : Hub
    {
        public async Task JoinCompanyGroup(string companyCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{companyCode}");
        }

        public async Task LeaveCompanyGroup(string companyCode)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Company_{companyCode}");
        }

        public override async Task OnConnectedAsync()
        {
            var userCompanyCode = Context.User?.FindFirst("CompanyCode")?.Value ?? "0";
            var userRole = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Auto-join user to their company group
            if (userRole == "Admin")
            {
                // Admin can receive all company updates
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllCompanies");
            }
            else if (!string.IsNullOrEmpty(userCompanyCode) && userCompanyCode != "0")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Company_{userCompanyCode}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Cleanup is automatic for groups
            await base.OnDisconnectedAsync(exception);
        }
    }
}