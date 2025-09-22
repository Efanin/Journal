using Journal.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;
using Microsoft.Data.SqlClient;


namespace Journal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SqlConnection connection = new("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\nadyb\\source\\repos\\Journal\\DataBase\\Database.mdf;Integrated Security=True");
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserModel user)
        {
            await Console.Out.WriteLineAsync(user.Email);
            await Console.Out.WriteLineAsync(user.Password);
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
              password: user.Password,
              salt: Encoding.Unicode.GetBytes("CGYzxeN4plZekNC14Umm1Q22"),
              prf: KeyDerivationPrf.HMACSHA256,
              iterationCount: 100000,
              numBytesRequested: 256 / 8));
            await Console.Out.WriteLineAsync(hashed);

            SqlCommand command = new(
                "select * from Students where Email=@Email and Password=@Password",
                connection
                );
            command.Parameters.AddWithValue("Email",user.Email);
            command.Parameters.AddWithValue("Password", user.Password);

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Password)
                };
            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = user.RememberMe,
                ExpiresUtc = user.RememberMe ?
                    DateTimeOffset.UtcNow.AddDays(7) :
                    DateTimeOffset.UtcNow.AddMinutes(60)
            };
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
