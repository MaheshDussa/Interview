// =====================================================================
//  04) ASP.NET CORE WEB API — Q&A and Scenarios
// =====================================================================
namespace Interview.WebApi
{
    // ---------------------------------------------------------------------
    // Q1: What is REST? Constraints?
    // A : Architectural style over HTTP. Stateless, cacheable, uniform
    //     interface, client-server, layered, code-on-demand (optional).
    //     Resources identified by URIs; actions via HTTP verbs.
    // ---------------------------------------------------------------------

    // Q2: HTTP verbs and idempotency.
    // A : GET    - read (safe, idempotent, cacheable)
    //     POST   - create (NOT idempotent)
    //     PUT    - replace (idempotent)
    //     PATCH  - partial update (not necessarily idempotent)
    //     DELETE - remove (idempotent)

    // Q3: Common status codes.
    // A : 200 OK, 201 Created, 202 Accepted, 204 No Content
    //     400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found
    //     409 Conflict, 422 Unprocessable Entity, 429 Too Many Requests
    //     500 Internal Server Error, 502 Bad Gateway, 503 Unavailable

    // Q4: 401 vs 403?
    // A : 401 = "I don't know who you are" (no/invalid auth).
    //     403 = "I know you, but you're not allowed".

    // ---------------------------------------------------------------------
    // Q5: How to design a versioned API?
    // A : URL  -> /api/v1/products  (most common)
    //     Header -> "api-version: 1.0"
    //     Query -> ?api-version=1.0
    //     Use Asp.Versioning.Mvc package.
    // ---------------------------------------------------------------------

    // Q6: Where do parameters come from? [FromBody], [FromQuery], [FromRoute],
    //     [FromHeader], [FromForm], [FromServices]?
    // A : Model binding searches in order: Form -> Route -> Query.
    //     For complex types, body is assumed; for primitives, route/query.

    // Q7: What is [ApiController] attribute?
    // A : Auto-400 on invalid model state, attribute routing required,
    //     inference for parameter sources, ProblemDetails responses.

    // ---------------------------------------------------------------------
    // Q8: Filters pipeline order?
    // A : Authorization -> Resource -> Action -> Result -> Exception
    //     Filters can be global, controller-level, or per-action.
    //
    //   /// public class LogFilter : IAsyncActionFilter
    //   /// {
    //   ///     public async Task OnActionExecutionAsync(ActionExecutingContext c,
    //   ///                                              ActionExecutionDelegate next)
    //   ///     {
    //   ///         /* before */
    //   ///         var result = await next();
    //   ///         /* after  */
    //   ///     }
    //   /// }
    // ---------------------------------------------------------------------

    // Q9: Authentication vs Authorization?
    // A : AuthN = who you are (JWT, cookies, OAuth).
    //     AuthZ = what you can do (roles, policies, claims).

    // Q10: How to secure a Web API with JWT?
    // A : builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //         .AddJwtBearer(o => o.TokenValidationParameters = ...);
    //     app.UseAuthentication(); app.UseAuthorization();
    //     [Authorize], [Authorize(Roles="Admin")], [Authorize(Policy="OwnerOnly")]

    // ---------------------------------------------------------------------
    // Q11: CORS — why and how?
    // A : Browsers block cross-origin XHR/fetch unless server allows it.
    //
    //   /// builder.Services.AddCors(o => o.AddPolicy("p",
    //   ///   p => p.WithOrigins("https://app.com").AllowAnyHeader().AllowAnyMethod()));
    //   /// app.UseCors("p");
    // ---------------------------------------------------------------------

    // Q12: How to handle validation?
    // A : Data annotations + [ApiController] auto-400, OR FluentValidation.
    //     Return ProblemDetails / ValidationProblemDetails for clients.

    // Q13: How to return files?
    // A : return File(stream, "application/pdf", "report.pdf");
    //     For very large files use FileStreamResult / streaming + chunked.

    // Q14: HATEOAS — needed?
    // A : Pure REST principle. Rarely required in practice; document with
    //     Swagger/OpenAPI instead.

    // Q15: Swagger/OpenAPI?
    // A : Swashbuckle.AspNetCore generates docs from controllers/XML comments.
    //     In .NET 9, built-in Microsoft.AspNetCore.OpenApi.

    // ---------------------------------------------------------------------
    // Q16: Idempotency on POST?
    // A : Accept an "Idempotency-Key" header; store key -> result; return
    //     same response for repeated requests within TTL.
    // ---------------------------------------------------------------------

    // Q17: How to implement rate limiting (.NET 7+)?
    // A : builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("api",
    //         opt => { opt.PermitLimit = 100; opt.Window = TimeSpan.FromMinutes(1); }));
    //     app.UseRateLimiter(); [EnableRateLimiting("api")]

    // Q18: How to log every request?
    // A : app.UseSerilogRequestLogging() (Serilog) OR built-in
    //     app.UseHttpLogging() in .NET 6+.

    // Q19: Difference between IActionResult, ActionResult<T>, Results.Ok(...)?
    // A : IActionResult     - non-generic, no return type info for Swagger.
    //     ActionResult<T>   - both result and typed body; preferred in MVC.
    //     Results.Ok(...)   - Minimal API style.

    // ---------------------------------------------------------------------
    // [Scenario] Q20: Client gets 415 Unsupported Media Type. Why?
    // A : Missing/wrong Content-Type header (should be application/json),
    //     OR endpoint uses [Consumes("application/xml")].
    // ---------------------------------------------------------------------

    // [Scenario] Q21: An API works in dev but in production behind a load
    //   balancer, Authorize redirects to login over HTTP instead of HTTPS.
    // A : Forwarded headers not honored. Add app.UseForwardedHeaders() with
    //     KnownProxies / ForwardedHeaders.XForwardedProto.

    // [Scenario] Q22: GET /products returns 100k rows and times out.
    // A : Add pagination (skip/take), filtering, projection (Select), and
    //     enable response compression. Consider streaming via IAsyncEnumerable.

    // [Scenario] Q23: Concurrent updates overwrite each other.
    // A : Use optimistic concurrency: RowVersion/ETag. Return 412 Precondition
    //     Failed or 409 Conflict when stale.

    // [Scenario] Q24: How would you upload a 2 GB file?
    // A : Use streaming multipart, disable model binding for the body,
    //     write to disk/blob storage in chunks, set request size limits.

    // [Scenario] Q25: Endpoint must return 202 + a status URL for polling.
    // A : Long-running pattern: enqueue job, return 202 with Location header
    //     pointing to /jobs/{id}; client polls until 200/done.

    internal static class _Api { }
}
