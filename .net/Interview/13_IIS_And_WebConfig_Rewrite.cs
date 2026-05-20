// =====================================================================
//  13) IIS & web.config / URL REWRITE — Interview Q&A
// =====================================================================
//  Why IIS still matters even in the .NET Core / Linux era:
//   • Many enterprise apps still host on Windows + IIS.
//   • ASP.NET Core can run BEHIND IIS via the ASP.NET Core Module (ANCM).
//   • web.config is required for IIS-hosted apps (rewrite, headers, MIME).
//   • URL Rewrite Module is one of the most asked operational tools.
// =====================================================================
namespace Interview.IIS
{
    // =====================================================================
    //  SECTION 1 — IIS BASICS
    // =====================================================================

    // Q1: What is IIS?
    // A : Internet Information Services — Microsoft's web server on Windows.
    //     Hosts HTTP/HTTPS apps, serves static files, runs management UI
    //     (inetmgr), exposes performance counters, logs, etc.

    // Q2: Core building blocks of IIS.
    // A : - Site            : a hostname + binding (e.g., www.contoso.com:443)
    //     - Application     : a virtual path inside a site that runs an app
    //     - Virtual Directory: a path mapped to a folder (no isolated app)
    //     - Application Pool: a worker process (w3wp.exe) with its own
    //                         identity, .NET version, and isolation.

    // Q3: What is an Application Pool (AppPool)?
    // A : The OS process that runs your app. Each AppPool maps to a
    //     w3wp.exe instance. Isolating apps in separate pools means a
    //     crash/leak in one app doesn't affect others. Key settings:
    //     - .NET CLR Version  (No Managed Code = required for ASP.NET Core)
    //     - Identity (ApplicationPoolIdentity / domain account / MSI)
    //     - Pipeline mode (Integrated vs Classic)
    //     - Recycling (time, requests, memory thresholds)
    //     - Idle timeout (default 20 min — app shuts down if no traffic)
    //     - Start mode (OnDemand vs AlwaysRunning)

    // Q4: Integrated pipeline vs Classic pipeline?
    // A : Integrated (default) : ASP.NET integrates with IIS modules; all
    //                            requests flow through both pipelines.
    //     Classic              : IIS and ASP.NET are separate; legacy only.
    //                            ASP.NET Core requires Integrated.

    // Q5: How does IIS host ASP.NET Core? (ANCM)
    // A : Two modes via the ASP.NET Core Module:
    //     - InProcess (default)  : Kestrel runs INSIDE w3wp.exe -> fastest.
    //     - OutOfProcess         : IIS reverse-proxies to a separate
    //                              dotnet.exe (Kestrel) over HTTP/HTTPS.
    //                              Required for some scenarios (e.g.,
    //                              long-polling, certain auth modules).
    //     AppPool must be set to "No Managed Code" because the .NET
    //     runtime is loaded by ANCM, not by IIS.

    // Q6: What is web.config in ASP.NET Core?
    // A : Still required when hosting on IIS — it points IIS to your
    //     published .dll via ANCM. Configures rewrite rules, custom
    //     headers, request limits, MIME types, etc.
    //
    //   /// <?xml version="1.0" encoding="utf-8"?>
    //   /// <configuration>
    //   ///   <location path="." inheritInChildApplications="false">
    //   ///     <system.webServer>
    //   ///       <handlers>
    //   ///         <add name="aspNetCore" path="*" verb="*"
    //   ///              modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    //   ///       </handlers>
    //   ///       <aspNetCore processPath=".\MyApp.exe"
    //   ///                   stdoutLogEnabled="false"
    //   ///                   stdoutLogFile=".\logs\stdout"
    //   ///                   hostingModel="InProcess" />
    //   ///     </system.webServer>
    //   ///   </location>
    //   /// </configuration>

    // Q7: Default ports and bindings.
    // A : Bindings = (Protocol, IP/All, Port, Host name, SNI cert).
    //     80 HTTP, 443 HTTPS. SNI lets multiple HTTPS sites share an IP+port
    //     by selecting cert based on Host header.

