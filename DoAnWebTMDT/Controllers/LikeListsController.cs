using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;
using DoAnWebTMDT.Models;
using Newtonsoft.Json;

namespace DoAnWebTMDT.Controllers
{
    public class LikeListsController : Controller
    {
        private readonly WebBanHangTmdtContext _context;

        public LikeListsController(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        // Lấy ID người dùng hiện tại từ session
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("AccountId");
        }

        // Xem danh sách yêu thích
        public IActionResult Index()
        {
            int? userId = GetCurrentUserId();
            List<LikeListViewModel> likedItems;

            if (userId.HasValue)
            {
                // Người dùng đã đăng nhập: Lấy từ bảng LikeList
                likedItems = _context.LikeLists
                    .Where(l => l.AccountId == userId)
                    .Include(l => l.Product)
                    .Where(l => l.Product != null)
                    .Select(l => new LikeListViewModel
                    {
                        ProductId = l.ProductId,
                        ProductName = l.Product.Name,
                        ImageUrl = l.Product.MediaPath,
                        NewPrice = l.Product.NewPrice ?? 0
                    }).ToList();
            }
            else
            {
                // Khách vãng lai: Lấy từ session
                var likeSession = HttpContext.Session.GetString("GuestLikeList");
                var guestLikes = likeSession != null
                    ? JsonConvert.DeserializeObject<List<LikeListViewModel>>(likeSession)
                    : new List<LikeListViewModel>();

                likedItems = guestLikes;
            }

            if (!likedItems.Any())
            {
                TempData["Message"] = "Danh sách yêu thích của bạn đang trống.";
            }

            return View(likedItems);
        }

        // Thêm sản phẩm vào danh sách yêu thích
        [HttpPost]
        public async Task<IActionResult> AddToLikeList(int productId)
        {
            try
            {
                int? userId = GetCurrentUserId();
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                if (userId.HasValue)
                {
                    var existingLike = await _context.LikeLists
                        .FirstOrDefaultAsync(l => l.AccountId == userId && l.ProductId == productId);

                    if (existingLike == null)
                    {
                        var newLike = new LikeList
                        {
                            AccountId = userId.Value,
                            ProductId = productId,
                            CreatedAt = DateTime.Now
                        };
                        _context.LikeLists.Add(newLike);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, message = "Đã thêm sản phẩm vào danh sách yêu thích!" });
                    }
                    return Json(new { success = false, message = "Sản phẩm này đã có trong danh sách yêu thích." });
                }
                else
                {
                    var likeSession = HttpContext.Session.GetString("GuestLikeList");
                    var guestLikes = likeSession != null
                        ? JsonConvert.DeserializeObject<List<LikeListViewModel>>(likeSession)
                        : new List<LikeListViewModel>();

                    if (!guestLikes.Any(l => l.ProductId == productId))
                    {
                        guestLikes.Add(new LikeListViewModel
                        {
                            ProductId = productId,
                            ProductName = product.Name,
                            ImageUrl = product.MediaPath,
                            NewPrice = product.NewPrice ?? 0
                        });
                        HttpContext.Session.SetString("GuestLikeList", JsonConvert.SerializeObject(guestLikes));
                        return Json(new { success = true, message = "Đã thêm sản phẩm vào danh sách yêu thích!" });
                    }
                    return Json(new { success = false, message = "Sản phẩm này đã có trong danh sách yêu thích." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thêm vào LikeList: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi thêm sản phẩm." });
            }
        }


        // Xóa sản phẩm khỏi danh sách yêu thích
        [HttpPost]
        public async Task<IActionResult> RemoveFromLikeList(int productId)
        {
            int? userId = GetCurrentUserId();

            if (userId.HasValue)
            {
                // Người dùng đã đăng nhập: Xóa khỏi bảng LikeList
                var likeItem = await _context.LikeLists
                    .FirstOrDefaultAsync(l => l.AccountId == userId && l.ProductId == productId);

                if (likeItem != null)
                {
                    _context.LikeLists.Remove(likeItem);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
                }
            }
            else
            {
                // Khách vãng lai: Xóa khỏi session
                var likeSession = HttpContext.Session.GetString("GuestLikeList");
                var guestLikes = likeSession != null
                    ? JsonConvert.DeserializeObject<List<LikeListViewModel>>(likeSession)
                    : new List<LikeListViewModel>();

                var itemToRemove = guestLikes.FirstOrDefault(l => l.ProductId == productId);
                if (itemToRemove != null)
                {
                    guestLikes.Remove(itemToRemove);
                    HttpContext.Session.SetString("GuestLikeList", JsonConvert.SerializeObject(guestLikes));
                    TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
                }
            }

            return RedirectToAction("Index");
        }
    }

    // ViewModel để hiển thị danh sách yêu thích
    public class LikeListViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal NewPrice { get; set; }
    }
}