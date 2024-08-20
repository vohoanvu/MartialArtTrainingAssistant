using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace FighterManager.Server.Helpers
{
    public class RedirectLoginMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectLoginMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Equals("/api/auth/v1/login", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // Forward the request to the new endpoint
                context.Request.Path = "/api/fighter/login";
            }

            await _next(context);
        }
    }

}
