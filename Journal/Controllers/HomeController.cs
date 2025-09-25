using Journal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;


namespace Journal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SqlConnection connection = new("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Академия\\source\\repos\\Journal\\DataBase\\Database.mdf;Integrated Security=True");
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
            connection.Open();
            SqlCommand command = new(
                "select * from Students where Email=@Email and Password=@Password",
                connection
                );
            command.Parameters.AddWithValue("Email",user.Email);
            command.Parameters.AddWithValue("Password", hashed);
            SqlDataReader reader = command.ExecuteReader();
            bool isLogin = false;
            while (reader.Read()) 
            {
                isLogin = true;
            }
            if (isLogin)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, hashed)
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
                return RedirectToAction("Privacy", "Home");
            }
            return View("Index");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Main()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Human(IFormFile file)
        {
            
            string Email = HttpContext.User.Identity.Name;
            string Name = "Igor";
            string Age = "26";
            string Info = "super hacker";
            using var memoryStream = new MemoryStream();
            file.CopyTo(memoryStream);
            byte[] filedb = memoryStream.ToArray();
            connection.Open();
            SqlCommand command = new(
                "insert into Human (Email,Name,Age,Info,Photo) values(@Email,@Name,@Age,@Info,@Photo)",
                connection
                );
            command.Parameters.AddWithValue("Email", Email);
            command.Parameters.AddWithValue("Name", Name);
            command.Parameters.AddWithValue("Age", Age);
            command.Parameters.AddWithValue("Info", Info);
            command.Parameters.AddWithValue("Photo", filedb);
            command.ExecuteNonQuery();
            return View("Main");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
