using Microsoft.AspNetCore.Mvc;
using DoAnWebTMDT.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DoAnWebTMDT.Data;

public class AuthController : Controller
{
    private readonly WebBanHangTmdtContext _context;
    private readonly EmailService _emailService;

    public AuthController(WebBanHangTmdtContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    private string GenerateOTP()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    [HttpPost]
    public async Task<IActionResult> Register(Account model)
    {
        if (_context.Accounts.Any(a => a.Email == model.Email))
        {
            ViewBag.Error = "Email đã được đăng ký!";
            return View();
        }

        string otp = GenerateOTP();
        model.OTP = otp;
        model.OTP_Expiry = DateTime.Now.AddMinutes(5); // Hết hạn sau 5 phút

        _context.Accounts.Add(model);
        await _context.SaveChangesAsync();

        await _emailService.SendEmailAsync(model.Email, "Xác nhận đăng ký", $"Mã OTP của bạn là: {otp}");

        return RedirectToAction("VerifyOTP", new { email = model.Email });
    }

    public IActionResult VerifyOTP(string email)
    {
        ViewBag.Email = email;
        return View();
    }

    [HttpPost]
    public IActionResult VerifyOTP(string email, string otp)
    {
        var user = _context.Accounts.FirstOrDefault(a => a.Email == email && a.OTP == otp);

        if (user == null || user.OTP_Expiry < DateTime.Now)
        {
            ViewBag.Error = "OTP không hợp lệ hoặc đã hết hạn!";
            return View();
        }

        user.OTP = null; // Xóa OTP sau khi xác nhận
        _context.SaveChanges();

        return RedirectToAction("Login");
    }
}
