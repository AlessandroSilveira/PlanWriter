namespace PlanWriter.API.Middleware
{
    public class MustChangePasswordMiddleware
    {
        private readonly RequestDelegate _next;

        public MustChangePasswordMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            // 🔓 ROTAS PÚBLICAS — NUNCA BLOQUEAR
            if (
                path.StartsWith("/api/auth/login") ||
                path.StartsWith("/api/auth/register") ||
                path.StartsWith("/api/auth/change-password") ||
                path.StartsWith("/api/auth/refresh") ||
                path.StartsWith("/api/auth/logout") ||
                path.StartsWith("/api/auth/logout-all") ||
                path.StartsWith("/swagger") ||
                path.StartsWith("/health")
            )
            {
                await _next(context);
                return;
            }

            // 🔒 SÓ APLICA PARA USUÁRIO AUTENTICADO
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var isAdmin = context.User.FindFirst("isAdmin")?.Value == "true";
                var mustChangePassword = context.User.FindFirst("mustChangePassword")?.Value == "true";

                if (isAdmin && mustChangePassword)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync(
                        "Admin must change password before accessing the system."
                    );
                    return;
                }
            }


            await _next(context);
        }
    }
}
