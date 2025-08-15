using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;
using System.Text;

namespace GGLogsApi.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BasicAuthAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _username;
        private readonly string _password;

        public BasicAuthAttribute(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(authHeader) || !AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
            {
                Challenge(context);
                return;
            }

            if (!"Basic".Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                Challenge(context);
                return;
            }

            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter ?? ""));
            var parts = credentials.Split(':', 2);
            if (parts.Length != 2 || parts[0] != _username || parts[1] != _password)
            {
                Challenge(context);
                return;
            }
        }

        private void Challenge(AuthorizationFilterContext context)
        {
            context.HttpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"LogsUI\"";
            context.Result = new UnauthorizedResult();
        }
    }
}
