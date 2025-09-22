namespace Journal.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/Home/Index") ||
                context.Request.Path.StartsWithSegments("/Home/Login"))
            {
                await _next(context);
                return;
            }
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.Redirect("/Home/Index");
                return;
            }
            await _next(context);
        }
    }
}
