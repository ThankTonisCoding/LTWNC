using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace FinancialPlatform.WebUI.Hubs
{
    public class MarketHub : Hub
    {
        // Client (trình duyệt) sẽ gọi hàm này để tham gia vào nhóm nhận tin của một mã (ví dụ: BTCUSDT)
        public async Task JoinGroup(string symbol)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, symbol);
        }
    }
}