using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SehirRehberi.API.Data;
using SehirRehberi.API.Dtos;
using SehirRehberi.API.Model;

namespace SehirRehberi.API.Controllers
{
    [Produces("application/json")]
    [Route("api/Auth")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository _auth;
        private readonly IConfiguration _conf;

        public AuthController(IAuthRepository auth, IConfiguration conf)
        {
            _auth = auth;
            _conf = conf;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserForRegisterDto userforRegister)
        {
            if (await _auth.UserExists(userforRegister.Username))
                ModelState.AddModelError("UserName", "Username already exists");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userToCreate = new User
            {
                Username = userforRegister.Username
            };

            var createdUser = await _auth.Register(userToCreate, userforRegister.Password);
            return StatusCode(201, createdUser);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserForLoginDto userforLogin)
        {
            var user = await _auth.Login(userforLogin.Username, userforLogin.Password);
            if ( user == null)
                return Unauthorized();

            var TokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_conf.GetSection("AppSettings:Token").Value);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                    new Claim(ClaimTypes.Name, user.Username)
                }),

                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };

            var token = TokenHandler.CreateToken(tokenDescriptor);
            var tokenString = TokenHandler.WriteToken(token);

            return Ok(tokenString);
        }
    }
}