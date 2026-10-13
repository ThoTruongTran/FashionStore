// File: Controllers/AccountController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
namespace FashionStore.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly IPasswordResetService _reset;
    private readonly IEmailService _email;
    private readonly IWebHostEnvironment _env;

    public AccountController(ApplicationDbContext db, IPasswordHasher<AppUser> hasher,
        IPasswordResetService reset, IEmailService email, IWebHostEnvironment env)
    { _db = db; _hasher = hasher; _reset = reset; _email = email; _env = env; }

    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
            return View(vm);
        }
        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa");
            return View(vm);
        }
        await SignInAsync(user, vm.RememberMe);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return user.Role!.Name is "Admin" or "Staff"
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index", "Home");
    }

    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Email này đã được đăng ký");
            return View(vm);
        }
        var role = await _db.Roles.FirstAsync(r => r.Name == "Customer");
        var user = new AppUser { Email = email, FullName = vm.FullName.Trim(), PhoneNumber = vm.PhoneNumber, RoleId = role.Id };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        user.Role = role;
        await SignInAsync(user, false);
        TempData["Success"] = "Đăng ký thành công, chào mừng bạn đến với FashionStore!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var u = await _db.Users.FindAsync(User.GetUserId());
        return View(new ProfileViewModel { FullName = u!.FullName, PhoneNumber = u.PhoneNumber, Gender = u.Gender, DateOfBirth = u.DateOfBirth });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _db.Users.Include(u => u.Role).FirstAsync(u => u.Id == User.GetUserId());
        user.FullName = vm.FullName.Trim();
        user.PhoneNumber = vm.PhoneNumber;
        user.Gender = vm.Gender;
        user.DateOfBirth = vm.DateOfBirth;
        await _db.SaveChangesAsync();

        var auth = await HttpContext.AuthenticateAsync();
        await SignInAsync(user, auth.Properties?.IsPersistent ?? false);   // cập nhật tên trên navbar
        TempData["Success"] = "Đã cập nhật thông tin";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, HttpGet] public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (_hasher.VerifyHashedPassword(user!, user!.PasswordHash, vm.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Mật khẩu hiện tại không đúng");
            return View(vm);
        }
        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đổi mật khẩu thành công";
        return RedirectToAction(nameof(Profile));
    }

    private async Task SignInAsync(AppUser user, bool remember)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role!.Name)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = remember });
    }

    [HttpGet] public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user != null)
        {
            var token = _reset.CreateToken(user);
            var link = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme)!;
            var body = $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>" +
                       $"<p>Bấm vào liên kết sau để đặt lại mật khẩu (có hiệu lực 30 phút):</p><p><a href=\"{link}\">{link}</a></p>" +
                       "<p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";
            var sent = await _email.SendAsync(user.Email, "Đặt lại mật khẩu FashionStore", body);
            if (!sent && _env.IsDevelopment()) TempData["DevLink"] = link;   // chỉ khi dev và chưa cấu hình SMTP
        }
        // Luôn trả lời giống nhau để không lộ email nào đã đăng ký
        TempData["Success"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (await _reset.ValidateAsync(token ?? "") == null)
        {
            TempData["Error"] = "Liên kết không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu lại.";
            return RedirectToAction(nameof(ForgotPassword));
        }
        return View(new ResetPasswordViewModel { Token = token! });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _reset.ValidateAsync(vm.Token);
        if (user == null)
        {
            TempData["Error"] = "Liên kết không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu lại.";
            return RedirectToAction(nameof(ForgotPassword));
        }
        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);   // token cũ tự vô hiệu
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đặt lại mật khẩu thành công, hãy đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
    }
}