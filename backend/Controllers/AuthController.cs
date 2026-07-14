using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AIChatBoxBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // In-memory user store for demo purposes
        private static readonly Dictionary<string, string> Users = new();
        private const string DummyJwt = "dummy-jwt-token"; // Replace with real JWT generation in production

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Invalid credentials" });

            if (Users.TryGetValue(request.Username, out var storedPwd) && storedPwd == request.Password)
            {
                return Ok(new { token = DummyJwt });
            }
            return Unauthorized(new { message = "Invalid username or password" });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Invalid data" });

            if (Users.ContainsKey(request.Username))
                return Conflict(new { message = "User already exists" });

            Users[request.Username] = request.Password;
            return Ok(new { message = "Registration successful" });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
