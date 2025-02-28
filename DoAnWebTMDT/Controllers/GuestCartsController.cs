using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // Lấy giỏ hàng từ Session
        private List<GuestCart> GetGuestCartFromSession()
        {
            var sessionCart = HttpContext.Session.GetString(CartSessionKey);
            return sessionCart != null ? JsonConvert.DeserializeObject<List<GuestCart>>(sessionCart) : new List<GuestCart>();
        }

        // Lưu giỏ hàng vào Session
        private void SaveGuestCartToSession(List<GuestCart> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));
        }

        // GET: GuestCarts (Hiển thị giỏ hàng)
        public IActionResult Index()
        {
            var cart = GetGuestCartFromSession();
            foreach (var item in cart)
            {
                item.Product = _context.Products.Find(item.ProductId);
            }
            return View(cart);
        }

        // POST: GuestCarts/Create (Thêm sản phẩm vào giỏ hàng)
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
                if (product != null)
                {
                    cart.Add(new GuestCart
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        CreatedAt = DateTime.Now,
                        Product = product
                    });
                }
            }

            SaveGuestCartToSession(cart);
            return RedirectToAction(nameof(Index));
        }

        // POST: GuestCarts/Update (Cập nhật số lượng sản phẩm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int quantity)
        {
            var cart = GetGuestCartFromSession();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                item.Quantity = quantity;
                SaveGuestCartToSession(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: GuestCarts/Delete (Xóa sản phẩm khỏi giỏ hàng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int productId)
        {
            var cart = GetGuestCartFromSession();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveGuestCartToSession(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: GuestCarts/Clear (Xóa toàn bộ giỏ hàng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
