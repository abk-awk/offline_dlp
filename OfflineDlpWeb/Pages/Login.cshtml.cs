using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace OfflineDlpWeb.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _config;

        public LoginModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var expectedUser = _config["Auth:Username"];
            var expectedPass = _config["Auth:Password"];

            if (Username == expectedUser && Password == expectedPass)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Username)
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Cookies", principal);

                return RedirectToPage("/Index");
            }

            ErrorMessage = "Identifiants invalides.";
            return Page();
        }
    }
}
