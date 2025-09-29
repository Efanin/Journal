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
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Main()
        {
            connection.Open();
            SqlCommand command = new(
                "select * from Human where (Email=@Email)",
                connection
                );
            command.Parameters.AddWithValue("Email", HttpContext.User.Identity.Name);
            SqlDataReader reader = command.ExecuteReader();
            HumanModel humanModel = null;
            
            while (reader.Read())
            {
                byte[] filedb = (byte[])reader["Photo"];
                var stream = new MemoryStream(filedb);
                IFormFile file = new FormFile(stream, 0, filedb.Length, "image", "image.png");
                string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/file", file.FileName);
                using (var stream1 = new FileStream(savePath, FileMode.Create))
                {
                    file.CopyTo(stream1);
                }
                humanModel = new(
                    Convert.ToString(reader["Email"]),
                    Convert.ToString(reader["Name"]),
                    Convert.ToString(reader["Age"]),
                    Convert.ToString(reader["Info"]),
                    "/file/image.png"
                    );
            }
            reader.Close();
            return View(humanModel);
        }
        [HttpPost]
        public async Task<IActionResult> Human(FileModel file)
        {
            
            string Email = HttpContext.User.Identity.Name;
            string Name = "Igor";
            string Age = "26";
            string Info = "super hacker";
            using var memoryStream = new MemoryStream();
            file.FileImage.CopyTo(memoryStream);
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


        public IActionResult HomeWork()
        {
            return View();
        }
        public IActionResult AddWork()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddWorkDB(HomeWorkModel homeWorkModel)
        {
            await Console.Out.WriteLineAsync(homeWorkModel.Info);
            if(homeWorkModel.FileWork.Length < 100 * 1024 * 1024)
            {
                string savePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/file",
                    homeWorkModel.FileWork.FileName);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    homeWorkModel.FileWork.CopyTo(stream);
                }
                using var memoryStream = new MemoryStream();
                homeWorkModel.FileWork.CopyTo(memoryStream);
                byte[] filedb = memoryStream.ToArray();
            }
            connection.Open();
            SqlCommand command = new(
                "insert into HomeWork (Info,NameWork,FileWork) values(@Info,@NameWork,@FileWork)",
                connection
                );
            return RedirectToAction("AddWork", "Home");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
