using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Models;
using Models.Entities;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using Sisbi.Extensions;
using Sisbi.Services.Contracts;

namespace Sisbi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public AccountController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send_otp")]
        public async Task<IActionResult> OptAsync(OtpRequest model)
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

            if (model.Role == Role.BadRole)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "The 'role' field is required."
                });
            }

            var user = await SisbiContext.GetUserAsync(model.Login, loginType);

            var otp = GenerateOpt();
            var now = DateTime.UtcNow.ToUnixTime();
            var waitingTime = 60;

            if (user == null)
            {
                if (loginType == LoginType.Phone)
                {
                    await SisbiContext.CreateAsync<User>(new
                    {
                        phone = model.Login,
                        role = model.Role,
                        otp,
                        otp_date = now,
                        otp_retry = 0
                    });
                }
                else
                {
                    await SisbiContext.CreateAsync<User>(new
                    {
                        email = model.Login,
                        role = model.Role,
                        otp,
                        otp_date = now,
                        otp_retry = 0
                    });
                }

                await SendOtp(otp, model.Login, loginType);

                return Ok(new
                {
                    success = true,
                    next_retry_timestamp = now + waitingTime
                });
            }

            if (user.otp_date + waitingTime <= now)
            {
                if (user.AlreadyRegistered)
                {
                    await SisbiContext.UpdateUserAsync(model.Login, loginType, new
                    {
                        otp,
                        otp_date = now,
                        otp_retry = ++user.otp_retry
                    });
                }
                else
                {
                    await SisbiContext.UpdateUserAsync(model.Login, loginType, new
                    {
                        role = model.Role,
                        otp,
                        otp_date = now,
                        otp_retry = ++user.otp_retry
                    });
                }

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
                next_retry_timestamp = user.otp_date + waitingTime
            });
        }

        [HttpPost("confirm_otp")]
        public async Task<IActionResult> ConfirmCodeAsync(ConfirmOtp model)
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

            var user = await SisbiContext.GetUserAsync(model.Login, loginType);

            if (user == null)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "No user with this login was found."
                });
            }

            if (user.otp == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Invalid one-time password."
                });
            }

            if (user.otp == model.Otp)
            {
                if (loginType == LoginType.Phone)
                {
                    await SisbiContext.UpdateUserAsync(model.Login, loginType, new
                    {
                        phone_confirmed = true,
                        otp = "NULL",
                        otp_date = "NULL",
                        otp_retry = "NULL"
                    });
                }
                else
                {
                    await SisbiContext.UpdateUserAsync(model.Login, loginType, new
                    {
                        email_confirmed = true,
                        otp = "NULL",
                        otp_date = "NULL",
                        otp_retry = "NULL"
                    });
                }

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

        [HttpPost("signup")]
        public async Task<IActionResult> SingUpAsync(AuthData model)
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

            var user = await SisbiContext.GetUserAsync(model.Login, loginType);

            if (user == null || !user.phone_confirmed && !user.email_confirmed)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Account with this login is not confirmed."
                });
            }

            if (!user.AlreadyRegistered)
            {
                var salt = GenerateSalt();
                var hash = await Sha512Async(model.Password, salt);
                var now = DateTime.UtcNow.ToUnixTime();

                if (loginType == LoginType.Phone)
                {
                    await SisbiContext.UpdateUserAsync(model.Login, loginType, new
                    {
                        phone_confirmed = true,
                        password = hash,
                        salt,
                        registration_date = now
                    });
                }
                else
                {
                    await SisbiContext.UpdateUserAsync(model.Login, loginType, new
                    {
                        email_confirmed = true,
                        password = hash,
                        salt,
                        registration_date = now
                    });
                }

                return Ok(new
                {
                    success = true,
                    access_token = GenerateJwt(model.Login, user.role),
                    expires_in = now + 40
                });
            }

            return BadRequest(new
            {
                success = false,
                description = "An account with this login has already been created."
            });
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SingInAsync(AuthData model)
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

            var user = await SisbiContext.GetUserAsync(model.Login, loginType);

            if (user == null || await Sha512Async(model.Password, user.salt) != user.password)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Wrong login or password."
                });
            }

            return Ok(new
            {
                success = true,
                access_token = GenerateJwt(model.Login, user.role),
                expires_in = DateTime.UtcNow.ToUnixTime() + 40
            });
        }

        [HttpPost("password/change")]
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

            var user = await SisbiContext.GetUserAsync(model.Login, loginType);

            if (user == null || await Sha512Async(model.Password, user.salt) != user.password)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Wrong login or password."
                });
            }

            var salt = GenerateSalt();
            var newPassword = await Sha512Async(model.NewPassword, salt);

            await SisbiContext.UpdateAsync<User>(user.id, new
            {
                password = newPassword,
                salt
            });

            return Ok(new
            {
                success = true
            });
        }

        /*[Authorize, HttpPost("password/restore")]
        public async Task<IActionResult> RestorePassword(AuthData model)
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

            var user = await SisbiContext.GetUserAsync(model.Login, loginType);

            if (user == null || await Sha512Async(model.Password, user.salt) != user.password)
            {
                return BadRequest(new
                {
                    success = false,
                    description = "Wrong login or password."
                });
            }

            return Ok(new
            {
                success = true,
                access_token = GenerateJwt(model.Login, user.role),
                expires_in = DateTime.UtcNow.ToUnixTime() + 40
            });
        }*/

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
        private static string GenerateJwt(string login, Role role)
        {
            var now = DateTime.UtcNow;
            var identity = GetIdentity(login, role);

            var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("superSecretKey@345"));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: "http://localhost:5000",
                audience: "http://localhost:5000",
                notBefore: now,
                expires: now.Add(TimeSpan.FromSeconds(40)),
                claims: identity.Claims,
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        [NonAction]
        private static ClaimsIdentity GetIdentity(string login, Role role)
        {
            var claims = new List<Claim>
            {
                new(ClaimsIdentity.DefaultNameClaimType, login),
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
                    body: $"?????? ??????: {otp}",
                    from: new PhoneNumber("+14243690280"),
                    to: new PhoneNumber(login)
                );
            }
            else
            {
                var request = new EmailRequest
                {
                    ToEmail = login,
                    Subject = "?????? ??????????????????????????",
                    Body = $"?????? ?????? ??????????????????????????: <b>{otp}</b>"
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