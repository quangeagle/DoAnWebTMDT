using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;
using DoAnWebTMDT.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace DoAnWebTMDT.Controllers
{
    public class GuestCartsController : Controller
    {
        private readonly WebBanHangTmdtContext _context;
        private const string CartSessionKey = "GuestCart";

        public GuestCartsController(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        // 🔹 Lấy giỏ hàng từ Session (Dùng List<CartItemViewModel> thay vì GuestCart)
        private List<CartItemViewModel> GetGuestCartFromSession()
        {
            var sessionCart = HttpContext.Session.GetString(CartSessionKey);
            return !string.IsNullOrEmpty(sessionCart)
                ? JsonConvert.DeserializeObject<List<CartItemViewModel>>(sessionCart)
                : new List<CartItemViewModel>();
        }

        // 🔹 Lưu giỏ hàng vào Session
        private void SaveGuestCartToSession(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));
        }

        // 🔹 Hiển thị giỏ hàng (Dùng chung View với giỏ hàng của user đăng nhập)
        public IActionResult Index()
        {
            var cart = GetGuestCartFromSession();
            return View("~/Views/GioHangs/Index.cshtml", cart);
        }

        // 🔹 Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int productId, int quantity)
        {
            var cart = GetGuestCartFromSession();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var product = _context.Products.Find(productId);
                if (product == null)
                {
                    return NotFound(); // Nếu sản phẩm không tồn tại
                }

                cart.Add(new CartItemViewModel
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductImage = product.MediaPath,
                    NewPrice = product.NewPrice ?? 0,
                    Quantity = quantity
                });
            }

            SaveGuestCartToSession(cart);
            return RedirectToAction(nameof(Index));
        }

        // 🔹 Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int quantity)
        {
            var cart = GetGuestCartFromSession();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                item.Quantity = Math.Max(1, quantity);
                SaveGuestCartToSession(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        // 🔹 Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int productId)
        {
            var cart = GetGuestCartFromSession();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveGuestCartToSession(cart);

            return RedirectToAction(nameof(Index));
        }

        // 🔹 Xóa toàn bộ giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
