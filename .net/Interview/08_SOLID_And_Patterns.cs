// =====================================================================
//  08) SOLID + Picking the Right Pattern (Interview Cheatsheet)
// =====================================================================
namespace Interview.Solid
{
    // ---------------------------------------------------------------------
    //  S  - Single Responsibility:  "A class should have ONE reason to change."
    //  O  - Open/Closed:             "Open for extension, closed for modification."
    //  L  - Liskov Substitution:     "Subclasses must be usable as base class."
    //  I  - Interface Segregation:   "Many small interfaces > one fat one."
    //  D  - Dependency Inversion:    "Depend on abstractions, not concretions."
    // ---------------------------------------------------------------------

    // [Scenario] Order class also sends emails + writes to disk.
    // -> SRP violation. Split: OrderService, IEmailSender, IOrderRepository.

    // [Scenario] Adding a new payment type forces edits to a giant switch.
    // -> OCP violation. Use Strategy: IPaymentProcessor implementations.

    // [Scenario] A Square inherits Rectangle and breaks tests setting width.
    // -> LSP violation. Prefer composition or a Shape abstraction.

    // [Scenario] IRepository has 30 methods; clients implement only 2.
    // -> ISP violation. Split into IReadRepo<T>, IWriteRepo<T>, etc.

    // [Scenario] Controller instantiates SqlConnection directly.
    // -> DIP violation. Inject IConnectionFactory; let DI choose the impl.

    // =====================================================================
    //  Quick "when to use which pattern?"
    // =====================================================================
    // • Need ONE shared instance?              -> Singleton
    // • Creation logic varies by input?        -> Factory / Abstract Factory
    // • Many optional params / steps?          -> Builder
    // • Cheap copy of expensive object?        -> Prototype
    // • Incompatible interface to integrate?   -> Adapter
    // • Add features at runtime?               -> Decorator
    // • Simplify complex subsystem?            -> Facade
    // • Lazy / access-controlled object?       -> Proxy
    // • Tree of part-whole objects?            -> Composite
    // • Two dimensions of variation?           -> Bridge
    // • Huge number of similar objects?        -> Flyweight
    // • Interchangeable algorithms?            -> Strategy
    // • Notify many on a change?               -> Observer
    // • Undo / queue of requests?              -> Command
    // • Custom traversal?                      -> Iterator
    // • Behavior depends on internal state?    -> State
    // • Shared algorithm, varying steps?       -> Template Method
    // • Process request through stages?        -> Chain of Responsibility
    // • Reduce N×N object coupling?            -> Mediator
    // • Undo snapshot?                         -> Memento
    // • Add ops to stable hierarchy?           -> Visitor
    // • Tiny DSL evaluator?                    -> Interpreter

    // =====================================================================
    //  Other "named" patterns asked about
    // =====================================================================
    // • Repository       - abstraction over data store (in EF, often optional).
    // • Unit of Work     - groups multiple repos under one SaveChanges/transaction.
    // • CQRS             - separate Command (writes) from Query (reads) models.
    // • Mediator (MediatR) - decouple controllers from handlers; popular in CQRS.
    // • Specification    - encapsulate query predicates as objects.
    // • Outbox           - reliable messaging: save event + state in same tx.
    // • Saga             - long-running, distributed workflows.
    // • Circuit Breaker  - stop calling a failing dependency (Polly).
    // • Retry / Backoff  - resilient calls with exponential backoff.
    // • Bulkhead         - isolate resource pools (Polly).

    // [Scenario] Microservice calls another that goes down for hours.
    //   What patterns? Retry with backoff + Circuit Breaker + Timeout + Fallback.
    //   Library? Polly / Microsoft.Extensions.Http.Resilience.

    // [Scenario] You need read-side optimizations distinct from write-side.
    //   Pattern? CQRS + projections (separate read DB / view model).

    internal static class _Solid { }
}
