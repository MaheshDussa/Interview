// =====================================================================
//  12) NETWORKING & CLOUD INFRASTRUCTURE — Interview Q&A
// =====================================================================
//  Topics covered (with focus on Azure, but concepts apply to AWS/GCP):
//    • IP basics, IPv4 vs IPv6, CIDR, public vs private
//    • DNS, ports, OSI layers (L4 vs L7)
//    • Load Balancers (L4) vs Application Gateway (L7) vs Front Door
//    • Autoscaling (scale up vs scale out)
//    • NSG, ASG, firewall, WAF, routing rules
//    • Hybrid: VPN, ExpressRoute, peering
//    • Health probes, sticky sessions, SSL termination
//    • Real-world scenarios
// =====================================================================
namespace Interview.Networking
{
    // =====================================================================
    //  SECTION 1 — IP, PORTS, DNS, BASICS
    // =====================================================================

    // Q1: What is an IP address?
    // A : A numeric label identifying a device on a network.
    //     IPv4: 32-bit, 4 octets, e.g., 192.168.1.10 (~4.3 billion total).
    //     IPv6: 128-bit, hex groups, e.g., 2001:db8::1 (huge address space).

    // Q2: IPv4 vs IPv6 — why migrate?
    // A : IPv4 exhausted. IPv6 has more addresses, simpler headers,
    //     built-in IPsec, no NAT needed, better autoconfig (SLAAC).

    // Q3: Public vs Private IP?
    // A : Private ranges (RFC 1918), not routable on the internet:
    //         10.0.0.0/8
    //         172.16.0.0/12
    //         192.168.0.0/16
    //     Public IPs are globally unique and reachable from the internet.

    // Q4: What is CIDR notation?
    // A : "IP/prefix" — prefix = number of network bits.
    //         10.0.0.0/24  -> 256 IPs (10.0.0.0–10.0.0.255)
    //         10.0.0.0/16  -> 65 536 IPs
    //     Smaller number = bigger range.

    // Q5: Subnet — what and why?
    // A : A range of IPs within a VNet/VPC. Used to isolate workloads
    //     (web, app, data tiers), apply different NSGs, route differently.

    // Q6: What is NAT?
    // A : Network Address Translation — many private IPs share one public
    //     IP for outbound traffic. In Azure: NAT Gateway / SNAT on LB.

    // Q7: What is DNS?
    // A : Resolves names (api.example.com) to IPs. Records:
    //         A     - IPv4
    //         AAAA  - IPv6
    //         CNAME - alias to another name
    //         MX    - mail servers
    //         TXT   - SPF/DKIM/verifications
    //         NS    - delegations
    //     TTL controls cache duration.

    // Q8: Common ports.
    // A : 80 HTTP, 443 HTTPS, 22 SSH, 21 FTP, 25 SMTP, 53 DNS,
    //     1433 SQL Server, 3306 MySQL, 5432 Postgres, 6379 Redis,
    //     27017 MongoDB, 3389 RDP.

    // Q9: TCP vs UDP?
    // A : TCP - connection-oriented, reliable, ordered (HTTP, SQL).
    //     UDP - connectionless, fast, lossy (DNS, video, gaming, QUIC base).

    // Q10: OSI model — quick recap.
    // A : 7-App  | 6-Presentation | 5-Session
    //     4-Transport (TCP/UDP)   | 3-Network (IP) | 2-Data Link | 1-Physical
    //     "Layer 4" = transport (ports). "Layer 7" = application (HTTP).

    // =====================================================================
    //  SECTION 2 — LOAD BALANCERS (L4) vs APPLICATION GATEWAY (L7)
    // =====================================================================

    // Q11: What is a load balancer?
    // A : Distributes traffic across multiple healthy backend instances
    //     for scale, availability, and zero-downtime deploys.

    // Q12: Azure Load Balancer (ALB) — key facts.
    // A : - Layer 4 (TCP/UDP), very high throughput, low latency.
    //     - Doesn't inspect URLs or headers.
    //     - Skus: Basic (legacy) vs Standard (zones, NSG, HA).
    //     - Public LB (internet) or Internal LB (private VNet).
    //     - Has frontend IP, backend pool, health probes, LB rules,
    //       NAT rules (for RDP/SSH to specific VMs).

