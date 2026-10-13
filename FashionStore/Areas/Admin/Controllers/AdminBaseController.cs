// File: AdminBaseController.cs
namespace FashionStore.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin,Staff")]
public abstract class AdminBaseController : Controller { }