﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace VideoSharing.Server.Helpers
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class JWTInvalidateFilter : IAuthorizationFilter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public JWTInvalidateFilter()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void OnAuthorization(AuthorizationFilterContext context)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var anonymous = context.ActionDescriptor.EndpointMetadata
                   .Where(x => x.GetType() == typeof(AllowAnonymousAttribute))
                   .ToList();
            if (anonymous.Count > 0)
            {
                return;
            }
            var ctx = context.HttpContext;
            ctx.Request.Headers.TryGetValue("Authorization", out var token);
            var jwt = token.ToString().Replace("Bearer ", "");

            if (IsJwtTokenExpired(context))
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.Unauthorized);
                return;    
            }
        
            var userId = int.Parse(context.HttpContext.User.Claims.FirstOrDefault(predicate: x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    
        private bool IsJwtTokenExpired(AuthorizationFilterContext context)
        {
            var expClaim = context.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == "exp");
            if (expClaim == null || !long.TryParse(expClaim.Value, out long expUnixTime))
            {
                return true;
            }
            var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;
            var now = DateTime.UtcNow;
            return now >= expDateTime;
        }

    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class BaseFilter : IActionFilter, IOrderedFilter
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public int Order { get; } = int.MinValue;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        private readonly ILogger<BaseFilter> _logger;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public BaseFilter(ILogger<BaseFilter> logger)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            _logger = logger;
        }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void OnActionExecuted(ActionExecutedContext context)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (context.Result is Response<object> response)
            {
                context.Result = new ObjectResult(response.Body)
                {
                    StatusCode = (int)response.StatusCode,
                };
                return;
            }
            if (context.Exception is ErrorResponseException errException)
            {
                _logger.LogError(errException, errException.Message);
                var e = errException.ToErrorResponse();
                context.Result = new ObjectResult(e)
                {
                    StatusCode = (int)errException.StatusCode,
                };
                context.ExceptionHandled = true;
                return;
            }
            if (context.Exception is Exception exception)
            {
                _logger.LogError(exception, "");
                var e = ErrorResponseException.Create().SetErrorCode(500).SetMessage(message: "There seems to be a problem connecting with our server. Rest assured we are looking into it. Please try again later.").SetType("error.internal.server").SetStatusCode(HttpStatusCode.InternalServerError).SetBody(exception?.StackTrace?.Split("\n")?.Append(exception.Message)).ToErrorResponse();
                context.Result = new ObjectResult(e)
                {
                    StatusCode = 500,
                };
                context.ExceptionHandled = true;
                return;
            }
            if (context.Result is ObjectResult result)
            {
                if (result.Value.GetType().IsGenericType && result.Value.GetType()
                    .GetGenericTypeDefinition() == typeof(Response<>))
                {
                    var valueType = result.Value.GetType();
                    result.StatusCode = (int) valueType.GetProperty("StatusCode")!.GetValue(result.Value)!;
                }
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void OnActionExecuting(ActionExecutingContext context)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
        }
    }
}