    // Q13: Azure Application Gateway (AGW) — key facts.
    // A : - Layer 7 (HTTP/HTTPS), URL/host/path-based routing.
    //     - SSL termination + end-to-end TLS, cookie-based affinity,
    //       redirects, rewrites, custom error pages.
    //     - Built-in WAF (Web Application Firewall) tier.
    //     - Listeners -> Rules -> Backend Pools -> HTTP Settings -> Probes.
    //     - Autoscaling (v2 SKU) by capacity units.

    // Q14: ALB vs Application Gateway — when to use which?
    // A : ALB (L4): non-HTTP protocols, raw TCP/UDP, max perf, simple LB.
    //     AGW (L7): HTTP routing rules, SSL, WAF, path-based routing,
    //               multiple sites on one IP, SSL offload.

    // Q15: Front Door / Traffic Manager / CDN — fit in the picture?
    // A : Azure Front Door  : global L7, anycast, WAF, multi-region routing,
    //                         SSL termination at the edge, caching.
    //     Traffic Manager   : DNS-based global routing (not a real proxy).
    //     CDN               : edge cache of static assets.
    //     Typical stack:
    //         Client -> Front Door -> Regional App Gateway -> AKS/AppService
    //         (Front Door = global; AGW = regional; LB = inside VNet.)

    // Q16: Round-robin / least connections / source IP affinity?
    // A : Distribution algorithms:
    //     - Round-robin  : next-in-line.
    //     - Least conn   : send to least-busy backend.
    //     - Source IP    : same client always lands on same backend (sticky).
    //     Sticky sessions enable stateful apps but reduce balance fairness.

    // Q17: Health probes — what and how?
    // A : LB periodically pings each backend (HTTP path or TCP port).
    //     If failing, instance is removed from rotation. ASP.NET Core:
    //
    //   /// builder.Services.AddHealthChecks().AddDbContextCheck<AppDb>();
    //   /// app.MapHealthChecks("/health");
    //
    //     Configure the probe path in LB/AGW to hit "/health".

    // Q18: SSL termination vs end-to-end TLS?
    // A : Termination - TLS ends at the gateway; backend gets plain HTTP.
    //                   Cheaper, but traffic to backend is unencrypted.
    //     End-to-end  - gateway terminates and re-encrypts to backend
    //                   (with backend cert). Required for compliance/PCI.

    // Q19: What is a WAF?
    // A : Web Application Firewall — inspects L7 traffic and blocks attacks
    //     (SQLi, XSS, etc.) using rule sets like OWASP CRS. Modes:
    //         Detection   - log only.
    //         Prevention  - block.
    //     Tune rules to avoid false positives.

    // =====================================================================
    //  SECTION 3 — SCALING
    // =====================================================================

    // Q20: Scale up vs scale out?
    // A : Scale UP (vertical)   - bigger box (more CPU/RAM). Hits a ceiling,
    //                              downtime to resize.
    //     Scale OUT (horizontal)- more boxes behind LB. Near-infinite, no
    //                              downtime, but app must be stateless.

    // Q21: How does Azure autoscaling work?
    // A : VM Scale Sets / App Service Plans / AKS HPA scale based on:
    //     - Metric rules: CPU > 70% for 10 min -> +2 instances.
    //     - Schedule:     scale to 10 instances Mon-Fri 8am, back at 8pm.
    //     - Custom metrics (Service Bus queue length, Application Insights).
    //     Include cool-down to avoid oscillation.

    // Q22: What makes an app "scale-out friendly"?
    // A : - Stateless (no in-memory session — use Redis / SQL).
    //     - Idempotent endpoints (so retries are safe).
    //     - Shared config (no per-instance files).
    //     - Distributed cache + distributed locks.
    //     - Shared Data Protection key ring (cookies/anti-forgery).
    //     - Background work in dedicated workers, not web instances.

    // Q23: Kubernetes scaling concepts.
    // A : HPA  - Horizontal Pod Autoscaler (CPU/mem/custom metrics).
    //     VPA  - Vertical Pod Autoscaler (sizes pods).
    //     CA   - Cluster Autoscaler (adds nodes).
    //     KEDA - event-driven scaling (queue depth -> pods).

