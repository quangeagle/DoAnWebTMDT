using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;
using DoAnWebTMDT.Models;
using Microsoft.AspNetCore.Http;

namespace DoAnWebTMDT.Controllers
{
    public class GioHangsController : Controller
    {
        private readonly WebBanHangTmdtContext _context;

        public GioHangsController(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        // Lấy ID người dùng từ Session
        private int? GetCurrentUserId()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = HttpContext.Session.GetInt32("AccountId");
            Console.WriteLine($"🔍 Debug: SessionID = {sessionId}, AccountId = {userId}");
            return userId;
        }


        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ!" });
            }

            int? userId = GetCurrentUserId();
            if (userId != null)
            {
                // Người dùng đã đăng nhập → Thêm vào database
                var cartItem = await _context.GioHangs
                    .FirstOrDefaultAsync(g => g.AccountId == userId && g.ProductId == productId);

                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                }
                else
                {
                    _context.GioHangs.Add(new GioHang
                    {
                        AccountId = userId.Value,
                        ProductId = productId,
                        Quantity = quantity,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // Khách vãng lai → Lưu vào Session
                var guestCart = HttpContext.Session.GetObjectFromJson<List<GuestCart>>("GuestCart") ?? new List<GuestCart>();

                var existingItem = guestCart.FirstOrDefault(c => c.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    guestCart.Add(new GuestCart
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        CreatedAt = DateTime.Now
                    });
                }

                HttpContext.Session.SetObjectAsJson("GuestCart", guestCart);
            }

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng!" });
        }

        // Hiển thị giỏ hàng của tài khoản đăng nhập
        public IActionResult Index()
        {
            int? userId = GetCurrentUserId();
            Console.WriteLine($"🔍 Debug: AccountId được lấy từ Session = {userId}");

            if (userId == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var gioHang = _context.GioHangs
                .Where(g => g.AccountId == userId)
                .Include(g => g.Product)
                .ToList();

            return View(gioHang);
        }
        public IActionResult GioHangCus()
        {
            int? userId = GetCurrentUserId();

            // ✅ Kiểm tra nếu không có UserId, chuyển hướng đến trang đăng nhập
            if (userId == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            Console.WriteLine($"🔍 Debug: AccountId được lấy từ Session = {userId}");

            // ✅ Lấy danh sách giỏ hàng theo userId
            var gioHang = _context.GioHangs
                .Where(g => g.AccountId == userId)
                .Include(g => g.Product)
                .ToList();

            return View(gioHang);
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity) // Nhận quantity từ route
        {
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ!" });
            }

            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }

            var cartItem = await _context.GioHangs
                .Include(g => g.Product) // Load Product để tính tiền
                .FirstOrDefaultAsync(g => g.AccountId == userId && g.CartId == id);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();

                decimal totalItemPrice = cartItem.Quantity * cartItem.Product.NewPrice.GetValueOrDefault();
                decimal totalCartPrice = _context.GioHangs
                    .Where(g => g.AccountId == userId)
                    .Sum(g => g.Quantity * g.Product.NewPrice.GetValueOrDefault());

                return Json(new { success = true, totalItemPrice, totalCartPrice });
            }

            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng!" });
        }


        // Cập nhật số lượng sản phẩm trong giỏ hàng
     

        // Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int productId)
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }

            var cartItem = await _context.GioHangs
                .FirstOrDefaultAsync(g => g.AccountId == userId && g.ProductId == productId);

            if (cartItem != null)
            {
                _context.GioHangs.Remove(cartItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa thành công!" });
            }

            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng!" });
        }

        // Xóa toàn bộ giỏ hàng của tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập!" });
            }

            var userCart = _context.GioHangs.Where(g => g.AccountId == userId);
            _context.GioHangs.RemoveRange(userCart);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng!" });
        }
    }
}
