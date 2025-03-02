using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnWebTMDT.Data;

namespace DoAnWebTMDT.Controllers
{
    public class AddressesController : Controller
    {
        private readonly WebBanHangTmdtContext _context;

        public AddressesController(WebBanHangTmdtContext context)
        {
            _context = context;
        }

        // GET: Addresses
        public async Task<IActionResult> Index()
        {
            var addresses = _context.Addresses.Include(a => a.Account);
            return View(await addresses.ToListAsync());
        }

        // GET: Addresses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var address = await _context.Addresses
                .Include(a => a.Account)
                .FirstOrDefaultAsync(m => m.AddressId == id);

            if (address == null)
                return NotFound();

            return View(address);
        }

        // GET: Addresses/Create
        public IActionResult Create()
        {
            ViewData["AccountId"] = new SelectList(_context.Accounts, "AccountId", "Username");
            return View();
        }

        // POST: Addresses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AddressId,AccountId,FullName,Phone,AddressDetail,IsDefault")] Address address)
        {
            if (ModelState.IsValid)
            {
                if (address.IsDefault == true)
                {
                    // Đặt tất cả địa chỉ khác của tài khoản thành false
                    var existingAddresses = _context.Addresses.Where(a => a.AccountId == address.AccountId);
                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                _context.Add(address);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["AccountId"] = new SelectList(_context.Accounts, "AccountId", "Username", address.AccountId);
            return View(address);
        }

        // GET: Addresses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
                return NotFound();

            ViewData["AccountId"] = new SelectList(_context.Accounts, "AccountId", "Username", address.AccountId);
            return View(address);
        }

        // POST: Addresses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AddressId,AccountId,FullName,Phone,AddressDetail,IsDefault")] Address address)
        {
            if (id != address.AddressId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (address.IsDefault == true)
                    {
                        var existingAddresses = _context.Addresses.Where(a => a.AccountId == address.AccountId);
                        foreach (var addr in existingAddresses)
                        {
                            addr.IsDefault = false;
                        }
                    }

                    _context.Update(address);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AddressExists(address.AddressId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["AccountId"] = new SelectList(_context.Accounts, "AccountId", "Username", address.AccountId);
            return View(address);
        }

        // GET: Addresses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var address = await _context.Addresses
                .Include(a => a.Account)
                .FirstOrDefaultAsync(m => m.AddressId == id);

            if (address == null)
                return NotFound();

            // Kiểm tra xem địa chỉ có được dùng trong đơn hàng không
            bool isUsedInOrders = _context.Orders.Any(o => o.AddressId == id);
            ViewBag.IsUsedInOrders = isUsedInOrders;

            return View(address);
        }

        // POST: Addresses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address != null)
            {
                bool isUsedInOrders = _context.Orders.Any(o => o.AddressId == id);
                if (isUsedInOrders)
                {
                    ModelState.AddModelError("", "Không thể xóa địa chỉ vì nó đang được sử dụng trong đơn hàng.");
                    return View(address);
                }

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AddressExists(int id)
        {
            return _context.Addresses.Any(e => e.AddressId == id);
        }
    }
}