    // Q8: Common IIS modules you should know.
    // A : - StaticFile, DefaultDocument, DirectoryBrowsing
    //     - HTTP Redirect, URL Rewrite (separate install)
    //     - Compression (Static + Dynamic)
    //     - WindowsAuthentication, BasicAuth, AnonymousAuth
    //     - RequestFiltering (limits, file ext blocks)
    //     - Logging, FailedRequestTracing (FREB)
    //     - ASP.NET Core Module v2 (ANCMv2)

    // Q9: AppPool Identity — what runs your app?
    // A : Default = "ApplicationPoolIdentity" (virtual account
    //     "IIS APPPOOL\<name>"). Grant filesystem/DB perms to this identity.
    //     For domain resources, switch to a domain service account or MSI.

    // Q10: Recycling — when does it happen and why?
    // A : Triggers (any):
    //     - Time (default 1740 min = 29h)
    //     - Number of requests
    //     - Memory thresholds (private/virtual)
    //     - Config change in web.config / applicationHost.config
    //     - Manual: appcmd recycle apppool
    //     Recycling = overlap launch: new w3wp warms up, then drains old one.

    // Q11: Idle Timeout and "first request is slow".
    // A : If no requests for IdleTimeout (default 20 min), w3wp shuts down;
    //     next request triggers cold start (JIT, EF model build).
    //     Fixes: set Idle Timeout = 0, Start Mode = AlwaysRunning,
    //     Preload = true, and add an Application Initialization warmup URL.

    // Q12: How IIS handles HTTPS.
    // A : Bind site to 443 with a certificate (My/LocalMachine store).
    //     SNI required for multiple HTTPS hosts on one IP. HTTP/2 supported.
    //     TLS protocols/ciphers are configured via Windows SCHANNEL +
    //     IISCrypto tool.

    // Q13: Logging in IIS.
    // A : - W3C logs in %SystemDrive%\inetpub\logs\LogFiles\W3SVC{n}\
    //     - HTTP.SYS logs in %windir%\System32\LogFiles\HTTPERR\
    //     - FailedRequestTracing (FREB): rules per status code/time;
    //       writes XML trace per failing request. Great for debugging 500/502.

    // Q14: HTTP.sys?
    // A : Kernel-mode driver that accepts requests before IIS user-mode
    //     picks them up. Bandwidth limits / queue length set here.
    //     "Service Unavailable" 503 with "Application Pool stopped" is from HTTP.sys.

    // =====================================================================
    //  SECTION 2 — URL REWRITE MODULE
    // =====================================================================

    // Q15: What is URL Rewrite?
    // A : An IIS module that intercepts incoming URLs and either:
    //     - REWRITES them (server-side, transparent to client) OR
    //     - REDIRECTS them (sends 301/302 to client, URL bar changes).
    //     Install: "URL Rewrite 2.x" from IIS Web Platform Installer or MSI.
    //     Outbound rules can also rewrite response content/headers.

    // Q16: Rewrite vs Redirect — when to use which?
    // A : Rewrite : keep URL clean for users, route to internal handler.
    //                e.g., /products/42 -> /Default.aspx?id=42
    //     Redirect: tell client to use a new URL (SEO, HTTPS).
    //                301 permanent / 302 temporary.

    // Q17: Force HTTPS — canonical example.
    //
    //   /// <system.webServer>
    //   ///   <rewrite>
    //   ///     <rules>
    //   ///       <rule name="HTTP to HTTPS" stopProcessing="true">
    //   ///         <match url="(.*)" />
    //   ///         <conditions>
    //   ///           <add input="{HTTPS}" pattern="^OFF$" />
    //   ///         </conditions>
    //   ///         <action type="Redirect"
    //   ///                 url="https://{HTTP_HOST}/{R:1}"
    //   ///                 redirectType="Permanent" />
    //   ///       </rule>
    //   ///     </rules>
    //   ///   </rewrite>
    //   /// </system.webServer>

