// File: UsersController.cs   (CHỈ ADMIN)
using Microsoft.AspNetCore.Identity;
namespace FashionStore.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    public UsersController(ApplicationDbContext db, IPasswordHasher<AppUser> hasher) { _db = db; _hasher = hasher; }

    public async Task<IActionResult> Index(string? keyword, int? roleId, int page = 1)
    {
        const int pageSize = 10;
        var q = _db.Users.Include(u => u.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(u => u.FullName.Contains(k) || u.Email.Contains(k) || (u.PhoneNumber != null && u.PhoneNumber.Contains(k)));
        }
        if (roleId.HasValue) q = q.Where(u => u.RoleId == roleId);

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        ViewBag.Paging = new PagingInfo { Page = page, TotalPages = totalPages };
        ViewBag.Roles = await _db.Roles.OrderBy(r => r.Id).ToListAsync();
        ViewBag.Keyword = keyword; ViewBag.RoleId = roleId;
        ViewBag.CurrentUserId = User.GetUserId();
        return View(await q.OrderBy(u => u.RoleId).ThenBy(u => u.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await _db.Roles.OrderBy(r => r.Id).ToListAsync();
        return View(new AdminUserCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserCreateViewModel vm)
    {
        var email = vm.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email)) ModelState.AddModelError(nameof(vm.Email), "Email đã tồn tại");
        if (!await _db.Roles.AnyAsync(r => r.Id == vm.RoleId)) ModelState.AddModelError(nameof(vm.RoleId), "Vai trò không hợp lệ");
        if (!ModelState.IsValid) { ViewBag.Roles = await _db.Roles.OrderBy(r => r.Id).ToListAsync(); return View(vm); }

        var user = new AppUser { Email = email, FullName = vm.FullName.Trim(), PhoneNumber = vm.PhoneNumber, RoleId = vm.RoleId };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã tạo tài khoản";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int id, int roleId)
    {
        if (id == User.GetUserId()) { TempData["Error"] = "Không thể tự đổi vai trò của chính mình"; return RedirectToAction(nameof(Index)); }
        var u = await _db.Users.FindAsync(id);
        if (u == null || !await _db.Roles.AnyAsync(r => r.Id == roleId)) return NotFound();
        u.RoleId = roleId;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã đổi vai trò của {u.FullName} (có hiệu lực khi họ đăng nhập lại)";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        if (id == User.GetUserId()) { TempData["Error"] = "Không thể tự khóa chính mình"; return RedirectToAction(nameof(Index)); }
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.IsActive = !u.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = u.IsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản";
        return RedirectToAction(nameof(Index));
    }
}