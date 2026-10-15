using Mapster;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Users;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Areas.Admin.Controllers;

[Route("admin/users")]
public sealed class UsersController(IUserAdminService userAdminService) : AdminController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await userAdminService.ListAsync(cancellationToken));

    [HttpGet("create")]
    public IActionResult Create() => View(new CreateUserForm());

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateUserForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid
            || !await TryExecuteAsync(() => userAdminService.CreateAsync(form.Adapt<CreateUserRequest>(), cancellationToken)))
        {
            return View(form);
        }

        FlashSuccess(string.IsNullOrEmpty(form.Password)
            ? "The user has been created and an invitation e-mail has been sent."
            : "The user has been created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var user = await userAdminService.GetAsync(id, cancellationToken);
        return View(user.Adapt<EditUserForm>());
    }

    [HttpPost("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, EditUserForm form, CancellationToken cancellationToken)
    {
        form.Id = id;
        if (!ModelState.IsValid
            || !await TryExecuteAsync(() => userAdminService.UpdateAsync(id, form.Adapt<UpdateUserRequest>(), cancellationToken)))
        {
            return View(form);
        }

        FlashSuccess("The user has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (await TryExecuteAsync(() => userAdminService.DeleteAsync(id, cancellationToken)))
        {
            FlashSuccess("The user and all of their shares have been deleted.");
        }
        else
        {
            FlashModelErrors();
        }

        return RedirectToAction(nameof(Index));
    }
}