    // Q18: Canonical host (force www, force apex).
    //
    //   /// <rule name="Force www" stopProcessing="true">
    //   ///   <match url="(.*)" />
    //   ///   <conditions>
    //   ///     <add input="{HTTP_HOST}" pattern="^contoso\.com$" />
    //   ///   </conditions>
    //   ///   <action type="Redirect"
    //   ///           url="https://www.contoso.com/{R:1}"
    //   ///           redirectType="Permanent" />
    //   /// </rule>

    // Q19: Pretty URLs (path -> querystring) — internal rewrite.
    //
    //   /// <rule name="ProductDetails">
    //   ///   <match url="^products/([0-9]+)$" />
    //   ///   <action type="Rewrite" url="Default.aspx?id={R:1}" />
    //   /// </rule>

    // Q20: Reverse-proxy rule (front IIS in front of Kestrel/Node).
    //
    //   /// <rule name="ReverseProxyToApi" stopProcessing="true">
    //   ///   <match url="api/(.*)" />
    //   ///   <action type="Rewrite" url="http://localhost:5000/{R:1}" />
    //   ///   <serverVariables>
    //   ///     <set name="HTTP_X_FORWARDED_FOR"   value="{REMOTE_ADDR}" />
    //   ///     <set name="HTTP_X_FORWARDED_PROTO" value="{REQUEST_SCHEME}" />
    //   ///   </serverVariables>
    //   /// </rule>
    //
    //   Requirements:
    //   - Install "Application Request Routing" (ARR), enable proxy.
    //   - Allow the server variables in IIS > Configuration > URL Rewrite > "Allowed server variables".

    // Q21: Remove trailing slash / lowercase URLs (SEO).
    //
    //   /// <rule name="LowerCase" stopProcessing="true">
    //   ///   <match url=".*[A-Z].*" ignoreCase="false" />
    //   ///   <action type="Redirect" url="{ToLower:{URL}}" redirectType="Permanent" />
    //   /// </rule>

    // Q22: Match types.
    // A : - Regular Expression (default)
    //     - Wildcard ( * matches segment )
    //     - Exact Match
    //     Use back-references: {R:0}=entire match, {R:1}=group 1, etc.
    //     {C:1} = back-reference from the last matched CONDITION.

    // Q23: Common server variables you'll see.
    // A : {HTTP_HOST}, {HTTP_USER_AGENT}, {HTTPS} (ON/OFF), {URL},
    //     {QUERY_STRING}, {REQUEST_URI}, {REQUEST_METHOD}, {REMOTE_ADDR},
    //     {REQUEST_SCHEME}, {SERVER_PORT}.

    // Q24: stopProcessing — what does it do?
    // A : If true, IIS skips the remaining rules in the set for this
    //     request once the rule matches. Order matters: most specific first.

    // Q25: Outbound rules.
    // A : Rewrite headers or response body content as it leaves IIS.
    //     Useful for adding security headers, replacing internal URLs in
    //     proxied responses.

    // =====================================================================
    //  SECTION 3 — web.config CHEAT-SHEET
    // =====================================================================

    // Q26: Add security headers via web.config.
    //
    //   /// <system.webServer>
    //   ///   <httpProtocol>
    //   ///     <customHeaders>
    //   ///       <add name="Strict-Transport-Security" value="max-age=31536000; includeSubDomains" />
    //   ///       <add name="X-Content-Type-Options"    value="nosniff" />
    //   ///       <add name="Referrer-Policy"           value="strict-origin-when-cross-origin" />
    //   ///       <add name="X-Frame-Options"           value="SAMEORIGIN" />
    //   ///       <remove name="X-Powered-By" />
    //   ///     </customHeaders>
    //   ///   </httpProtocol>
    //   /// </system.webServer>

    // Q27: Limit upload size + request timeout.
    //
    //   /// <security>
    //   ///   <requestFiltering>
    //   ///     <requestLimits maxAllowedContentLength="104857600" /> <!-- 100 MB -->
    //   ///   </requestFiltering>
    //   /// </security>
    //   /// <aspNetCore processPath="..\" requestTimeout="00:10:00" />

