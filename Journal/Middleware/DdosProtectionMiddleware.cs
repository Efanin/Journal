using Microsoft.Extensions.Caching.Memory;

namespace Journal.Middleware
{
    public class DdosProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DdosProtectionMiddleware> _logger;

        public DdosProtectionMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<DdosProtectionMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(ip))
            {
                await _next(context);
                return;
            }

            // Проверяем форму на CSRF токен
            if (context.Request.Method == "POST" &&
                (context.Request.Path.StartsWithSegments("/Account/Login") ||
                 context.Request.Path.StartsWithSegments("/FileUpload/Upload")))
            {
                if (!await IsValidCsrfToken(context))
                {
                    _logger.LogWarning($"CSRF validation failed for IP: {ip}");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("CSRF validation failed");
                    return;
                }
            }

            // Проверка частоты запросов
            if (!await CheckRateLimit(ip, context))
            {
                return;
            }

            await _next(context);
        }

        private async Task<bool> CheckRateLimit(string ip, HttpContext context)
        {
            var key = $"rate_limit_{ip}";
            var window = TimeSpan.FromMinutes(1);
            var maxRequests = 60; // Макс. 60 запросов в минуту

            var requestCount = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return 0;
            });

            if (requestCount >= maxRequests)
            {
                _logger.LogWarning($"Rate limit exceeded for IP: {ip}, Count: {requestCount}");

                // Добавляем задержку для превысивших лимит
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, requestCount - maxRequests)));

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Add("Retry-After", "60");
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return false;
            }

            _cache.Set(key, requestCount + 1);
            return true;
        }

        private async Task<bool> IsValidCsrfToken(HttpContext context)
        {
            if (!context.Request.HasFormContentType)
                return false;

            var form = await context.Request.ReadFormAsync();
            var token = form["__RequestVerificationToken"].FirstOrDefault();
            var cookieToken = context.Request.Cookies["XSRF-TOKEN"];

            // Упрощенная проверка - в реальном проекте используйте встроенную валидацию
            return !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(cookieToken);
        }


    }

    public static class DdosProtectionMiddlewareExtensions
    {
        public static IApplicationBuilder UseDdosProtection(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DdosProtectionMiddleware>();
        }
    }
}
