using System;
using System.Security.Claims;
using Newtonsoft.Json;
using Twilio.Rest.Chat.V1.Service;

namespace Sisbi.Extensions
{
    public static class IdentityExtensions
    {
        public static Guid Id(this ClaimsPrincipal principal)
        {
            return Guid.Parse(principal.FindFirstValue("id"));
        }
    }
}