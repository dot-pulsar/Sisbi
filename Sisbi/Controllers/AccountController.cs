using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Models;
using Models.Entities;
using Models.Requests;
using Newtonsoft.Json;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using Sisbi.Extensions;
using Sisbi.Services;
using Sisbi.Services.Contracts;

namespace Sisbi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly VkontakteService _vkontakteService;
        private readonly SisbiContext _context;

        public AccountController(IEmailService emailService, VkontakteService vkontakteService, SisbiContext context)
        {
            _emailService = emailService;
            _vkontakteService = vkontakteService;
            _context = context;
        }

        #region Default

        public async Task<User> GetUserAsync(string login, LoginType loginType)
        {
            if (loginType == LoginType.Phone)
            {
                return await _context.Users.SingleOrDefaultAsync(u => u.Phone == login);
            }

            return await _context.Users.SingleOrDefaultAsync(u => u.Email == login);
        }

        [AllowAnonymous, HttpPost("otp/send")]
        public async Task<IActionResult> SendCodeAsync(SendOtpRequest model)
        {
            var loginType = GetLoginType(model.Login);

            if (loginType == LoginType.BadLogin)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'login' field is not in the correct format."
                });
            }

            if (model.Type == OtpType.BadType)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'type' field is required."
                });
            }

            var user = await GetUserAsync(model.Login, loginType);

            var otp = GenerateOpt();
            var now = DateTime.UtcNow.ToUnixTime();
            var waitingTime = 60;

            if (user == null)
            {
                if (loginType == LoginType.Phone)
                {
                    user = new User
                    {
                        Phone = model.Login,
                        Otp = otp,
                        OtpDate = now,
                        OtpRetry = 0,
                        OtpType = model.Type
                    };
                }
                else
                {
                    user = new User
                    {
                        Email = model.Login,
                        Otp = otp,
                        OtpDate = now,
                        OtpRetry = 0,
                        OtpType = model.Type
                    };
                }

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                await SendOtp(otp, model.Login, loginType);

                return Ok(new
                {
                    success = true,
                    next_retry_timestamp = now + waitingTime
                });
            }

            //TODO: Проверка на тип otp

            if (user.OtpDate + waitingTime <= now)
            {
                user.Otp = otp;
                user.OtpDate = now;
                user.OtpRetry = ++user.OtpRetry;
                user.OtpType = model.Type;

                await SendOtp(otp, model.Login, loginType);

                return Ok(new
                {
                    success = true,
                    next_retry_timestamp = now + waitingTime
                });
            }

            return BadRequest(new
            {
                success = false,
                next_retry_timestamp = user.OtpDate + waitingTime
            });
        }

        [AllowAnonymous, HttpPost("otp/confirm")]
        public async Task<IActionResult> ConfirmCodeAsync(ConfirmOtpRequest model)
        {
            var loginType = GetLoginType(model.Login);

            if (loginType == LoginType.BadLogin)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'login' field is not in the correct format."
                });
            }

            if (model.Otp == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'otp' field is required."
                });
            }

            var user = await GetUserAsync(model.Login, loginType);

            if (user == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "No user with this login was found."
                });
            }

            if (user.Otp == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Invalid one-time password."
                });
            }

            if (user.Otp == model.Otp)
            {
                if (loginType == LoginType.Phone)
                {
                    user.PhoneConfirmed = true;
                    user.Otp = null;
                    user.OtpDate = null;
                    user.OtpRetry = null;
                }
                else
                {
                    user.EmailConfirmed = true;
                    user.Otp = null;
                    user.OtpDate = null;
                    user.OtpRetry = null;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true
                });
            }

            return BadRequest(new
            {
                success = false,
                description = "Invalid one-time password."
            });
        }

        [AllowAnonymous, HttpPost("signup")]
        public async Task<IActionResult> SingUpAsync(SignUpRequest model)
        {
            var loginType = GetLoginType(model.Login);

            if (loginType == LoginType.BadLogin)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'login' field is not in the correct format."
                });
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'password' field is not in the correct format."
                });
            }

            if (model.Role == Role.BadRole)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'role' field is required."
                });
            }

            var user = await GetUserAsync(model.Login, loginType);

            if (user == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Account with this login is not confirmed."
                });
            }

            if (user.OtpType != OtpType.SignUp)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Otp type mismatch detected."
                });
            }

            if (!user.AlreadyRegistered)
            {
                var salt = GenerateSalt();
                var hash = await Sha512Async(model.Password, salt);
                var now = DateTime.UtcNow.ToUnixTime();

                if (loginType == LoginType.Phone)
                {
                    user.PhoneConfirmed = true;
                    user.Password = hash;
                    user.Salt = salt;
                    user.Role = model.Role;
                    user.RegistrationDate = now;
                }
                else
                {
                    user.EmailConfirmed = true;
                    user.Password = hash;
                    user.Salt = salt;
                    user.Role = model.Role;
                    user.RegistrationDate = now;
                }

                await _context.SaveChangesAsync();

                var refreshToken = new RefreshToken
                {
                    Token = GenerateRefreshToken(),
                    ExpireIn = DateTime.UtcNow.AddDays(30).ToUnixTime(),
                    UserId = user.Id
                };

                await _context.RefreshTokens.AddAsync(refreshToken);

                return Ok(new
                {
                    success = true,
                    access_token = GenerateJwt(user.Id, model.Role),
                    refresh_token = refreshToken.Token,
                    expires_in = now + 40
                });
            }

            return BadRequest(new
            {
                success = false,
                description = "An account with this login has already been created."
            });
        }

        [AllowAnonymous, HttpPost("signin")]
        public async Task<IActionResult> SingInAsync(SignInRequest model)
        {
            var loginType = GetLoginType(model.Login);

            if (loginType == LoginType.BadLogin)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'login' field is not in the correct format."
                });
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'password' field is not in the correct format."
                });
            }

            var user = await GetUserAsync(model.Login, loginType);

            if (user == null || await Sha512Async(model.Password, user.Salt) != user.Password)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Wrong login or password."
                });
            }

            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                ExpireIn = DateTime.UtcNow.AddDays(30).ToUnixTime(),
                UserId = user.Id
            };

            var now = DateTime.UtcNow.ToUnixTime();
            var expireTokens = _context.RefreshTokens.Where(rt => now > rt.ExpireIn);

            _context.RemoveRange(expireTokens);

            await _context.RefreshTokens.AddAsync(refreshToken);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                access_token = GenerateJwt(user.Id, user.Role),
                refresh_token = refreshToken.Token,
                expires_in = DateTime.UtcNow.ToUnixTime() + 40
            });
        }

        [Authorize, HttpPost("password/change")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest model)
        {
            var loginType = GetLoginType(model.Login);

            if (loginType == LoginType.BadLogin)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'login' field is not in the correct format."
                });
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'password' field is not in the correct format."
                });
            }

            if (string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'new_password' field is not in the correct format."
                });
            }

            if (model.Password == model.NewPassword)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "'password' and 'new_password' must not be equal."
                });
            }

            var user = await GetUserAsync(model.Login, loginType);

            if (user == null || await Sha512Async(model.Password, user.Salt) != user.Password)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Wrong login or password."
                });
            }

            var salt = GenerateSalt();
            var newPassword = await Sha512Async(model.NewPassword, salt);

            user.Password = newPassword;
            user.Salt = salt;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        [AllowAnonymous, HttpPost("password/restore")]
        public async Task<IActionResult> RestorePassword(RestorePasswordRequest model)
        {
            var loginType = GetLoginType(model.Login);

            if (loginType == LoginType.BadLogin)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'login' field is not in the correct format."
                });
            }

            /*if (string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'password' field is not in the correct format."
                });
            }*/

            var user = await GetUserAsync(model.Login, loginType);

            if (user == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Account not found."
                });
            }

            var now = DateTime.UtcNow.ToUnixTime();

            return Ok(new
            {
                success = true,
                access_token = GenerateJwt(user.Id, user.Role),
                expires_in = now + 40
            });
        }

        #endregion

        [NonAction]
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        [NonAction]
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("superSecretKey@345"));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience =
                    false, //you might want to validate the audience and issuer depending on your use case
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = secretKey,
                ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal result;
            try
            {
                result = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                    return null;
            }
            catch
            {
                return null;
            }


            return result;
        }

        [AllowAnonymous, HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return BadRequest("неверный id");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok();
        }
        
        [AllowAnonymous, HttpPost("refresh_token")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest body)
        {
            var principal = GetPrincipalFromExpiredToken(body.Token);

            var userId = Guid.Empty;

            if (principal == null ||
                !principal.HasClaim(claim =>
                    claim.Type == "id"
                    && Guid.TryParse(claim.Value, out userId)))
            {
                return BadRequest();
            }

            if (userId != null)
            {
                var refreshToken =
                    await _context.RefreshTokens.Where(r =>
                        r.User.Id == userId && r.Token == body.RefreshToken).FirstOrDefaultAsync();

                if (body.RefreshToken != null
                    && refreshToken != null
                    && refreshToken.Token == body.RefreshToken
                    && refreshToken.ExpireIn < DateTime.UtcNow.ToUnixTime())
                {
                    var newRefreshToken = new RefreshToken
                    {
                        Token = GenerateRefreshToken(),
                        ExpireIn = DateTime.UtcNow.AddDays(30).ToUnixTime(),
                        UserId = refreshToken.UserId
                    };

                    await _context.RefreshTokens.AddAsync(newRefreshToken);

                    _context.RefreshTokens.Remove(refreshToken);

                    await _context.SaveChangesAsync();

                    return Ok();
                }
            }


            return BadRequest();
        }

        public class RefreshTokenRequest
        {
            [JsonProperty("token")] public string Token { get; set; }
            [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
        }

        #region Other

        [NonAction]
        private static int GenerateOpt() => new Random().Next(100000, 999999);

        [NonAction]
        private static LoginType GetLoginType(string login)
        {
            const string phoneRegex = @"^\+[0-9]{11}";
            const string emailRegex = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";

            if (string.IsNullOrEmpty(login))
            {
                return LoginType.BadLogin;
            }

            if (Regex.IsMatch(login, phoneRegex))
            {
                return LoginType.Phone;
            }

            if (Regex.IsMatch(login, emailRegex))
            {
                return LoginType.Email;
            }

            return LoginType.BadLogin;
        }

        [NonAction]
        private static string GenerateJwt(Guid id, Role role)
        {
            var now = DateTime.UtcNow;
            var identity = GetIdentity(id, role);

            var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("superSecretKey@345"));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: "http://localhost:5000",
                audience: "http://localhost:5000",
                notBefore: now,
                expires: now.Add(TimeSpan.FromHours(40)),
                claims: identity.Claims,
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        [NonAction]
        private static ClaimsIdentity GetIdentity(Guid id, Role role)
        {
            var claims = new List<Claim>
            {
                new("id", id.ToString()),
                new(ClaimsIdentity.DefaultRoleClaimType, role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                "Token",
                ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }

        [NonAction]
        private async Task SendOtp(int otp, string login, LoginType loginType)
        {
            if (loginType == LoginType.Phone)
            {
                await MessageResource.CreateAsync(
                    body: $"Ваш код: {otp}",
                    from: new PhoneNumber("+14243690280"),
                    to: new PhoneNumber(login)
                );
            }
            else
            {
                var request = new EmailRequest
                {
                    ToEmail = login,
                    Subject = "Код подтверждения",
                    Body = $"Ваш код подтверждения: <b>{otp}</b>"
                };

                await _emailService.SendEmailAsync(request);
            }
        }

        [NonAction]
        private static async Task<string> Sha512Async(string password, string salt)
        {
            var data = Encoding.UTF8.GetBytes(password + salt);

            await using (var ms = new MemoryStream(data))
            {
                using (var shaM = new SHA512Managed())
                {
                    var hash = await shaM.ComputeHashAsync(ms);
                    return HashToString(hash);
                }
            }
        }

        [NonAction]
        private static string GenerateSalt()
        {
            var salt = new byte[32];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }

        [NonAction]
        private static string GenerateSalt(int length)
        {
            var salt = new byte[length];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }

        [NonAction]
        private static string HashToString(byte[] hash)
        {
            return BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLower();
        }

        #endregion
    }
}