// =====================================================================
//  05) ASP.NET CORE MVC — Q&A and Scenarios
// =====================================================================
namespace Interview.Mvc
{
    // ---------------------------------------------------------------------
    // Q1: What is MVC?
    // A : Model (data/business), View (UI/Razor), Controller (orchestrator).
    //     Promotes separation of concerns and testability.
    // ---------------------------------------------------------------------

    // Q2: Razor view vs Razor Page?
    // A : View      = part of MVC, paired with a controller action.
    //     Razor Page = self-contained page with its own PageModel (handlers).

    // ---------------------------------------------------------------------
    // Q3: How does routing work?
    // A : Two flavors:
    //   • Convention-based:
    //   ///   app.MapControllerRoute(name: "default",
    //   ///       pattern: "{controller=Home}/{action=Index}/{id?}");
    //   • Attribute-based:
    //   ///   [Route("products/{id:int}")]
    //   ///   public IActionResult Get(int id) { ... }
    // ---------------------------------------------------------------------

    // Q4: Route constraints?
    // A : {id:int}, {name:alpha}, {slug:regex(^[a-z0-9-]+$)}, {age:min(18)}

    // Q5: Order of view discovery?
    // A : /Views/{Controller}/{Action}.cshtml
    //     /Views/Shared/{Action}.cshtml

    // ---------------------------------------------------------------------
    // Q6: ViewData vs ViewBag vs TempData vs Model?
    // A : ViewData - IDictionary<string,object>; per-request; needs cast.
    //     ViewBag  - dynamic wrapper over ViewData.
    //     TempData- survives ONE redirect (cookie or session-backed).
    //     Model   - strongly typed; preferred.
    // ---------------------------------------------------------------------

    // Q7: PRG pattern?
    // A : Post-Redirect-Get. After POST succeeds, redirect to a GET so
    //     refresh doesn't resubmit the form. Use TempData for one-shot msg.

    // ---------------------------------------------------------------------
    // Q8: Model binding sources?
    // A : Form -> Route -> Query (for primitives). [FromBody] for JSON.
    //     Complex types bound from form by default.
    //
    //   /// [HttpPost] public IActionResult Save(OrderVm vm) { ... }
    // ---------------------------------------------------------------------

    // Q9: Validation in MVC?
    // A : Data annotations on the view model:
    //   /// public class LoginVm
    //   /// {
    //   ///     [Required, EmailAddress] public string Email { get; set; }
    //   ///     [Required, MinLength(6)] public string Password { get; set; }
    //   /// }
    //   /// if (!ModelState.IsValid) return View(vm);

    // Q10: Tag helpers vs HTML helpers?
    // A : Tag helpers (asp-for, asp-action) are HTML-like, IDE-friendly.
    //     HTML helpers (@Html.TextBoxFor) are method-based, older style.

    // Q11: Partial view vs View Component?
    // A : Partial - reuses a chunk of markup, can take a model.
    //     ViewComponent - mini-controller with its own logic; better for
    //                     widgets like menus, cart summary, login status.

    // Q12: Layouts and sections?
    // A : _Layout.cshtml defines the shell. Pages provide content +
    //     @section Scripts { ... } for placing scripts at the bottom.

    // Q13: How does anti-forgery work?
    // A : Server issues a token cookie + hidden form field. On POST it
    //     validates them. [ValidateAntiForgeryToken] enforces it.
    //     Razor's <form> auto-injects the token.

    // Q14: Action filters for MVC?
    // A : Same pipeline as Web API: Auth -> Resource -> Action -> Result.
    //     Use OnActionExecuting for logging, OnResultExecuting to modify view.

    // Q15: Areas?
    // A : Group large MVC apps by feature (e.g., /Admin/*) with their own
    //     Controllers/Views/Models folders. Routed via {area:exists}.

    // ---------------------------------------------------------------------
    // [Scenario] Q16: After redirect, your flash message is gone. Why?
    // A : You stored it in ViewBag/ViewData — those die at request end.
    //     Use TempData.
    // ---------------------------------------------------------------------

    // [Scenario] Q17: Posted form values aren't binding to your model.
    // A : Property names mismatch input names (case/spelling), property has
    //     no public setter, or [Bind] excludes them. Check Network tab and
    //     ModelState errors.

    // [Scenario] Q18: A view shows a stale dropdown after validation fails.
    // A : You returned View(model) without repopulating the SelectList
    //     because ModelState retained user input but not lookups.

    // [Scenario] Q19: How do you avoid duplicate form submissions on refresh?
    // A : PRG pattern (Post -> RedirectToAction -> Get).

    // [Scenario] Q20: How to share a layout across Areas?
    // A : Place _ViewStart.cshtml or _Layout.cshtml in /Views/Shared, or set
    //     Layout in each area's _ViewStart.

    internal static class _Mvc { }
}
