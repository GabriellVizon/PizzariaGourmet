using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[ValidateAntiForgeryToken]
public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Preencha todos os campos.";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Email, Password, true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return RedirectToPage("/Admin/Index");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "Conta temporariamente bloqueada por muitas tentativas. Tente novamente em 15 minutos.";
            return Page();
        }

        ErrorMessage = "Email ou senha inválidos.";
        return Page();
    }
}
