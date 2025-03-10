using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;
using DoAnWebTMDT.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DoAnWebTMDT.Controllers
{
    public class AccountsController : Controller
    {
        private readonly WebBanHangTmdtContext _context;

        public AccountsController(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        // GET: Accounts/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Accounts/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra username đã tồn tại chưa
            if (_context.Accounts.Any(a => a.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }

            // Kiểm tra email đã tồn tại chưa
            if (_context.Accounts.Any(a => a.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            // Hash mật khẩu
            string passwordHash = HashPassword(model.Password);

            // Tạo OTP
            string otp = GenerateOTP();
            DateTime otpExpiry = DateTime.Now.AddMinutes(5);

            // Lưu vào DB
            var account = new Account
            {
                Username = model.Username, // 🆕 Gán giá trị Username
                Email = model.Email,
                PasswordHash = passwordHash,
                OTP = otp,
                OTP_Expiry = otpExpiry,
                Role = "User"
            };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // Gửi OTP qua email
            await SendOTPByEmail(model.Email, otp);

            // Chuyển đến trang xác nhận OTP
            return RedirectToAction("VerifyOTP", new { email = model.Email });
        }


        // GET: Accounts/VerifyOTP
        public IActionResult VerifyOTP(string email)
        {
            return View(new VerifyOTPViewModel { Email = email });
        }

        // POST: Accounts/VerifyOTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(VerifyOTPViewModel model)
        {
            var user = _context.Accounts.FirstOrDefault(a => a.Email == model.Email);

            if (user == null || user.OTP != model.OTP || user.OTP_Expiry < DateTime.Now)
            {
                ModelState.AddModelError("OTP", "Mã OTP không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }

            // Xác nhận thành công -> Xóa OTP trong DB
            user.OTP = null;
            user.OTP_Expiry = null;
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET: Accounts/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Accounts/Login

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);



            var user = _context.Accounts.FirstOrDefault(a => a.Email == model.Email);
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Role))
            {
                Console.WriteLine("🔴 Debug: Username hoặc Role bị NULL.");
                ModelState.AddModelError("Email", "Sai email hoặc mật khẩu.");
                return View(model);
            }

            if (!VerifyPassword(model.Password, user.PasswordHash, user.Username))
            {
                Console.WriteLine("🔴 Debug: Mật khẩu nhập vào không đúng.");
                ModelState.AddModelError("Email", "Sai email hoặc mật khẩu.");
                return View(model);
            }




            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("AccountId", user.AccountId.ToString()) // 🆕 Thêm AccountId vào Claims
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        
            HttpContext.Session.SetInt32("AccountId", user.AccountId);

       
            var sessionUserId = HttpContext.Session.GetInt32("AccountId");
            Console.WriteLine($"🔹 Debug: AccountId trong session = {sessionUserId}");

    
            return user.Role == "Admin"
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("CustomerIndex", "Products");
        }


        // Đăng xuất
        public async Task<IActionResult> Logout()
        {
            // ✅ Xóa toàn bộ Session (bao gồm giỏ hàng)
            HttpContext.Session.Clear();

            // ✅ Xóa Cookie Authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // ✅ Điều hướng về trang đăng nhập
            return RedirectToAction("Login", "Accounts");
        }


        // Gửi OTP để reset mật khẩu
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            var user = _context.Accounts.FirstOrDefault(a => a.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "Email không tồn tại.");
                return View();
            }
            Console.WriteLine($"User found: {user.Username}, Role: {user.Role}");
            string otp = GenerateOTP();
            user.OTP = otp;
            user.OTP_Expiry = DateTime.Now.AddMinutes(5);
            await _context.SaveChangesAsync();

            await SendOTPByEmail(user.Email, otp);
            return RedirectToAction("ResetPassword", new { email = user.Email });
        }

        public IActionResult ResetPassword(string email)
        {
            return View(new ResetPasswordViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = _context.Accounts.FirstOrDefault(a => a.Email == model.Email);
            if (user == null || user.OTP != model.OTP || user.OTP_Expiry < DateTime.Now)
            {
                ModelState.AddModelError("OTP", "Mã OTP không hợp lệ hoặc đã hết hạn.");
                return View();
            }

            user.PasswordHash = HashPassword(model.NewPassword);
            user.OTP = null;
            user.OTP_Expiry = null;
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // Tạo OTP 6 chữ số
        private string GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Hash mật khẩu bằng PBKDF2
        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32
            );

            return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        // Kiểm tra mật khẩu
        private bool VerifyPassword(string password, string storedHash, string username)
        {
            // Nếu là tài khoản admin, so sánh trực tiếp không cần hash
            if (username == "admin")
            {
                return password == storedHash;
            }

            if (string.IsNullOrEmpty(storedHash) || !storedHash.Contains('.'))
            {
                return false;
            }

            string[] parts = storedHash.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] hash = Convert.FromBase64String(parts[1]);

                byte[] testHash = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 32
                );

                return hash.SequenceEqual(testHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }


        // Gửi OTP qua Email
        public async Task SendOTPByEmail(string email, string otp)
        {
            try
            {
                var fromAddress = new MailAddress("haoquang16122004@gmail.com", "Tên hiển thị");
                var toAddress = new MailAddress(email);
                const string fromPassword = "rzml efui uzqy neik"; // Dùng App Password thay vì mật khẩu Gmail
                const string subject = "Mã OTP xác nhận tài khoản";
                string body = $"Mã OTP của bạn là: {otp}. Mã này có hiệu lực trong 5 phút.";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com", // Gmail SMTP Server
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                throw;
            }
        }
    }
}