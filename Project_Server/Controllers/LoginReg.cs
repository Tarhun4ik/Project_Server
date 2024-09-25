using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Server.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Project_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ProjectContext _context;

        public AuthController(ProjectContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Регистрация нового пользователя.
        /// </summary>
        /// <param name="model">Модель регистрации, содержащая логин, email и пароль.</param>
        /// <returns>Ответ об успешной регистрации или сообщение об ошибке.</returns>
        /// <response code="200">Регистрация успешна.</response>
        /// <response code="400">Некорректные данные.</response>
        /// <response code="409">Пользователь с таким логином уже существует.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(SuccessResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponse
                {
                    Message = "Некорректные данные.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return Conflict(new ErrorResponse { Message = "Пользователь с таким логином уже существует." });

            if (!IsValidEmail(model.Email))
                return BadRequest(new ErrorResponse { Message = "Неверный формат email." });

            var passwordHash = model.Password;

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                UserPass = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new SuccessResponse { Message = "Регистрация успешна." });
        }

        /// <summary>
        /// Авторизация пользователя.
        /// </summary>
        /// <param name="model">Модель авторизации, содержащая логин и пароль.</param>
        /// <returns>Ответ об успешной авторизации или сообщение об ошибке.</returns>
        /// <response code="200">Авторизация успешна.</response>
        /// <response code="400">Некорректные данные.</response>
        /// <response code="401">Неверный логин или пароль.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(SuccessResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponse
                {
                    Message = "Некорректные данные.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || !VerifyPassword(model.Password, user.UserPass))
                return Unauthorized(new ErrorResponse { Message = "Неверный логин или пароль." });

            return Ok(new SuccessResponse { Message = "Авторизация успешна." });
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hash = password;
            return hash == storedHash;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Логин обязателен.")]
        [MinLength(3, ErrorMessage = "Логин должен содержать как минимум 3 символа.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Неверный формат email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен.")]
        [MinLength(6, ErrorMessage = "Пароль должен содержать как минимум 6 символов.")]
        public string Password { get; set; }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Логин обязателен.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Пароль обязателен.")]
        public string Password { get; set; }
    }


    public class ErrorResponse
    {
        public string Message { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }

    public class SuccessResponse
    {
        public string Message { get; set; }
    }
}
