using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebAPI.Domain.Models;
using WebAPI.Infrastructure.Data.Context;

namespace WebAPI.Application.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new();
    private readonly Context _context;
    public ChatHub(Context context)
    {
        _context = context;
    }

    public async Task JoinChat(string userId)
    {
        // Проверяем, что userId соответствует аутентифицированному пользователю
        var authenticatedUserId = Context.User?.FindFirst("sub")?.Value ?? 
                                  Context.User?.FindFirst("nameid")?.Value ??
                                  Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        
        if (authenticatedUserId != userId)
        {
            throw new HubException("Unauthorized: User ID mismatch");
        }
        
        UserConnections[userId] = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        await Clients.Others.SendAsync("UserConnected", userId);
    }
        
    public async Task SendVoice(string user, string fileUrl)
    {
        await Clients.All.SendAsync("ReceiveVoice", user, fileUrl);
    }

    public async Task SendVoiceMessage(string fromUserId, string toUserId, string fileUrl, string fileName = "")
    {
        var timestamp = DateTime.UtcNow;
        var message = !string.IsNullOrEmpty(fileName) ? $"Голосовое сообщение: {fileName}" : "Голосовое сообщение";

        var chatMessage = new ChatMessage
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Message = message,
            MessageType = "voice",
            FileUrl = fileUrl,
            Timestamp = timestamp,
            IsRead = false
        };

        _context.ChatMessages.Add(chatMessage);

        await UpdateOrCreateConversation(fromUserId, toUserId);

        await _context.SaveChangesAsync();

        var id = chatMessage.Id;
        var ts = chatMessage.Timestamp;

        await Clients.Group($"User_{toUserId}").SendAsync("ReceiveVoiceMessage", fromUserId, fileUrl, message, ts);
        await Clients.Group($"User_{toUserId}").SendAsync("ReceiveVoiceMessageV2", fromUserId, id, fileUrl, message, ts);

        await Clients.Group($"User_{fromUserId}").SendAsync("VoiceMessageSent", toUserId, fileUrl, message, ts);
        await Clients.Group($"User_{fromUserId}").SendAsync("VoiceMessageSentV2", toUserId, id, fileUrl, message, ts);
    }
        
    public async Task SendMessageToUser(string fromUserId, string toUserId, string message)
    {
        var timestamp = DateTime.UtcNow;

        var chatMessage = new ChatMessage
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Message = message,
            Timestamp = timestamp,
            IsRead = false
        };

        _context.ChatMessages.Add(chatMessage);

        await UpdateOrCreateConversation(fromUserId, toUserId);

        await _context.SaveChangesAsync();

        var id = chatMessage.Id;
        var ts = chatMessage.Timestamp;

        await Clients.Group($"User_{toUserId}").SendAsync("ReceiveMessage", fromUserId, message, ts);                // legacy
        await Clients.Group($"User_{toUserId}").SendAsync("ReceiveMessageV2", fromUserId, id, message, ts);         // new

        await Clients.Group($"User_{fromUserId}").SendAsync("MessageSent", toUserId, message, ts);                  // legacy
        await Clients.Group($"User_{fromUserId}").SendAsync("MessageSentV2", toUserId, id, message, ts);            // new
    }

    public async Task GetOnlineUser(string userId)
    {
        var isOnline = UserConnections.ContainsKey(userId);
        await Clients.Caller.SendAsync("UserOnlineStatus", userId, isOnline);
    }
        
    public async Task GetOnlineUsers()
    {
        var onlineUsers = UserConnections.Keys.ToList();
        await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
    }

    public async Task UserTyping(string fromUserId, string toUserId)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("UserTyping", fromUserId);
    }

    public async Task UserStoppedTyping(string fromUserId, string toUserId)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("UserStoppedTyping", fromUserId);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (!string.IsNullOrEmpty(userId))
        {
            UserConnections.TryRemove(userId, out _);
            await Clients.Others.SendAsync("UserDisconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task UpdateOrCreateConversation(string user1Id, string user2Id)
    {
        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

        if (conversation == null)
        {
            conversation = new ChatConversation
            {
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = DateTime.UtcNow,
                LastMessageTime = DateTime.UtcNow,
                IsActive = true
            };
            _context.ChatConversations.Add(conversation);
        }
        else
        {
            conversation.LastMessageTime = DateTime.UtcNow;
        }
    }

    public async Task NotifyMessageDeleted(int messageId, string toUserId, string fromUserId)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("MessageDeleted", messageId);
        await Clients.Group($"User_{fromUserId}").SendAsync("MessageDeleted", messageId);
    }

    public async Task NotifyMessageEdited(int messageId, string newContent, string toUserId, string fromUserId)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("MessageEdited", messageId, newContent);
        await Clients.Group($"User_{fromUserId}").SendAsync("MessageEdited", messageId, newContent);
    }

    public static async Task BroadcastMessageDeleted(IHubContext<ChatHub> hub, int messageId, string toUserId, string fromUserId)
    {
        await hub.Clients.Group($"User_{toUserId}").SendAsync("MessageDeleted", messageId);
        await hub.Clients.Group($"User_{fromUserId}").SendAsync("MessageDeleted", messageId);
    }

    public static async Task BroadcastMessageEdited(IHubContext<ChatHub> hub, int messageId, string newContent, string toUserId, string fromUserId)
    {
        await hub.Clients.Group($"User_{toUserId}").SendAsync("MessageEdited", messageId, newContent);
        await hub.Clients.Group($"User_{fromUserId}").SendAsync("MessageEdited", messageId, newContent);
    }

    // Дополнительные методы для совместимости с фронтендом
    public async Task BroadcastMessageDeleted(string messageId, string toUserId)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("MessageDeleted", messageId);
    }

    public async Task SendMessageDeleted(string toUserId, string messageId)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("MessageDeleted", messageId);
    }

    public async Task BroadcastMessageEdited(string messageId, string newContent, string toUserId)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("MessageEdited", messageId, newContent);
    }

    public async Task SendMessageEdited(string toUserId, string messageId, string newContent)
    {
        await Clients.Group($"User_{toUserId}").SendAsync("MessageEdited", messageId, newContent);
    }
}