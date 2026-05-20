// =====================================================================
//  13) MICROSERVICES & ARCHITECTURE — Interview Q&A
// =====================================================================
namespace Interview.Microservices
{
    // ---------------------------------------------------------------------
    //  FUNDAMENTALS
    // ---------------------------------------------------------------------

    // Q1: Monolith vs Microservices?
    // A : Monolith     - single deployable, simple, shared DB, scales as a whole.
    //     Microservices - many small services, own DB, deploy independently,
    //                     scale per service, but distributed-system complexity.

    // Q2: When NOT to use microservices?
    // A : Small team, unclear domain boundaries, no DevOps maturity,
    //     low traffic. Premature split = "distributed monolith" pain.

    // Q3: What is a "bounded context" (DDD)?
    // A : A logical boundary where a model is consistent. Each microservice
    //     usually owns one bounded context (Orders, Catalog, Billing).

    // Q4: Database per service — why?
    // A : Loose coupling, independent deploys, schema freedom.
    //     Cross-service queries become API calls or events, not joins.

    // ---------------------------------------------------------------------
    //  COMMUNICATION
    // ---------------------------------------------------------------------

    // Q5: Sync vs async communication?
    // A : Sync  - HTTP/REST, gRPC. Simple, but couples availability.
    //     Async - events/messages (queue, bus). Decoupled, resilient.
    //     Rule of thumb: use async events for cross-service writes,
    //                    sync for user-facing reads.

    // Q6: REST vs gRPC?
    // A : REST  - text JSON over HTTP, browser-friendly, ubiquitous.
    //     gRPC  - binary Protobuf over HTTP/2, contracts (.proto),
    //             streaming, fast, ideal for service-to-service calls.

    // Q7: API Gateway pattern?
    // A : Single entry point for clients. Handles auth, rate limiting,
    //     routing, aggregation, versioning. Examples: Azure APIM, Ocelot,
    //     YARP, Kong, NGINX, AWS API Gateway.

    // Q8: BFF (Backend For Frontend)?
    // A : Per-client gateway (one for web, one for mobile) that tailors
    //     responses and hides multiple services. Reduces over-fetching.

    // Q9: Service discovery?
    // A : How services find each other dynamically. In Kubernetes: DNS-based
    //     (Service objects). Others: Consul, Eureka. In Azure: Service Fabric,
    //     APIM, or simply DNS + ingress.

    // ---------------------------------------------------------------------
    //  RESILIENCE
    // ---------------------------------------------------------------------

    // Q10: Patterns for resilient service calls?
    // A : Timeout       - fail fast.
    //     Retry         - with exponential backoff + jitter (idempotent only).
    //     Circuit Breaker - stop calling failing service for a window.
    //     Bulkhead      - isolate resource pools so one slow dep doesn't drown all.
    //     Fallback      - return cached/default response.
    //     Library: Polly / Microsoft.Extensions.Http.Resilience (.NET 8+).

    // Q11: Idempotency — why it matters?
    // A : Retries can duplicate requests. Make operations idempotent via:
    //     - Natural idempotency (PUT, DELETE).
    //     - Idempotency-Key header + dedupe store.
    //     - Event handlers check "already processed?" before applying.

    // Q12: Eventual consistency?
    // A : Across services, you can't have a single ACID transaction.
    //     State converges over time via events. Embrace it; design UI to
    //     show "processing…" where appropriate.

    // ---------------------------------------------------------------------
    //  DATA & TRANSACTIONS
    // ---------------------------------------------------------------------

    // Q13: Saga pattern?
    // A : A long-running business transaction split into local steps with
    //     compensating actions on failure. Two styles:
    //     - Choreography : services react to each other's events. No central
    //                      coordinator. Simple, but harder to monitor.
    //     - Orchestration: a central saga orchestrator calls steps and
    //                      compensations. Easier to track and reason about.

    // Q14: Outbox pattern?
    // A : Save business state + outgoing event in the SAME DB transaction.
    //     A relay process reads the outbox and publishes events to the bus.
    //     Guarantees: never lose an event if the DB commit succeeded.

