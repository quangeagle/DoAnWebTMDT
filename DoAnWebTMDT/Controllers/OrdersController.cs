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
            List<CheckoutItemViewModel> cartItems;
            List<AddressViewModel> userAddresses = new();

            if (userId.HasValue)
            {
                cartItems = _context.GioHangs
                    .Where(g => g.AccountId == userId)
                    .Include(g => g.Product)
                    .Where(g => g.Product != null)
                    .Select(g => new CheckoutItemViewModel
                    {
                        ProductId = g.ProductId,
                        ProductName = g.Product.Name,
                        ImageUrl = g.Product.MediaPath,
                        Quantity = g.Quantity,
                        NewPrice = g.Product.NewPrice ?? 0
                    }).ToList();

                userAddresses = _context.Addresses
                    .Where(a => a.AccountId == userId)
                    .Select(a => new AddressViewModel
                    {
                        AddressId = a.AddressId,
                        FullName = a.FullName,
                        Phone = a.Phone,
                        AddressDetail = a.AddressDetail
                    }).ToList();
            }
            else
            {
                var cartSession = HttpContext.Session.GetString("GuestCart");
                var guestCart = cartSession != null
                    ? JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartSession)
                    : new List<CartItemViewModel>();

                cartItems = guestCart.Select(g => new CheckoutItemViewModel
                {
                    ProductId = g.ProductId,
                    ProductName = g.ProductName,
                    ImageUrl = g.ProductImage,
                    Quantity = g.Quantity,
                    NewPrice = g.NewPrice
                }).ToList();
            }

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào trong giỏ hàng.";
                return RedirectToAction("Cart", "Shopping");
            }

            return View(new CheckoutViewModel
            {
                CartItems = cartItems,
                Addresses = userAddresses
            });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int? selectedAddressId, string? newFullName, string? newPhone, string? newAddressDetail, string fullName, string phoneNumber, string email, string paymentMethod)
        {
            int? userId = GetCurrentUserId();
            List<CartItemViewModel> cartItems;

            if (userId.HasValue)
            {
                cartItems = _context.GioHangs
                    .Include(g => g.Product)
                    .Where(g => g.AccountId == userId && g.Product != null)
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

            int? addressId = selectedAddressId;
            string finalAddress = newAddressDetail;

            if (userId.HasValue)
            {
                if (selectedAddressId.HasValue)
                {
                    var selectedAddress = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.AddressId == selectedAddressId);

                    if (selectedAddress != null)
                    {
                        addressId = selectedAddress.AddressId;
                        finalAddress = selectedAddress.AddressDetail;
                    }
                }
                else if (!string.IsNullOrEmpty(newAddressDetail) && !string.IsNullOrEmpty(newFullName) && !string.IsNullOrEmpty(newPhone))
                {
                    var newAddress = new Address
                    {
                        AccountId = userId.Value,
                        FullName = newFullName,
                        Phone = newPhone,
                        AddressDetail = newAddressDetail
                    };

                    _context.Addresses.Add(newAddress);
                    await _context.SaveChangesAsync();

                    addressId = newAddress.AddressId;
                    finalAddress = newAddress.AddressDetail;
                }
            }

            decimal totalAmount = cartItems.Sum(g => g.Quantity * g.NewPrice);

            var order = new Order
            {
                AccountId = userId,
                AddressId = addressId,
                GuestFullName = userId == null ? fullName : null,
                GuestPhone = userId == null ? phoneNumber : null,
                GuestEmail = userId == null ? email : null,
                GuestAddress = userId == null ? finalAddress : null,
                TotalAmount = totalAmount,
                OrderStatus = "Pending", // Mặc định Pending
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderDetails = cartItems.Select(item => new OrderDetail
            {
                OrderId = order.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.NewPrice
            }).ToList();

            _context.OrderDetails.AddRange(orderDetails);
            await _context.SaveChangesAsync();

            // Lưu thông tin thanh toán vào bảng Payment
            var payment = new Payment
            {
                OrderId = order.OrderId,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == "COD" ? "Completed" : "Pending",
                TransactionDate = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Nếu là COD, cập nhật đơn hàng thành công luôn
            if (paymentMethod == "COD")
            {
                order.OrderStatus = "Pending";
                await _context.SaveChangesAsync();
            }

            if (userId.HasValue)
            {
                _context.GioHangs.RemoveRange(_context.GioHangs.Where(g => g.AccountId == userId));
            }
            else
            {
                HttpContext.Session.Remove("GuestCart");
            }

            await _context.SaveChangesAsync();

            // Nếu là COD, chuyển đến trang thành công
            if (paymentMethod == "COD")
            {
                return RedirectToAction("OrderSuccess"); // Chuyển đến trang xác nhận thành công
            }

            // Nếu là chuyển khoản, tiếp tục tới ZaloPay
            return RedirectToAction("CreatePayment", "ZaloPay", new { orderId = order.OrderId, amount = order.TotalAmount });
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
             .Include(o => o.Payments) // 🔹 Thêm để lấy phương thức thanh toán
             .OrderByDescending(o => o.CreatedAt)
             .ToListAsync();


            return View(orders);
        }

        public IActionResult OrderSuccess(int orderId)
        {
            ViewData["OrderId"] = orderId;
            return View();
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
        [HttpPost]
        public async Task<IActionResult> ConfirmReceived(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (payment != null && payment.PaymentMethod == "COD")
            {
                order.IsPaid = true;
                payment.PaymentStatus = "Completed";
            }

            order.OrderStatus = "Completed";
            await _context.SaveChangesAsync();

            // ✅ Kiểm tra xem có AccountId hay không
            if (order.AccountId.HasValue)
            {
                Console.WriteLine($"✅ Cộng điểm cho AccountID: {order.AccountId.Value}, Tổng tiền: {order.TotalAmount}");
                AddLoyaltyPoints(order.AccountId.Value, order.TotalAmount);
            }
            else
            {
                Console.WriteLine("⚠️ Đơn hàng không có AccountId, không thể cộng điểm!");
            }

            return RedirectToAction("OrderHistory");
        }


        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = status;
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageOrders");
        }
        private void AddLoyaltyPoints(int accountId, decimal orderAmount)
        {
            int pointsEarned = (int)(orderAmount / 10000);
            Console.WriteLine($"🚀 Cộng {pointsEarned} điểm cho AccountID: {accountId}");

            var loyalty = _context.LoyaltyPoints.FirstOrDefault(x => x.AccountId == accountId);
            if (loyalty == null)
            {
                Console.WriteLine($"🆕 Tạo mới điểm thưởng cho AccountID: {accountId}");
                _context.LoyaltyPoints.Add(new LoyaltyPoint
                {
                    AccountId = accountId,
                    TotalPoints = pointsEarned
                });
            }
            else
            {
                Console.WriteLine($"🔄 Cập nhật điểm: {loyalty.TotalPoints} + {pointsEarned}");
                loyalty.TotalPoints += pointsEarned;
            }

            _context.SaveChanges();
        }



        public IActionResult ManageOrders()
        {
            var orders = _context.Orders
                  .Include(o => o.Payments) // Nếu có PaymentMethod
                  .ToList();
            return View(orders);
        }

    }
}

