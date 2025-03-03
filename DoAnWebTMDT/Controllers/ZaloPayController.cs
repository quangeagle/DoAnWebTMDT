using DoAnWebTMDT.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class ZaloPayController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly WebBanHangTmdtContext _context; // Thêm DbContext

    public ZaloPayController(HttpClient httpClient, WebBanHangTmdtContext context)
    {
        _httpClient = httpClient;
        _context = context; // Khởi tạo DbContext
    }

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> CreatePayment(int orderId, decimal amount)
    {
        // Lấy thông tin đơn hàng
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound("Không tìm thấy đơn hàng.");
        }

        // Nếu đơn hàng có tài khoản, dùng AccountId, nếu không, dùng tên khách vãng lai
        var appUser = order.AccountId.HasValue ? order.AccountId.ToString() : order.GuestFullName ?? "guest";

        // Nếu amount = 0, kiểm tra lại TotalAmount của đơn hàng
        if (amount <= 0)
        {
            amount = order.TotalAmount;
            if (amount <= 0)
            {
                return BadRequest("Số tiền thanh toán không hợp lệ.");
            }
        }

        var appTransId = $"{DateTime.Now:yyMMdd}_{orderId}"; // Mã giao dịch theo ngày

        int amountInt = (int)Math.Round(amount);
        var data = new
        {
            app_id = ZaloPayConfig.AppId,
            app_trans_id = appTransId,
            app_user = appUser,
            app_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            amount = amountInt,
            item = "[]",
            embed_data = "{}",
            bank_code = "",
            description = $"Thanh toán đơn hàng #{orderId}"
        };

        string dataString = $"{ZaloPayConfig.AppId}|{data.app_trans_id}|{data.app_user}|{data.amount}|{data.app_time}|{data.embed_data}|{data.item}";
        string mac = HmacSha256(dataString, ZaloPayConfig.Key1);

        var postData = new
        {
            data.app_id,
            data.app_trans_id,
            data.app_user,
            data.app_time,
            data.amount,
            data.item,
            data.embed_data,
            data.bank_code,
            data.description,
            mac
        };

        var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(ZaloPayConfig.APIUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

        return Content(JsonConvert.SerializeObject(new
        {
            message = "Giao dịch thành công",
            qr_code = responseJson["qr_code"],
            order_url = responseJson["order_url"],
            cashier_order_url = responseJson["cashier_order_url"]
        }), "application/json");
    }


    private static string HmacSha256(string data, string key)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-", "").ToLower();
        }
    }
}