    // Q28: Custom MIME types.
    //
    //   /// <staticContent>
    //   ///   <mimeMap fileExtension=".webmanifest" mimeType="application/manifest+json" />
    //   /// </staticContent>

    // Q29: Disable directory browsing + set default doc.
    //
    //   /// <directoryBrowse enabled="false" />
    //   /// <defaultDocument><files><clear /><add value="index.html" /></files></defaultDocument>

    // Q30: Static file caching.
    //
    //   /// <staticContent>
    //   ///   <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="365.00:00:00" />
    //   /// </staticContent>

    // Q31: Compression.
    //
    //   /// <urlCompression doStaticCompression="true" doDynamicCompression="true" />

    // Q32: Block file extensions.
    //
    //   /// <requestFiltering>
    //   ///   <fileExtensions allowUnlisted="true">
    //   ///     <add fileExtension=".config" allowed="false" />
    //   ///     <add fileExtension=".bak"    allowed="false" />
    //   ///   </fileExtensions>
    //   /// </requestFiltering>

    // =====================================================================
    //  SECTION 4 — OPERATIONAL / TROUBLESHOOTING
    // =====================================================================

    // Q33: appcmd basics (run as admin).
    // A : appcmd list sites
    //     appcmd list apppools
    //     appcmd recycle apppool /apppool.name:"MyAppPool"
    //     appcmd start  site /site.name:"Default Web Site"
    //     appcmd stop   site /site.name:"Default Web Site"
    //     appcmd set config -section:system.applicationHost/... 

    // Q34: PowerShell equivalents.
    // A : Import-Module WebAdministration
    //     Get-Website ; Get-WebAppPoolState -Name MyPool
    //     Restart-WebAppPool MyPool
    //     New-WebBinding -Name "Default Web Site" -Protocol https -Port 443 -HostHeader "x.com"

    // Q35: HTTP 502.5 / 500.30 / 500.31 / 500.32 (ASP.NET Core).
    // A : 500.30 - app failed to start (check stdout logs / Event Viewer).
    //     500.31 - ANCM failed to find native runtime.
    //     500.32 - ANCM failed to load .NET Core (architecture mismatch).
    //     500.34 - mixed hosting models / multiple in-process apps in one pool.
    //     502.5  - process failure (out-of-process mode).

    // Q36: Where to find ASP.NET Core hosting errors.
    // A : 1) Event Viewer -> Windows Logs -> Application (source "IIS AspNetCore Module").
    //     2) stdout logs (enable in web.config; remember to disable later).
    //     3) FailedRequestTracing for specific status codes.

    // Q37: 503 Service Unavailable.
    // A : AppPool stopped. Reasons:
    //     - Rapid-Fail Protection tripped (5 failures in 5 min by default).
    //     - Identity password expired / account disabled.
    //     - Memory recycling loops.
    //     Fix root cause, then "Start" the AppPool.

    // Q38: 413 Request Too Large.
    // A : Increase requestLimits.maxAllowedContentLength (IIS) AND
    //     Kestrel limit (KestrelServerOptions.Limits.MaxRequestBodySize)
    //     AND form options (FormOptions.MultipartBodyLengthLimit).

    // Q39: Logging best practices.
    // A : Forward IIS logs + app logs to a central sink (App Insights,
    //     Seq, ELK, Splunk). Include {TraceId}/{SpanId} for correlation.
    //     Trim raw stdout logs in production to avoid disk fill.

    // Q40: IIS Express vs IIS?
    // A : IIS Express = lightweight dev-only server, used by Visual Studio.
    //     Same config schema. Production should use full IIS or Kestrel
    //     behind a reverse proxy (IIS / Nginx).

    // =====================================================================
    //  SECTION 5 — SCENARIOS
    // =====================================================================

    // [Scenario] Q41: After publishing, the site shows "HTTP Error 500.19 -
    //   Internal Server Error" with "Cannot read configuration file".
    // A : web.config invalid XML, OR a locked section being overridden
    //     (e.g., <httpErrors>). Unlock at server level or wrap the section
    //     in <location ... overrideMode="Allow">.

