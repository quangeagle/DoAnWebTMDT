using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;
using DoAnWebTMDT.Hubs;
using DoAnWebTMDT.Models;

namespace DoAnWebTMDT.Controllers
{
    public class ChatsController : Controller
    {
        private readonly WebBanHangTmdtContext _context;
        private readonly IHubContext<ChatHub> _chatHub;

        public ChatsController(WebBanHangTmdtContext context, IHubContext<ChatHub> chatHub)
        {
            _context = context;
            _chatHub = chatHub;
        }

        private int? GetCurrentUserId()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = HttpContext.Session.GetInt32("AccountId");

            Console.WriteLine($"🔍 Debug: SessionID = {sessionId}, AccountId = {userId}");

            if (userId == null)
            {
                Console.WriteLine("⚠️ Lỗi: Không tìm thấy AccountId trong session!");
            }

            return userId;
        }


        // Lấy danh sách tin nhắn theo ChatRoomId
        public async Task<IActionResult> Index(Guid chatRoomId)
        {
            // Giả sử UserId được lưu trong session (hoặc lấy từ Identity)
            var userId = HttpContext.Session.GetInt32("AccountId"); // Hoặc cách khác để lấy UserId
            if (userId == null)
            {
                return RedirectToAction("Login"); // Chuyển hướng nếu chưa đăng nhập
            }

            var messages = await _context.Chats
                .Where(c => c.ChatRoomId == chatRoomId)
                .OrderBy(c => c.SentAt)
                .ToListAsync();

            ViewBag.UserId = userId; // Truyền UserId vào View

            return View(messages);
        }


        public IActionResult AdminChat()
        {
            return View();
        }

        // API để gửi tin nhắn (cho frontend gọi)
  
        [HttpPost]
      
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageModel model)
        {
            int? senderId = GetCurrentUserId();

            if (senderId == null)
            {
                return BadRequest(new { error = "Lỗi xác thực, vui lòng đăng nhập lại." });
            }

            // Nếu người gửi không phải admin (4), thì người nhận luôn là admin
            if (senderId != 4)
            {
                model.ReceiverId = 4;
            }
            else
            {
                // Nếu admin gửi, tìm người dùng gần nhất đã nhắn tin
                var lastMessage = _context.Chats
                    .Where(c => c.SenderId != 4) // Chỉ lấy tin nhắn từ user
                    .OrderByDescending(c => c.SentAt)
                    .FirstOrDefault();

                if (lastMessage != null)
                {
                    model.ReceiverId = lastMessage.SenderId;
                }
                else
                {
                    return BadRequest(new { error = "Không có người dùng nào để trả lời." });
                }
            }

            // Kiểm tra nội dung tin nhắn
            model.Message = model.Message?.Trim();
            if (string.IsNullOrEmpty(model.Message))
            {
                return BadRequest(new { error = "Nội dung tin nhắn không được để trống." });
            }

            // Kiểm tra hoặc tạo ChatRoomId
            var chatRoomId = _context.Chats
                .Where(c => (c.SenderId == senderId && c.ReceiverId == model.ReceiverId) ||
                            (c.SenderId == model.ReceiverId && c.ReceiverId == senderId))
                .Select(c => c.ChatRoomId)
                .FirstOrDefault();

            if (chatRoomId == Guid.Empty)
            {
                chatRoomId = Guid.NewGuid();
            }

            // Lưu tin nhắn vào database
            var chat = new Chat
            {
                SenderId = senderId.Value,
                ReceiverId = model.ReceiverId,
                Message = model.Message,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                ChatRoomId = chatRoomId
            };

            _context.Chats.Add(chat);
            int saved = await _context.SaveChangesAsync();

            if (saved > 0)
            {
                await _chatHub.Clients.User(model.ReceiverId.ToString()).SendAsync("ReceiveMessage", senderId, model.Message);
                return Ok(new { success = true, chatRoomId = chatRoomId });
            }
            else
            {
                return StatusCode(500, new { error = "Lỗi hệ thống, vui lòng thử lại." });
            }
        }



      [HttpGet]
public async Task<IActionResult> GetMessages(int senderId, int receiverId)
{
    var messages = await _context.Chats
        .Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                    (m.SenderId == receiverId && m.ReceiverId == senderId))
        .OrderBy(m => m.SentAt)
        .Select(m => new
        {
            SenderId = m.SenderId,
            Message = m.Message,
            SentAt = m.SentAt
        })
        .ToListAsync();

    return Ok(messages);
}



        [HttpGet]
        public async Task<IActionResult> GetChatUsers()
        {
            var users = await _context.Chats
                .GroupBy(c => c.SenderId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastMessage = g.OrderByDescending(c => c.SentAt).FirstOrDefault().Message,
                    LastSentAt = g.OrderByDescending(c => c.SentAt).FirstOrDefault().SentAt
                })
                .OrderByDescending(c => c.LastSentAt)
                .ToListAsync();

            return Ok(users);
        }


        // API để cập nhật trạng thái tin nhắn đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid chatRoomId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return BadRequest("User ID không hợp lệ.");
            }

            var messages = await _context.Chats
                .Where(c => c.ChatRoomId == chatRoomId && c.ReceiverId == userId && c.IsRead == false)
                .ToListAsync();

            if (messages.Any())
            {
                foreach (var msg in messages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }
    }
}
    