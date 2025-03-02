using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class ZaloPayController : Controller
{
    private readonly HttpClient _httpClient;

    // Inject HttpClient qua constructor
    public ZaloPayController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<IActionResult> CreatePayment(int orderId, decimal amount)
    {
        var userId = HttpContext.Session.GetInt32("AccountId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Accounts");
        }

        var appTransId = $"{DateTime.Now:yyMMdd}_{orderId}"; // Mã giao dịch theo ngày

        int amountInt = (int)Math.Round(amount);
        var data = new
        {
            app_id = ZaloPayConfig.AppId,
            app_trans_id = appTransId,
            app_user = userId.ToString(),
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

        // Chuyển đổi JSON phản hồi từ ZaloPay thành object
        var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

        return Content(JsonConvert.SerializeObject(new
        {
            message = "Giao dịch thành công",
            qr_code = responseJson["qr_code"], // Link quét QR
            order_url = responseJson["order_url"], // Link mở trong app ZaloPay
            cashier_order_url = responseJson["cashier_order_url"] // Link hỗ trợ cả thẻ ngân hàng
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
