using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Web.Controllers;
using SharingSystem.Web.Infrastructure;

namespace SharingSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = WebConstants.AdminPolicy)]
public abstract class AdminController : AppController;
