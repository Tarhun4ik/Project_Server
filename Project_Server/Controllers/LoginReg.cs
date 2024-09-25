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
        /// ����������� ������ ������������.
        /// </summary>
        /// <param name="model">������ �����������, ���������� �����, email � ������.</param>
        /// <returns>����� �� �������� ����������� ��� ��������� �� ������.</returns>
        /// <response code="200">����������� �������.</response>
        /// <response code="400">������������ ������.</response>
        /// <response code="409">������������ � ����� ������� ��� ����������.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(SuccessResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponse
                {
                    Message = "������������ ������.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return Conflict(new ErrorResponse { Message = "������������ � ����� ������� ��� ����������." });

            if (!IsValidEmail(model.Email))
                return BadRequest(new ErrorResponse { Message = "�������� ������ email." });

            var passwordHash = model.Password;

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                UserPass = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new SuccessResponse { Message = "����������� �������." });
        }

        /// <summary>
        /// ����������� ������������.
        /// </summary>
        /// <param name="model">������ �����������, ���������� ����� � ������.</param>
        /// <returns>����� �� �������� ����������� ��� ��������� �� ������.</returns>
        /// <response code="200">����������� �������.</response>
        /// <response code="400">������������ ������.</response>
        /// <response code="401">�������� ����� ��� ������.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(SuccessResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ErrorResponse
                {
                    Message = "������������ ������.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || !VerifyPassword(model.Password, user.UserPass))
                return Unauthorized(new ErrorResponse { Message = "�������� ����� ��� ������." });

            return Ok(new SuccessResponse { Message = "����������� �������." });
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
        [Required(ErrorMessage = "����� ����������.")]
        [MinLength(3, ErrorMessage = "����� ������ ��������� ��� ������� 3 �������.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email ����������.")]
        [EmailAddress(ErrorMessage = "�������� ������ email.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "������ ����������.")]
        [MinLength(6, ErrorMessage = "������ ������ ��������� ��� ������� 6 ��������.")]
        public string Password { get; set; }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "����� ����������.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "������ ����������.")]
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