    // [Scenario] Q42: Site responds first request very slowly, then fast.
    // A : Cold start due to idle timeout / recycle. Enable Always Running:
    //     - AppPool: Idle Time-out = 0, Start Mode = AlwaysRunning.
    //     - Site: Preload Enabled = True.
    //     - Install "Application Initialization" and configure warmup URL.

    // [Scenario] Q43: HTTPS works on the server but external clients get
    //   "ERR_CERT_COMMON_NAME_INVALID".
    // A : Cert subject/SAN doesn't include the requested hostname. Re-issue
    //     cert with proper SAN. Check binding uses SNI and correct cert.

    // [Scenario] Q44: An attacker is probing /wp-admin/, /xmlrpc.php on
    //   your ASP.NET site, generating noise.
    // A : Add request-filtering rules to deny those paths and return 404
    //     immediately; or add URL Rewrite rule with AbortRequest action.

    // [Scenario] Q45: The site needs to support both /api -> Kestrel app
    //   and /admin -> legacy ASP.NET WebForms on the same hostname.
    // A : Two Applications under one Site:
    //     - "/api" : separate AppPool (No Managed Code, ANCM)
    //     - "/admin": classic AppPool (.NET Framework 4.x Integrated)
    //     URL Rewrite optional for friendlier routing.

    // [Scenario] Q46: 502 errors only when responses are large.
    // A : Upstream/Kestrel timeout shorter than IIS, or ARR proxy timeout
    //     too low. Increase ARR proxy timeout (default 30s) via:
    //     - IIS > Server > Application Request Routing Cache > Proxy.
    //     Also check responseBufferLimit + Kestrel KeepAlive settings.

    // [Scenario] Q47: Need to migrate IIS-hosted ASP.NET Core to Linux/Nginx.
    // A : web.config -> NOT used on Linux. Replace with:
    //     - Kestrel listening on a Unix socket or 127.0.0.1:5000.
    //     - Nginx reverse proxy: proxy_pass, X-Forwarded-* headers.
    //     - systemd unit file for auto-restart.
    //     - Update CI/CD to publish self-contained Linux binaries.

    // [Scenario] Q48: After deployment, web.config changes don't take effect.
    // A : IIS watches web.config for changes and auto-recycles. If it
    //     doesn't, file ACLs may block read access for the AppPool identity,
    //     or the file is locked by an editor. Manually recycle the pool.

    // [Scenario] Q49: Need to redirect old URLs (/old/x) to new (/new/x)
    //   permanently for SEO.
    //
    //   /// <rule name="Old to New" stopProcessing="true">
    //   ///   <match url="^old/(.*)$" />
    //   ///   <action type="Redirect" url="/new/{R:1}" redirectType="Permanent" />
    //   /// </rule>

    // [Scenario] Q50: How to make ASP.NET Core trust X-Forwarded-* set by
    //   IIS / ARR?
    // A : app.UseForwardedHeaders(new ForwardedHeadersOptions {
    //         ForwardedHeaders = ForwardedHeaders.XForwardedFor
    //                          | ForwardedHeaders.XForwardedProto });
    //     Place BEFORE UseAuthentication. Without it, RemoteIp = 127.0.0.1
    //     and HTTPS redirects can loop.

    // =====================================================================
    //  QUICK REFERENCE — "IIS feature => when to use"
    // =====================================================================
    // • Lots of small sites on one box      -> separate AppPools per site
    // • Hot redeploys without downtime      -> overlapped recycling
    // • SPA hosted on IIS, deep-link routing-> URL Rewrite to /index.html
    // • Front legacy app with new routes    -> URL Rewrite + ARR (proxy)
    // • Force HTTPS                         -> Rewrite redirect + HSTS header
    // • Block known bad paths               -> Request Filtering
    // • Reverse proxy to Kestrel            -> ANCM (recommended) or ARR
    // • Multi-tenant HTTPS on one IP        -> Host headers + SNI certs
    // • Stop "slow first hit"               -> AlwaysRunning + AppInit warmup

    internal static class _Iis { }
}
