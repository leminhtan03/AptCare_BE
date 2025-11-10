using AptCare.Service.Dtos.ChatDtos;
using Microsoft.AspNetCore.SignalR;
namespace AptCare.Service.Hub;

public class ChatHub: Microsoft.AspNetCore.SignalR.Hub
{
    public async Task SendMessage(MessageDto message)
    {
        await Clients.Group(message.Slug).SendAsync("ReceiveMessage", message);
    }
    public async Task JoinConversation(string slug)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, slug);
            Console.WriteLine("User joined conversation:  "+ slug);
            //await Clients.OthersInGroup(slug).SendAsync("UserJoined", new { User = Context.UserIdentifier, Slug = slug });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
            
    }
    public async Task LeaveConversation(string slugConversation)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, slugConversation);
            //await Clients.All.SendAsync("User left conversation: "+ slugConversation);
            Console.WriteLine("User left conversation: "+ slugConversation);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }           
    }

    public async Task MarkAsDeliveried(IEnumerable<int> messageIds, string slug)
    {
        await Clients.Group(slug).SendAsync("MarkAsDeliveried", messageIds);
    }
    public async Task MarkAsRead(IEnumerable<int> messageIds, string slug)
    {
        await Clients.Group(slug).SendAsync("MarkAsRead", messageIds);
    }
}