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
    public class OrdersController : Controller
    {
        private readonly WebBanHangTmdtContext _context;

        public OrdersController(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        // Lấy ID người dùng từ Session
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("AccountId");
        }

        // Hiển thị trang nhập thông tin mua hàng với danh sách sản phẩm trong giỏ hàng
        public IActionResult Checkout()
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            // Lấy danh sách sản phẩm từ giỏ hàng
            var gioHang = _context.GioHangs
                .Where(g => g.AccountId == userId)
                .Include(g => g.Product)
                .ToList();

            if (!gioHang.Any())
            {
                ViewBag.ErrorMessage = "Không có sản phẩm nào trong giỏ hàng.";
                return View(); // Trả về view nhưng không có sản phẩm
            }

            // Chuyển đổi sang ViewModel để hiển thị trong Checkout
            var checkoutModel = gioHang.Select(g => new CheckoutViewModel
            {
                ProductId = g.ProductId,
                ProductName = g.Product.Name,
                ImageUrl = g.Product.MediaPath,
                Quantity = g.Quantity,
                Price = g.Product.NewPrice ?? 0
            }).ToList();

            return View(checkoutModel);
        }


        // Xử lý thanh toán và tạo đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
     
        public async Task<IActionResult> ConfirmOrder(string fullName, string phoneNumber, string addressDetail)
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            // Kiểm tra xem userId có tồn tại trong bảng Account không
            var accountExists = await _context.Accounts.AnyAsync(a => a.AccountId == userId);
            if (!accountExists)
            {
                TempData["ErrorMessage"] = "Tài khoản không hợp lệ.";
                return RedirectToAction("Checkout");
            }

            var cartItems = await _context.GioHangs
                .Include(g => g.Product)
                .Where(g => g.AccountId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Checkout");
            }

            // ✅ Tạo Address trước để lấy AddressId
            var address = new Address
            {
                AccountId = userId.Value, // ✅ Thêm AccountId vào Address
                FullName = fullName,
                Phone = phoneNumber,
                AddressDetail = addressDetail
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync(); // Lưu trước để có AddressId

            // ✅ Tạo đơn hàng với AddressId vừa lưu
            var order = new Order
            {
                AccountId = userId.Value,
                AddressId = address.AddressId, // ✅ Gán AddressId hợp lệ
                TotalAmount = cartItems.Sum(g => g.Quantity * g.Product.NewPrice.GetValueOrDefault()),
                OrderStatus = "Pending", // Đổi từ "Chờ xác nhận" thành "Pending"

                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ Lưu chi tiết đơn hàng
            var orderDetails = cartItems.Select(item => new OrderDetail
            {
                OrderId = order.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Product.NewPrice ?? 0
            }).ToList();

            _context.OrderDetails.AddRange(orderDetails);

            // ✅ Xóa giỏ hàng sau khi đặt hàng thành công
            _context.GioHangs.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Orders");
        }


        // Hiển thị danh sách đơn hàng của người dùng hiện tại
        public async Task<IActionResult> Index()
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var orders = await _context.Orders
                .Where(o => o.AccountId == userId)
                .Include(o => o.Address)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ToListAsync();

            return View(orders);
        }

        // Xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Account)
                .Include(o => o.Address)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}
