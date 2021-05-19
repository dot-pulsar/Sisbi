using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Sisbi.Hubs
{
    public class ResponseHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("Notification", new
            {
                user = user.ToUpper(), 
                message = message.ToUpper()
            });
        }
    }
}