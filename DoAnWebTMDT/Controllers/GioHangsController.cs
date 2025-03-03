using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;
using DoAnWebTMDT.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

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

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            int? userId = GetCurrentUserId();
            if (userId != null)
            {
                // 🟢 Người dùng đã đăng nhập → Thêm vào database
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
                // 🟠 Khách vãng lai → Lưu vào Session
                var guestCart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("GuestCart") ?? new List<CartItemViewModel>();

                var existingItem = guestCart.FirstOrDefault(c => c.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    guestCart.Add(new CartItemViewModel
                    {
                        ProductId = productId,
                        ProductName = product.Name,
                        ProductImage = product.MediaPath,
                        NewPrice = product.NewPrice ?? 0,
                        Quantity = quantity
                    });
                }

                HttpContext.Session.SetObjectAsJson("GuestCart", guestCart);
            }

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng!" });
        }


        // Hiển thị giỏ hàng của tài khoản đăng nhập
        public IActionResult Index()
        {
            int? userId = GetCurrentUserId();  // Lấy ID nếu có đăng nhập
            List<CartItemViewModel> cartItems = new List<CartItemViewModel>();

            if (userId != null)
            {
                // 🛒 Lấy giỏ hàng từ database nếu người dùng đã đăng nhập
                cartItems = _context.GioHangs
                    .Where(g => g.AccountId == userId)
                    .Include(g => g.Product)
                    .Select(g => new CartItemViewModel
                    {
                        ProductId = g.ProductId,
                        ProductName = g.Product.Name,
                        ProductImage = g.Product.MediaPath,
                        NewPrice = g.Product.NewPrice ?? 0,
                        Quantity = g.Quantity
                    })
                    .ToList();
            }
            else
            {
                // 🛍️ Lấy giỏ hàng từ Session nếu là khách vãng lai
                var sessionCart = HttpContext.Session.GetString("GuestCart");
                if (!string.IsNullOrEmpty(sessionCart))
                {
                    cartItems = JsonConvert.DeserializeObject<List<CartItemViewModel>>(sessionCart);
                }
            }

            return View(cartItems);  // Trả về danh sách giỏ hàng của cả khách vãng lai & đăng nhập
        }


        public IActionResult GioHangCus()
        {
            int? userId = GetCurrentUserId(); // Lấy ID người dùng nếu có đăng nhập
            List<CartItemViewModel> cartItems = new List<CartItemViewModel>();

            if (userId != null)
            {
                // 🛒 Lấy giỏ hàng từ database nếu người dùng đã đăng nhập
                cartItems = _context.GioHangs
                    .Where(g => g.AccountId == userId)
                    .Include(g => g.Product)
                    .Select(g => new CartItemViewModel
                    {
                        ProductId = g.ProductId,
                        ProductName = g.Product.Name,
                        ProductImage = g.Product.MediaPath,
                        NewPrice = g.Product.NewPrice ?? 0,
                        Quantity = g.Quantity
                    })
                    .ToList();
            }
            else
            {
                // 🛍️ Lấy giỏ hàng từ Session nếu là khách vãng lai
                var sessionCart = HttpContext.Session.GetString("GuestCart");
                if (!string.IsNullOrEmpty(sessionCart))
                {
                    cartItems = JsonConvert.DeserializeObject<List<CartItemViewModel>>(sessionCart);
                }
            }

            return View(cartItems);  // Trả về view hiển thị giỏ hàng
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