    // Q24: Blue/green vs canary vs rolling deploy?
    // A : Rolling : replace instances batch-by-batch (default).
    //     Blue/Green : two full envs, swap traffic at LB; instant rollback.
    //     Canary : send small % traffic to new version, monitor, then ramp.

    // =====================================================================
    //  SECTION 4 — NETWORK SECURITY & RULES
    // =====================================================================

    // Q25: Virtual Network (VNet) / VPC?
    // A : Logically isolated network in the cloud. Subnets, NSGs, routes,
    //     peering with other VNets, gateways for hybrid connectivity.

    // Q26: NSG (Network Security Group) — what is it?
    // A : Stateful firewall at subnet/NIC level. Allow/Deny rules by:
    //         Source / Destination (IP, service tag, ASG)
    //         Port / Port range
    //         Protocol (TCP/UDP/Any)
    //         Priority (lower number = higher precedence)
    //         Direction (Inbound / Outbound)
    //     Default rules: AllowVNetInBound, AllowAzureLoadBalancerInBound,
    //                    DenyAllInBound (last).

    // Q27: ASG (Application Security Group)?
    // A : Group of NICs by role ("web", "db") so NSG rules reference the
    //     group instead of IP lists. Simpler when IPs change.

    // Q28: NSG vs Azure Firewall vs WAF?
    // A : NSG          - L3/L4 allow/deny rules at VNet edges.
    //     Azure Firewall - managed L3-L7, FQDN-based filtering, threat intel.
    //     WAF on AGW/FD - L7 web-attack protection (OWASP).
    //     Use together: defense in depth.

    // Q29: Service Endpoints vs Private Endpoints?
    // A : Service Endpoint - VNet route to PaaS over Microsoft backbone;
    //                       PaaS still has public IP.
    //     Private Endpoint - PaaS exposed via a private IP inside YOUR VNet;
    //                        no public exposure. Preferred for security.

    // Q30: VPN Gateway vs ExpressRoute?
    // A : VPN Gateway   - encrypted tunnel over public internet (S2S/P2S).
    //     ExpressRoute  - private fiber to Azure (no internet); lower
    //                     latency, higher SLA, costlier.

    // Q31: Peering?
    // A : Connect two VNets so VMs talk over Microsoft backbone (no public).
    //     Hub-and-spoke topology centralizes shared services (firewall, AD).

    // Q32: UDR (User Defined Route)?
    // A : Override default system routes; e.g., force all egress through
    //     Azure Firewall (NVA).

    // =====================================================================
    //  SECTION 5 — APP-RELEVANT NETWORKING
    // =====================================================================

    // Q33: CORS vs reverse proxy?
    // A : CORS - browser policy; server tells browser which origins may call it.
    //     Reverse proxy (AGW/Front Door) - forwards client request to backend;
    //     can also rewrite headers, terminate TLS, add auth, etc.

    // Q34: What is a reverse proxy in ASP.NET Core?
    // A : When behind AGW/Front Door, original client IP and scheme arrive in
    //     X-Forwarded-For / X-Forwarded-Proto. Add:
    //
    //   /// app.UseForwardedHeaders(new ForwardedHeadersOptions {
    //   ///     ForwardedHeaders = ForwardedHeaders.XForwardedFor
    //   ///                       | ForwardedHeaders.XForwardedProto
    //   /// });
    //
    //     Otherwise HTTPS redirects, RemoteIp, and Authorize redirects break.

    // Q35: How to limit who can reach your app at the network layer?
    // A : - NSG inbound rules (IP allowlist).
    //     - App Service "Access Restrictions".
    //     - Private Endpoint + remove public access.
    //     - WAF rules on country, IP, header.

    // Q36: How does an outbound request from your app reach the internet
    //      in Azure?
    // A : App Service / VMSS uses default SNAT pool (limited ports).
    //     For predictable IP + scale: attach a NAT Gateway to the subnet.

    // Q37: Why does "intermittent 502" happen at App Gateway?
    // A : Common causes:
    //     - Backend health probe failing (wrong path, slow startup).
    //     - Backend cert/hostname mismatch in end-to-end TLS.
    //     - Backend connection timeout shorter than gateway's idle.
    //     - WAF blocking; check WAF logs.

