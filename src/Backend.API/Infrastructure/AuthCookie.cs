namespace Backend.API.Infrastructure;

// Token en cookie httpOnly: JavaScript no puede leerlo, así que un XSS no puede robarlo.
public static class AuthCookie
{
    public const string Name = "access_token";

    // Las peticiones que modifican datos y se autentican con la cookie deben traer esta cabecera.
    // Un sitio externo no puede agregarla sin pasar por CORS, lo que bloquea ataques CSRF.
    public const string CsrfHeader = "X-CSRF";

    public static void Append(HttpResponse response, string token, DateTimeOffset expires, bool secure)
    {
        response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expires,
            IsEssential = true
        });
    }

    public static void Delete(HttpResponse response, bool secure)
    {
        response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
    }
}
