using DoAnWebTMDT.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
[ApiController] // ✅ Thêm để ASP.NET hiểu là API Controller
[Route("api/[controller]")] // ✅ Chuẩn hóa route
public class ZaloPayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebBanHangTmdtContext _context;
    private readonly string key2 = "kLtgPl8HHhfvMuDHPwKfgfsY4Ydm9eIz"; // Key2 từ ZaloPay

    public ZaloPayController(IHttpClientFactory httpClientFactory, WebBanHangTmdtContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    [HttpGet("create-payment")]
    public async Task<IActionResult> CreatePayment(int orderId, decimal amount)
    {
        Console.WriteLine($"Bắt đầu CreatePayment cho Order ID: {orderId}, Số tiền: {amount}");
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            Console.WriteLine("Không tìm thấy đơn hàng.");
            return NotFound("Không tìm thấy đơn hàng.");
        }

        var appUser = order.AccountId.HasValue ? order.AccountId.ToString() : order.GuestFullName ?? "guest";

        if (amount <= 0)
        {
            amount = order.TotalAmount;
            if (amount <= 0)
            {
                Console.WriteLine("Số tiền thanh toán không hợp lệ.");
                return BadRequest("Số tiền thanh toán không hợp lệ.");
            }
        }

        var appTransId = $"{DateTime.Now:yyMMdd}_{orderId}";
        int amountInt = (int)Math.Round(amount);
        Console.WriteLine($"Tạo giao dịch với app_trans_id: {appTransId}, Amount: {amountInt}");

        var data = new
        {
            app_id = ZaloPayConfig.AppId,
            app_trans_id = appTransId,
            app_user = appUser,
            app_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            amount = amountInt,
            item = "[]",
            embed_data = JsonConvert.SerializeObject(new
            {
                redirecturl = "https://9f09-113-161-95-116.ngrok-free.app/Categories/TrangChu"
            }),
            callback_url = "https://9f09-113-161-95-116.ngrok-free.app/api/ZaloPay/zalo-callback",
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
            data.callback_url,
            mac
        };

        var httpClient = _httpClientFactory.CreateClient();
        var content = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(ZaloPayConfig.APIUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Phản hồi từ ZaloPay: {responseContent}");

        var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);
        if (responseJson["return_code"] != 1)
        {
            return BadRequest("Lỗi khi tạo giao dịch ZaloPay.");
        }

        return Content(JsonConvert.SerializeObject(new
        {
            message = "Giao dịch thành công",
            qr_code = responseJson["qr_code"],
            order_url = responseJson["order_url"],
            cashier_order_url = responseJson["cashier_order_url"]
        }), "application/json");
    }




    [HttpPost("zalo-callback")]


    public async Task<IActionResult> ZaloCallback()
    {
        Console.WriteLine("🔍 Nhận request từ ZaloPay!");

        try
        {
        
            Request.EnableBuffering();
            var body = await new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            Request.Body.Position = 0;

            Console.WriteLine($"📌 Raw Body: {body}");

      
            var cbdata = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
            if (cbdata == null || !cbdata.ContainsKey("data") || !cbdata.ContainsKey("mac"))
            {
                Console.WriteLine("❌ Thiếu data hoặc mac");
                return BadRequest(new { return_code = -1, return_message = "Thiếu data hoặc mac" });
            }

            var dataStr = cbdata["data"].ToString().Trim();
            var reqMac = cbdata["mac"].ToString().Trim();
            Console.WriteLine($"📌 Data nhận được: {dataStr}");
            Console.WriteLine($"📌 MAC từ ZaloPay: {reqMac}");

         
            var computedMac = HmacSha256(dataStr, key2).ToLower().Trim();
            Console.WriteLine($"📌 MAC Tính Toán: {computedMac}");

            if (!string.Equals(reqMac, computedMac, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("❌ MAC không hợp lệ! Kiểm tra lại data truyền vào.");
                return Ok(new { return_code = -1, return_message = "MAC không hợp lệ" });
            }

           
            var dataJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
            Console.WriteLine($"📌 JSON Data Parsed: {JsonConvert.SerializeObject(dataJson, Formatting.Indented)}");

            if (dataJson.ContainsKey("embed_data") && dataJson["embed_data"] is string embedDataStr)
            {
                try
                {
                    var embedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(embedDataStr);
                    dataJson["embed_data"] = embedData;
                    Console.WriteLine($"📌 Parsed embed_data: {JsonConvert.SerializeObject(embedData, Formatting.Indented)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Lỗi parse embed_data: {ex.Message}");
                }
            }

          
            if (!dataJson.ContainsKey("app_trans_id"))
            {
                Console.WriteLine("❌ Thiếu app_trans_id");
                return BadRequest(new { return_code = -1, return_message = "Thiếu app_trans_id" });
            }

            
            int status = 0; 
            if (dataJson.ContainsKey("sub_return_code"))
            {
                int subReturnCode = Convert.ToInt32(dataJson["sub_return_code"]);
                Console.WriteLine($"📌 sub_return_code: {subReturnCode}");

                if (subReturnCode == 1)
                {
                    status = 1; 
                }
                else
                {
                    status = 0; 
                }
            }
            else
            {
                Console.WriteLine("⚠️ Không tìm thấy sub_return_code trong callback, mặc định là 0 (Pending/Canceled)");
            }

         
            var appTransId = dataJson["app_trans_id"].ToString();
            if (!appTransId.Contains("_"))
            {
                Console.WriteLine($"❌ app_trans_id không hợp lệ: {appTransId}");
                return BadRequest(new { return_code = -1, return_message = "app_trans_id không hợp lệ" });
            }

            int orderId = Convert.ToInt32(appTransId.Split('_')[1]);
            Console.WriteLine($"✅ Callback hợp lệ - OrderID: {orderId}, Status: {status}");

           
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                Console.WriteLine($"⚠️ Không tìm thấy đơn hàng: {orderId}");
                return Ok(new { return_code = 0, return_message = "Không tìm thấy đơn hàng" });
            }

            order.OrderStatus = status == 1 ? "Pending" : "Completed";
            await _context.SaveChangesAsync();
            Console.WriteLine($"🔄 Cập nhật đơn hàng {orderId} thành {order.OrderStatus}");

            return Ok(new { return_code = 1, return_message = "Cập nhật trạng thái đơn hàng thành công" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi: {ex.Message}");
            return Ok(new { return_code = 0, return_message = ex.Message });
        }
    }






    private static string HmacSha256(string data, string key)
    {
        data = data.Trim(); // Xóa khoảng trắng trước khi mã hóa
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
                .Replace("-", "")
                .ToLower();
        }
    }

}
