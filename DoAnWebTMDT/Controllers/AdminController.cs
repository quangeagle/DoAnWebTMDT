using DoAnWebTMDT.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
public class AdminController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }

    private readonly WebBanHangTmdtContext _context;

    public AdminController(WebBanHangTmdtContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> RevenueStatistics()
    {
        // 1️⃣ Lấy tổng doanh thu từ các đơn hàng hoàn thành
        decimal totalRevenue = await _context.Orders
            .Where(o => o.OrderStatus == "Completed")
            .SumAsync(o => o.TotalAmount);

        // 2️⃣ Lấy doanh thu theo từng tháng trong năm hiện tại
        var monthlyRevenue = await _context.Orders
            .Where(o => o.OrderStatus == "Completed" && o.CreatedAt.HasValue)
            .GroupBy(o => new { Year = o.CreatedAt.Value.Year, Month = o.CreatedAt.Value.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        // Chuyển dữ liệu thành danh sách hiển thị
        var labels = monthlyRevenue.Select(m => $"{m.Month}/{m.Year}").ToArray();
        var revenueData = monthlyRevenue.Select(m => m.Revenue).ToArray();

        // 3️⃣ Trả dữ liệu về View
        ViewBag.TotalRevenue = totalRevenue.ToString("N0", new CultureInfo("vi-VN")) + " đ";
        ViewBag.Labels = labels;
        ViewBag.RevenueData = revenueData;

        return View();
    }
}

