using Microsoft.AspNetCore.SignalR;

namespace DiscountCodeService.Hubs
{
    public class DiscountCodeHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
