using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PlanWriter.API.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // usuário não autenticado
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var isAdminClaim =
                context.HttpContext.User.FindFirst("isAdmin")?.Value;
            var adminMfaVerifiedClaim =
                context.HttpContext.User.FindFirst("adminMfaVerified")?.Value;

            if (isAdminClaim != "true" || adminMfaVerifiedClaim != "true")
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
