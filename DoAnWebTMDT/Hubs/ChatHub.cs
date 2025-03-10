using DoAnWebTMDT.Data;
using DoAnWebTMDT.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DoAnWebTMDT.Hubs
{
    public class ChatHub : Hub
    {
        private readonly WebBanHangTmdtContext _context;

        public ChatHub(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            var chat = new Chat
            {
                SenderId = int.Parse(senderId),  // Ép kiểu về int
                ReceiverId = int.Parse(receiverId),  // Ép kiểu về int
                Message = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            // Gửi tin nhắn cho người nhận
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);

            // Cập nhật danh sách user nhắn tin cho Admin
            await Clients.All.SendAsync("NewMessage", senderId);
        }

    }
}