    // Q15: Inbox pattern (idempotent consumer)?
    // A : On consume, store the message id. If seen again -> skip.
    //     Pairs with Outbox to give exactly-once business effect.

    // Q16: CQRS — Command Query Responsibility Segregation?
    // A : Separate models for writes (Commands) and reads (Queries).
    //     Reads can be denormalized projections optimized for the view.
    //     Often paired with Event Sourcing, but not required.

    // Q17: Event Sourcing?
    // A : Persist a stream of immutable EVENTS as the source of truth.
    //     Current state = fold over events. Free audit trail + time-travel.
    //     Trade-offs: schema evolution, replay cost.

    // ---------------------------------------------------------------------
    //  OBSERVABILITY
    // ---------------------------------------------------------------------

    // Q18: Three pillars of observability?
    // A : Logs, Metrics, Traces. In .NET use OpenTelemetry to emit all
    //     three; export to App Insights / Jaeger / Prometheus + Grafana.

    // Q19: Distributed tracing?
    // A : One TraceId propagates across services via headers (W3C
    //     traceparent). Each operation = a span. Helps find which service
    //     in a chain caused the latency.

    // Q20: Correlation IDs?
    // A : An id (often = TraceId) attached to every log line for a request.
    //     ASP.NET Core: built-in Activity.TraceId. Add to log scopes:
    //
    //   /// using (logger.BeginScope(new { TraceId = Activity.Current?.TraceId }))
    //   /// { ... }

    // ---------------------------------------------------------------------
    //  DEPLOYMENT TOPOLOGIES
    // ---------------------------------------------------------------------

    // Q21: Sidecar pattern?
    // A : A helper container deployed alongside the app pod (logging,
    //     proxy, secrets fetcher). Examples: Envoy, Dapr, Istio sidecars.

    // Q22: Service mesh?
    // A : Infrastructure layer for service-to-service traffic: mTLS, retries,
    //     timeouts, traffic shifting, telemetry — without code changes.
    //     Examples: Istio, Linkerd, Consul Connect.

    // Q23: Dapr?
    // A : Distributed Application Runtime — sidecar exposing building blocks
    //     (pub/sub, state, secrets, bindings) via HTTP/gRPC. Polyglot-friendly.

    // ---------------------------------------------------------------------
    //  SCENARIOS
    // ---------------------------------------------------------------------

    // [Scenario] Q24: How would you decompose a monolith into microservices?
    // A : 1) Map domain (Event Storming) -> bounded contexts.
    //     2) Extract the seam: introduce an interface, run side-by-side.
    //     3) Use Strangler-Fig via a gateway/router.
    //     4) Move data last; introduce events to decouple writes.
    //     5) Add observability + automated tests before/after each step.

    // [Scenario] Q25: An order needs payment + inventory + shipping; how to
    //   keep them consistent?
    // A : Saga (orchestration). Each step publishes events; failures trigger
    //     compensating actions (refund payment, release inventory).

    // [Scenario] Q26: Service A calls Service B which is down for 1 hour.
    // A : Polly: timeout + retry-with-backoff + circuit breaker + fallback
    //     (return cached/stub). Surface the degradation in monitoring.

    // [Scenario] Q27: Two services need the same customer data.
    // A : Owner service exposes it via API. Consumers either:
    //     - Call API on demand (sync).
    //     - Subscribe to CustomerChanged events and keep a local read model.

    // [Scenario] Q28: Versioning microservice APIs?
    // A : Backward-compatible changes only (add fields, never remove/rename).
    //     If breaking: bump major version (/v2/...), run both, deprecate old
    //     after consumers migrate.

    // [Scenario] Q29: Where do you put cross-cutting concerns (auth, logging)?
    // A : Combine: gateway (auth, rate limit), shared NuGet/library
    //     (telemetry, correlation), service mesh (mTLS, retries).
    //     Don't duplicate the same logic in every service.

    // [Scenario] Q30: How to test a microservice in isolation?
    // A : - Contract tests (Pact) verify expectations of dependencies.
    //     - Integration tests use Testcontainers for real DB/bus.
    //     - WebApplicationFactory for in-proc HTTP tests.
    //     - Avoid full end-to-end as the only safety net (slow + flaky).

    internal static class _Micro { }
}