    // Q38: What is HSTS?
    // A : "Strict-Transport-Security" header tells browsers to use HTTPS
    //     only for a duration. Enable AFTER stable HTTPS deployment.

    // Q39: TLS handshake — high level?
    // A : ClientHello -> ServerHello + cert chain -> key exchange ->
    //     symmetric session key -> encrypted application data.
    //     TLS 1.3 is faster and removes weak ciphers.

    // Q40: Public DNS for an app.
    // A : Register zone (api.example.com) -> CNAME to AGW/Front Door
    //     hostname -> attach managed cert or upload PFX. TTL low during
    //     cutovers.

    // =====================================================================
    //  SCENARIOS
    // =====================================================================

    // [Scenario] Q41: You must host www.contoso.com and api.contoso.com on
    //   the same public IP with separate backends.
    // A : Application Gateway with multi-site listeners:
    //         Listener "www" (host www.contoso.com)  -> Pool A
    //         Listener "api" (host api.contoso.com)  -> Pool B
    //     One Public IP, SNI selects cert. Add path-based rules under each.

    // [Scenario] Q42: Web app should only be reachable from corporate office IPs.
    // A : - AGW WAF / Listener with custom rule + IP match.
    //     - Or NSG inbound allow office CIDR, deny rest.
    //     - Or App Service Access Restrictions.

    // [Scenario] Q43: Your API needs to call SQL Database without public exposure.
    // A : Private Endpoint for SQL into your VNet + remove public access.
    //     App in the same/peered VNet uses the private IP via Private DNS.

    // [Scenario] Q44: Behind App Gateway, users land on different backends
    //   each request and lose their shopping cart.
    // A : Either:
    //     - Enable cookie-based session affinity on AGW.
    //     - Or (better) externalize state to Redis / DB so any pod serves any user.

    // [Scenario] Q45: CPU rarely exceeds 30%, but the app feels slow under load.
    // A : Could be I/O-bound (DB, downstream). Scale-up won't help much;
    //     consider caching, query tuning, and scale-out + async I/O.

    // [Scenario] Q46: Autoscale flaps — instances added then removed every few mins.
    // A : Increase the metric duration window, raise scale-in threshold,
    //     and add a cool-down period. Also check if startup is too slow,
    //     making new instances appear "unhealthy".

    // [Scenario] Q47: SSL cert renewed but clients still see expired one.
    // A : Cert cached at CDN/Front Door/AGW. Rebind listener with new cert
    //     (or use managed certs/Key Vault auto-rotation). Check DNS TTL too.

    // [Scenario] Q48: Cross-region failover plan?
    // A : Front Door / Traffic Manager with two regional stacks
    //     (AGW + App Service + DB geo-replica). Health probes detect region
    //     outage; client traffic routes to the healthy region. Practice it.

    // [Scenario] Q49: WAF blocks a legitimate POST.
    // A : Check WAF logs for the matched rule ID -> add per-rule exclusion
    //     or change the field (header/body/cookie) excluded. Don't disable
    //     WAF globally; tune surgically.

    // [Scenario] Q50: How would you secure outbound traffic from VMs?
    // A : Route via Azure Firewall (UDR pointing 0.0.0.0/0 to firewall),
    //     allow only required FQDNs/IPs, log denies. Use Private Endpoints
    //     for PaaS to avoid leaving the VNet at all.

    // =====================================================================
    //  QUICK REFERENCE — "Which Azure resource for what?"
    // =====================================================================
    // • Distribute global users to nearest region   -> Front Door
    // • L7 routing inside one region with WAF       -> Application Gateway
    // • L4 TCP/UDP balancing                        -> Azure Load Balancer
    // • DNS-based geo routing (no proxy)            -> Traffic Manager
    // • Static asset edge cache                     -> Azure CDN
    // • Outbound public IP from VNet                -> NAT Gateway
    // • Private connectivity to PaaS                -> Private Endpoint
    // • Site-to-site to on-prem (encrypted internet)-> VPN Gateway
    // • Private fiber to Azure                      -> ExpressRoute
    // • Centralized L3-L7 firewall + FQDN rules     -> Azure Firewall
    // • Block OWASP attacks at L7                   -> WAF (on AGW or FD)

    internal static class _Network { }
}
