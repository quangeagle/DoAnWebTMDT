using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;
using DoAnWebTMDT.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DoAnWebTMDT.Controllers
{
    public class OrdersController : Controller
    {
        private readonly WebBanHangTmdtContext _context;

        public OrdersController(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("AccountId");
        }

        public IActionResult Checkout()
        {
            int? userId = GetCurrentUserId();
            List<CheckoutViewModel> checkoutModel;

            if (userId != null)
            {
                var gioHang = _context.GioHangs
                    .Where(g => g.AccountId == userId)
                    .Include(g => g.Product)
                    .ToList();

                checkoutModel = gioHang.Where(g => g.Product != null)
                    .Select(g => new CheckoutViewModel
                    {
                        ProductId = g.ProductId,
                        ProductName = g.Product.Name,
                        ImageUrl = g.Product.MediaPath,
                        Quantity = g.Quantity,
                        NewPrice = g.Product.NewPrice ?? 0
                    }).ToList();
            }
            else
            {
                var cartSession = HttpContext.Session.GetString("GuestCart");
                var guestCart = cartSession != null
                    ? JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartSession)
                    : new List<CartItemViewModel>();

                checkoutModel = guestCart.Select(g => new CheckoutViewModel
                {
                    ProductId = g.ProductId,
                    ProductName = g.ProductName,
                    ImageUrl = g.ProductImage,
                    Quantity = g.Quantity,
                    NewPrice = g.NewPrice
                }).ToList();
            }

            if (!checkoutModel.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào trong giỏ hàng.";
                return View();
            }

            return View(checkoutModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(string fullName, string phoneNumber, string email, string addressDetail)
        {
            int? userId = GetCurrentUserId();
            List<CartItemViewModel> cartItems;

            if (userId != null)
            {
                cartItems = _context.GioHangs.Include(g => g.Product)
                    .Where(g => g.AccountId == userId)
                    .Select(g => new CartItemViewModel
                    {
                        ProductId = g.ProductId,
                        ProductName = g.Product.Name,
                        Quantity = g.Quantity,
                        NewPrice = g.Product.NewPrice ?? 0
                    }).ToList();
            }
            else
            {
                var cartSession = HttpContext.Session.GetString("GuestCart");
                cartItems = cartSession != null
                    ? JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartSession)
                    : new List<CartItemViewModel>();
            }

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Checkout");
            }

            int? addressId = userId != null ? _context.Addresses
                .Where(a => a.AccountId == userId)
                .Select(a => a.AddressId)
                .FirstOrDefault() : null;

            decimal totalAmount = cartItems.Sum(g => g.Quantity * g.NewPrice);

            var order = new Order
            {
                AccountId = userId,
                GuestFullName = userId == null ? fullName : null,
                GuestPhone = userId == null ? phoneNumber : null,
                GuestEmail = userId == null ? email : null,
                GuestAddress = userId == null ? addressDetail : null,
                TotalAmount = totalAmount,
                OrderStatus = "Pending",
                CreatedAt = DateTime.Now,
                AddressId = addressId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.NewPrice
                };

                _context.OrderDetails.Add(orderDetail);
            }
            await _context.SaveChangesAsync();

            if (userId != null)
            {
                var gioHangItems = _context.GioHangs.Where(g => g.AccountId == userId);
                _context.GioHangs.RemoveRange(gioHangItems);
                await _context.SaveChangesAsync();
            }
            else
            {
                HttpContext.Session.Remove("GuestCart");
            }

            return RedirectToAction("CreatePayment", "ZaloPay", new { orderId = order.OrderId, amount = order.TotalAmount });
        }

        public IActionResult CreatePayment(int orderId, decimal amount)
        {
            return View();
        }

        public async Task<IActionResult> OrderHistory()
        {
            int? userId = GetCurrentUserId();
            if (userId == null)
            {
                return View(new List<Order>()); // ✅ Luôn trả về danh sách rỗng nếu không có đơn hàng
            }

            var orders = await _context.Orders
                .Where(o => o.AccountId == userId)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();  // ✅ Luôn lấy danh sách (List<Order>)

            return View(orders);
        }


        [HttpPost]
        public async Task<IActionResult> GuestOrderLookup(string phoneNumber)
        {
            var orders = await _context.Orders
                .Where(o => o.GuestPhone == phoneNumber)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();  // ✅ Đảm bảo trả về danh sách

            if (!orders.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng nào cho số điện thoại này.";
                return View(new List<Order>()); // ✅ Trả về danh sách rỗng thay vì null
            }

            return View("OrderHistory", orders);
        }

    }
}
