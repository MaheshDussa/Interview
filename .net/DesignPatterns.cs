// =====================================================================
//  DESIGN PATTERNS IN C# — Self-Explained with One-Line Memory
// =====================================================================
//  WHAT IS A DESIGN PATTERN?
//  A reusable, proven solution to a commonly occurring problem in
//  software design. Patterns are NOT code you copy — they are recipes
//  that you adapt to your own context.
//
//  THREE CATEGORIES (Gang of Four classification):
//    1) Creational  — HOW objects are created   (instantiation logic)
//    2) Structural  — HOW objects are composed  (relationships)
//    3) Behavioral  — HOW objects communicate   (interactions)
//
//  HOW THIS FILE IS ORGANIZED:
//    • Each pattern lives in its own namespace (DP.<PatternName>) so
//      class names like "Adapter" or "Composite" don't collide.
//    • Every pattern has:
//        - A one-line memory hint  (the "remember this" phrase)
//        - "Real world" analogy   (intuitive picture)
//        - "Use when"             (when to reach for it)
//        - Roles                  (who plays which part)
//        - A small runnable demo  (in DesignPatternsDemo.Run)
//
//  HOW TO RUN:
//    In Program.cs add:   DesignPatternsDemo.Run();
//    Then in the .net folder run:   dotnet run
// =====================================================================

using System;
using System.Collections;
using System.Collections.Generic;

// =====================================================================
// 1) CREATIONAL PATTERNS  —  control object creation
// =====================================================================

// ---------------------------------------------------------------------
//  SINGLETON
//  One-liner : "Only one instance, ever."
//  Analogy   : The President of a country — only one at a time.
//  Use when  : You need exactly ONE shared object (logger, config,
//              cache, connection pool) accessible globally.
//  Roles     : Singleton class itself (holds its own single instance).
//  Watch out : Hard to unit-test; can hide dependencies; consider DI.
// ---------------------------------------------------------------------
namespace DP.Singleton
{
    public sealed class Logger
    {
        // 'static readonly' = created once, when the type is first used.
        // 'sealed' prevents subclassing (which could break uniqueness).
        private static readonly Logger _instance = new Logger();

        // Private constructor: outsiders CANNOT do `new Logger()`.
        private Logger() { }

        // The only way to get the instance.
        public static Logger Instance => _instance;

        public void Log(string msg) => Console.WriteLine($"[LOG] {msg}");
    }
}

// ---------------------------------------------------------------------
//  FACTORY METHOD
//  One-liner : "Ask a method to create the object for you."
//  Analogy   : Ordering at a restaurant — you say "burger", the kitchen
//              decides HOW to make it. You never touch the stove.
//  Use when  : The exact class to instantiate depends on input/config,
//              and you don't want `new` scattered across your code.
//  Roles     : Product (abstract), ConcreteProduct(s), Factory method.
// ---------------------------------------------------------------------
namespace DP.Factory
{
    // Product — the common interface/base type returned by the factory.
    public abstract class Vehicle { public abstract void Drive(); }

    // Concrete products — variations the factory can produce.
    public class Car  : Vehicle { public override void Drive() => Console.WriteLine("Driving car"); }
    public class Bike : Vehicle { public override void Drive() => Console.WriteLine("Riding bike"); }

    // The Factory — encapsulates the `new` keyword in one place.
    public static class VehicleFactory
    {
        public static Vehicle Create(string type) => type switch
        {
            "car"  => new Car(),
            "bike" => new Bike(),
            _ => throw new ArgumentException("Unknown vehicle")
        };
    }
}

// ---------------------------------------------------------------------
//  ABSTRACT FACTORY
//  One-liner : "A factory of factories — creates FAMILIES of related objects."
//  Analogy   : IKEA furniture sets — a "Modern" set gives modern chair
//              + modern table; a "Victorian" set gives matching pieces.
//              Pieces from different families don't mix.
//  Use when  : You need to create groups of related objects that must
//              be used together (e.g., UI controls for Windows vs Mac).
//  Roles     : AbstractFactory, ConcreteFactories, AbstractProducts,
//              ConcreteProducts.
// ---------------------------------------------------------------------
namespace DP.AbstractFactory
{
    // Abstract products — the "categories" of items each factory makes.
    public interface IButton   { void Render(); }
    public interface ICheckbox { void Render(); }

    // Concrete products grouped into families: Windows family...
    public class WinButton   : IButton   { public void Render() => Console.WriteLine("Win Button"); }
    public class WinCheckbox : ICheckbox { public void Render() => Console.WriteLine("Win Checkbox"); }
    // ...and Mac family.
    public class MacButton   : IButton   { public void Render() => Console.WriteLine("Mac Button"); }
    public class MacCheckbox : ICheckbox { public void Render() => Console.WriteLine("Mac Checkbox"); }

    // Abstract factory — declares creation methods for each product type.
    public interface IGUIFactory { IButton CreateButton(); ICheckbox CreateCheckbox(); }

    // Each concrete factory produces ONE consistent family of products.
    public class WinFactory : IGUIFactory
    {
        public IButton   CreateButton()   => new WinButton();
        public ICheckbox CreateCheckbox() => new WinCheckbox();
    }
    public class MacFactory : IGUIFactory
    {
        public IButton   CreateButton()   => new MacButton();
        public ICheckbox CreateCheckbox() => new MacCheckbox();
    }
}

// ---------------------------------------------------------------------
//  BUILDER
//  One-liner : "Build complex objects step-by-step."
//  Analogy   : Building a Subway sandwich — choose bread, then sauce,
//              then toppings. Same process, many possible results.
//  Use when  : An object has many optional parts / configurations and
//              huge constructors become unreadable ("telescoping ctor").
//  Roles     : Product, Builder (fluent), optional Director.
// ---------------------------------------------------------------------
namespace DP.Builder
{
    // The Product being built (could be much more complex in real life).
    public class Pizza
    {
        public string Dough = "", Sauce = "", Topping = "";
        public override string ToString() => $"Pizza({Dough}, {Sauce}, {Topping})";
    }

    // The Builder — each "WithX" returns `this` to enable method chaining.
    public class PizzaBuilder
    {
        private Pizza _p = new();
        public PizzaBuilder WithDough(string d)   { _p.Dough = d;   return this; }
        public PizzaBuilder WithSauce(string s)   { _p.Sauce = s;   return this; }
        public PizzaBuilder WithTopping(string t) { _p.Topping = t; return this; }
        public Pizza Build() => _p;     // Final step returns the finished product.
    }
}

// ---------------------------------------------------------------------
//  PROTOTYPE
//  One-liner : "Clone existing object instead of creating new."
//  Analogy   : Photocopying a document instead of retyping it.
//  Use when  : Creating a new object is expensive (DB load, heavy
//              computation) and you already have a similar one.
//  Roles     : Prototype interface with Clone(), concrete prototypes.
// ---------------------------------------------------------------------
namespace DP.Prototype
{
    public class Person : ICloneable
    {
        public string Name = "";
        public int Age;

        // MemberwiseClone = shallow copy of all fields. For deep copies
        // (when fields are reference types) you copy those manually.
        public object Clone() => MemberwiseClone();

        public override string ToString() => $"{Name}, {Age}";
    }
}

// =====================================================================
// 2) STRUCTURAL PATTERNS  —  compose classes/objects into bigger structures
// =====================================================================

// ---------------------------------------------------------------------
//  ADAPTER
//  One-liner : "Plug converter — makes incompatible interfaces work together."
//  Analogy   : EU-to-US power plug adapter — different shapes, same goal.
//  Use when  : You want to use an existing class but its interface
//              doesn't match what your code expects (e.g., 3rd-party lib).
//  Roles     : Target (what client expects), Adaptee (existing class
//              with wrong interface), Adapter (bridges the two).
// ---------------------------------------------------------------------
namespace DP.Adapter
{
    // Target — the interface the client code wants to call.
    public interface ITarget { string Request(); }

    // Adaptee — existing class with a DIFFERENT (incompatible) method name.
    public class Adaptee { public string SpecificRequest() => "Specific behavior"; }

    // Adapter — implements Target, but delegates to the Adaptee inside.
    public class Adapter : ITarget
    {
        private readonly Adaptee _a = new();
        public string Request() => $"Adapter -> {_a.SpecificRequest()}";
    }
}

// ---------------------------------------------------------------------
//  DECORATOR
//  One-liner : "Wrap to add features dynamically."
//  Analogy   : Putting toppings on ice cream — each topping wraps the
//              previous combo, adding flavor without changing the cone.
//  Use when  : You want to add responsibilities to objects at runtime
//              without modifying their class or making subclasses for
//              every combination (e.g., "Coffee + Milk + Sugar + Vanilla").
//  Roles     : Component interface, ConcreteComponent, Decorator(s)
//              implementing the same interface and holding a Component.
// ---------------------------------------------------------------------
namespace DP.Decorator
{
    public interface ICoffee { decimal Cost(); string Desc(); }

    // Base, plain object.
    public class SimpleCoffee : ICoffee
    {
        public decimal Cost() => 5m;
        public string Desc()  => "Coffee";
    }

    // Decorator — implements the SAME interface AND wraps an ICoffee.
    public class MilkDecorator : ICoffee
    {
        private readonly ICoffee _c;                    // The thing being wrapped.
        public MilkDecorator(ICoffee c) => _c = c;
        public decimal Cost() => _c.Cost() + 2m;        // Add to inner cost.
        public string Desc()  => _c.Desc() + " + Milk"; // Add to description.
    }
}

// ---------------------------------------------------------------------
//  FACADE
//  One-liner : "One simple door to a complex system."
//  Analogy   : A car's ignition button — one press triggers fuel pump,
//              starter, ECU, etc. You don't care about the details.
//  Use when  : A subsystem has many moving parts and clients only need
//              a simple high-level API.
//  Roles     : Facade (the simple API), Subsystem classes (the complexity).
// ---------------------------------------------------------------------
namespace DP.Facade
{
    // Subsystem parts (kept internal/simple here for the demo).
    class CPU    { public void Boot() => Console.WriteLine("CPU boot"); }
    class Memory { public void Load() => Console.WriteLine("Memory load"); }
    class Disk   { public void Read() => Console.WriteLine("Disk read"); }

    // Facade — the client just calls Computer.Start().
    public class Computer
    {
        public void Start()
        {
            new CPU().Boot();
            new Memory().Load();
            new Disk().Read();
        }
    }
}

// ---------------------------------------------------------------------
//  PROXY
//  One-liner : "Stand-in that controls access to the real object."
//  Analogy   : A credit card is a proxy for your bank account — same
//              effect (paying), but adds checks, limits, logging.
//  Use when  : You want lazy loading, access control, caching, logging,
//              or remote access — without changing the real object.
//  Roles     : Subject (common interface), RealSubject, Proxy.
// ---------------------------------------------------------------------
namespace DP.Proxy
{
    public interface IImage { void Display(); }

    // Heavy/expensive object — we want to delay creating it.
    public class RealImage : IImage
    {
        public RealImage() => Console.WriteLine("Loading heavy image...");
        public void Display() => Console.WriteLine("Displaying image");
    }

    // Proxy — same interface, but only creates RealImage on first use.
    public class ImageProxy : IImage
    {
        private RealImage? _img;
        public void Display()
        {
            _img ??= new RealImage();   // Lazy init: create on demand.
            _img.Display();
        }
    }
}

// ---------------------------------------------------------------------
//  COMPOSITE
//  One-liner : "Treat group of objects same as single object (tree)."
//  Analogy   : A folder contains files AND other folders — but in code
//              you can call "size" / "show" on either uniformly.
//  Use when  : You have part-whole hierarchies (UI trees, file systems,
//              org charts) and want to ignore the difference between
//              individual items and groups.
//  Roles     : Component interface, Leaf (no children), Composite
//              (has children, all of type Component).
// ---------------------------------------------------------------------
namespace DP.Composite
{
    // Common interface — both leaves and groups implement Show().
    public interface IComponent { void Show(string indent = ""); }

    // Leaf — terminal node, has no children.
    public class Leaf : IComponent
    {
        private readonly string _name;
        public Leaf(string name) => _name = name;
        public void Show(string indent = "") => Console.WriteLine($"{indent}- {_name}");
    }

    // Composite — node that contains other IComponents (leaves or composites).
    public class Composite : IComponent
    {
        private readonly string _name;
        private readonly List<IComponent> _kids = new();
        public Composite(string name) => _name = name;
        public void Add(IComponent c) => _kids.Add(c);
        public void Show(string indent = "")
        {
            Console.WriteLine($"{indent}+ {_name}");
            foreach (var k in _kids) k.Show(indent + "  ");   // Recursive walk.
        }
    }
}

// ---------------------------------------------------------------------
//  BRIDGE
//  One-liner : "Split abstraction from implementation so both evolve independently."
//  Analogy   : A TV remote (abstraction) works with any TV brand
//              (implementation). Add new remotes OR new TVs separately.
//  Use when  : You have two dimensions of variation and don't want a
//              class explosion (e.g., Shape × Renderer = N×M classes).
//  Roles     : Abstraction (holds an Implementor), RefinedAbstraction,
//              Implementor (interface), ConcreteImplementors.
// ---------------------------------------------------------------------
namespace DP.Bridge
{
    // Implementor side — the "how it's drawn" dimension.
    public interface IRenderer { void Render(string shape); }
    public class VectorRenderer : IRenderer { public void Render(string s) => Console.WriteLine($"Vector {s}"); }
    public class RasterRenderer : IRenderer { public void Render(string s) => Console.WriteLine($"Raster {s}"); }

    // Abstraction side — the "what shape" dimension. Holds a renderer.
    public abstract class Shape
    {
        protected readonly IRenderer R;
        protected Shape(IRenderer r) => R = r;
        public abstract void Draw();
    }
    public class Circle : Shape
    {
        public Circle(IRenderer r) : base(r) { }
        public override void Draw() => R.Render("Circle");
    }
}

// ---------------------------------------------------------------------
//  FLYWEIGHT
//  One-liner : "Share common data to save memory."
//  Analogy   : In a forest game, thousands of trees share the SAME
//              texture/mesh data — only their position differs.
//  Use when  : You'd otherwise create huge numbers of similar objects
//              and run out of memory (text glyphs, particles, tiles).
//  Roles     : Flyweight (shared intrinsic state), Factory (caches them),
//              client supplies extrinsic state (e.g., position).
// ---------------------------------------------------------------------
namespace DP.Flyweight
{
    // The shared, immutable "intrinsic" data.
    public class TreeType
    {
        public string Name, Color;
        public TreeType(string n, string c) { Name = n; Color = c; }
        public override string ToString() => $"{Name}/{Color}";
    }

    // Factory ensures one TreeType per (name,color) — reused everywhere.
    public static class TreeFactory
    {
        private static readonly Dictionary<string, TreeType> _cache = new();
        public static TreeType Get(string name, string color)
        {
            var key = $"{name}-{color}";
            if (!_cache.TryGetValue(key, out var t))
                _cache[key] = t = new TreeType(name, color);   // Create once.
            return t;                                          // Reuse forever.
        }
        public static int CacheSize => _cache.Count;
    }
}

// =====================================================================
// 3) BEHAVIORAL PATTERNS  —  manage communication between objects
// =====================================================================

// ---------------------------------------------------------------------
//  STRATEGY
//  One-liner : "Swap algorithms at runtime."
//  Analogy   : Google Maps lets you pick driving/walking/transit —
//              same goal (route), different algorithms.
//  Use when  : You have multiple ways to do the same task and want to
//              choose/replace them at runtime (sorting, pricing, AI).
//  Roles     : Strategy interface, ConcreteStrategies, Context that
//              holds a Strategy reference and delegates to it.
// ---------------------------------------------------------------------
namespace DP.Strategy
{
    public interface ISortStrategy { void Sort(int[] a); }

    public class QuickSort  : ISortStrategy
    {
        public void Sort(int[] a) { Array.Sort(a); Console.WriteLine("Quick sorted"); }
    }
    public class BubbleSort : ISortStrategy
    {
        public void Sort(int[] a)
        {
            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < a.Length - 1 - i; j++)
                    if (a[j] > a[j + 1]) (a[j], a[j + 1]) = (a[j + 1], a[j]);
            Console.WriteLine("Bubble sorted");
        }
    }

    // Context — doesn't know HOW sorting happens, only that it does.
    public class Sorter
    {
        private readonly ISortStrategy _s;
        public Sorter(ISortStrategy s) => _s = s;
        public void Run(int[] a) => _s.Sort(a);
    }
}

// ---------------------------------------------------------------------
//  OBSERVER
//  One-liner : "Subscribe and get notified when subject changes."
//  Analogy   : YouTube channel — subscribers get notified when a new
//              video is published, without the channel knowing them.
//  Use when  : One object's state change must update many others, and
//              you want loose coupling between them (events, MVC).
//  Roles     : Subject (publisher), Observers (subscribers).
//  Note      : In C#, `event` + `Action`/`EventHandler` is built-in support.
// ---------------------------------------------------------------------
namespace DP.Observer
{
    public class Publisher
    {
        // event = a list of subscribers managed by the runtime.
        public event Action<string>? OnNews;

        // Notify all subscribers; ?. handles the "no subscribers" case.
        public void Publish(string n) => OnNews?.Invoke(n);
    }
}

// ---------------------------------------------------------------------
//  COMMAND
//  One-liner : "Wrap a request as an object (so you can queue/undo/log it)."
//  Analogy   : A restaurant order ticket — captures who wants what.
//              The kitchen can queue, cancel, or replay tickets.
//  Use when  : You need undo/redo, task queues, macro recording, or
//              decoupling "what to do" from "when/where to do it".
//  Roles     : Command interface, ConcreteCommand, Receiver (real worker),
//              Invoker (triggers commands), Client (assembles them).
// ---------------------------------------------------------------------
namespace DP.Command
{
    public interface ICommand { void Execute(); }

    // Receiver — the object that actually does the work.
    public class Light { public void On() => Console.WriteLine("Light ON"); }

    // Concrete command — binds a receiver to an action.
    public class LightOnCommand : ICommand
    {
        private readonly Light _l;
        public LightOnCommand(Light l) => _l = l;
        public void Execute() => _l.On();
    }
}

// ---------------------------------------------------------------------
//  ITERATOR
//  One-liner : "Walk through a collection without exposing its internals."
//  Analogy   : A TV remote's channel-up button — you don't know how
//              channels are stored, you just go to the next one.
//  Use when  : You need uniform traversal of different collections
//              (array, list, tree) or want to hide internal structure.
//  Roles     : Iterator (has MoveNext/Current), Aggregate (creates iterators).
//  Note      : C# bakes this in via IEnumerable<T> + `yield return`.
// ---------------------------------------------------------------------
namespace DP.Iterator
{
    public class NumberCollection : IEnumerable<int>
    {
        private readonly int[] _n = { 1, 2, 3, 4 };

        // `yield return` builds an iterator state machine automatically.
        public IEnumerator<int> GetEnumerator()
        {
            foreach (var i in _n) yield return i;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

// ---------------------------------------------------------------------
//  STATE
//  One-liner : "Object behavior changes when its internal state changes."
//  Analogy   : A traffic light — same object, but Red/Yellow/Green
//              each respond differently to "next()".
//  Use when  : You have lots of if/switch on a "mode" field — replace
//              them with state objects that know their own behavior.
//  Roles     : State interface, ConcreteStates, Context (holds current state).
// ---------------------------------------------------------------------
namespace DP.State
{
    public interface IState { void Handle(Context c); }

    // Context delegates to whichever state it currently holds.
    public class Context
    {
        public IState State;
        public Context(IState s) => State = s;
        public void Request() => State.Handle(this);
    }

    // Each state knows what to do AND which state comes next.
    public class StateA : IState
    {
        public void Handle(Context c) { Console.WriteLine("State A -> B"); c.State = new StateB(); }
    }
    public class StateB : IState
    {
        public void Handle(Context c) { Console.WriteLine("State B -> A"); c.State = new StateA(); }
    }
}

// ---------------------------------------------------------------------
//  TEMPLATE METHOD
//  One-liner : "Define skeleton, let subclasses fill in steps."
//  Analogy   : A recipe template: boil water -> add ingredient -> serve.
//              The "ingredient" step is filled by tea/coffee/soup subclasses.
//  Use when  : Multiple algorithms share the same overall structure but
//              differ in specific steps. Avoids duplicated control flow.
//  Roles     : Abstract class with the template method (final-ish) and
//              abstract "hook" steps; subclasses override only the hooks.
// ---------------------------------------------------------------------
namespace DP.Template
{
    public abstract class Report
    {
        // The template method — fixed order, never overridden.
        public void Generate() { Header(); Body(); Footer(); }

        protected void Header()           => Console.WriteLine("== Header ==");
        protected abstract void Body();   // Subclass-specific step.
        protected void Footer()           => Console.WriteLine("== Footer ==");
    }

    public class SalesReport : Report
    {
        protected override void Body() => Console.WriteLine("Sales: $1000");
    }
}

// ---------------------------------------------------------------------
//  CHAIN OF RESPONSIBILITY
//  One-liner : "Pass request along a chain until someone handles it."
//  Analogy   : Tech support escalation — Tier 1 tries, else passes to
//              Tier 2, else Tier 3. Each level decides itself.
//  Use when  : Multiple objects might handle a request and you don't
//              want the sender to know which one (logging filters,
//              middleware pipelines, event bubbling).
//  Roles     : Handler base with `Next` link, ConcreteHandlers.
// ---------------------------------------------------------------------
namespace DP.Chain
{
    public abstract class Handler
    {
        protected Handler? Next;

        // Returns the next handler so you can chain calls fluently.
        public Handler SetNext(Handler h) { Next = h; return h; }
        public abstract void Handle(int req);
    }

    public class LowHandler : Handler
    {
        public override void Handle(int req)
        {
            if (req < 5) Console.WriteLine($"Low handled {req}");
            else Next?.Handle(req);          // Pass it along.
        }
    }
    public class HighHandler : Handler
    {
        public override void Handle(int req) => Console.WriteLine($"High handled {req}");
    }
}

// ---------------------------------------------------------------------
//  MEDIATOR
//  One-liner : "Central hub — objects talk through it, not directly."
//  Analogy   : Air traffic control tower — planes don't call each other,
//              they all coordinate through the tower.
//  Use when  : Many objects communicate in complex ways (UI dialogs with
//              many controls). Reduces N×N coupling to N×1.
//  Roles     : Mediator interface, ConcreteMediator, Colleagues.
// ---------------------------------------------------------------------
namespace DP.Mediator
{
    public interface IMediator { void Notify(string sender, string ev); }

    public class ChatRoom : IMediator
    {
        // The hub decides how to route/log/transform messages.
        public void Notify(string sender, string ev) => Console.WriteLine($"{sender}: {ev}");
    }
}

// ---------------------------------------------------------------------
//  MEMENTO
//  One-liner : "Snapshot for undo."
//  Analogy   : Saving a game checkpoint — you can restore later.
//  Use when  : You need undo/history without exposing the object's
//              internal fields to the outside.
//  Roles     : Originator (creates/restores memento), Memento (snapshot),
//              Caretaker (keeps mementos but doesn't peek inside).
// ---------------------------------------------------------------------
namespace DP.Memento
{
    // The snapshot — opaque to the outside world (keep it simple here).
    public class Memento { public string State = ""; }

    public class Editor
    {
        public string Text = "";
        public Memento Save() => new() { State = Text };       // Capture state.
        public void Restore(Memento m) => Text = m.State;      // Roll back.
    }
}

// ---------------------------------------------------------------------
//  VISITOR
//  One-liner : "Add new operations without changing the classes."
//  Analogy   : A tax inspector visits each shop and computes a report.
//              The shops don't change — the inspector brings the logic.
//  Use when  : You have a stable set of element classes but keep adding
//              new operations over them (compilers, AST tools).
//  Roles     : Visitor (Visit overloads), Elements with Accept(visitor).
// ---------------------------------------------------------------------
namespace DP.Visitor
{
    public interface IVisitor { void Visit(Book b); }

    public class Book
    {
        public decimal Price = 20m;
        // Element offers itself to the visitor — "double dispatch".
        public void Accept(IVisitor v) => v.Visit(this);
    }

    // To add a new operation, write a new IVisitor — no changes to Book.
    public class PricePrinter : IVisitor
    {
        public void Visit(Book b) => Console.WriteLine($"Book price: {b.Price}");
    }
}

// ---------------------------------------------------------------------
//  INTERPRETER
//  One-liner : "Define a grammar and interpret sentences in it."
//  Analogy   : A calculator parsing "2 + 3 * 4" — each piece (number,
//              operator) knows how to evaluate itself.
//  Use when  : You have a small, well-defined language (filters, rules,
//              DSLs). For anything big, use a real parser generator.
//  Roles     : AbstractExpression, Terminal & NonTerminal expressions.
// ---------------------------------------------------------------------
namespace DP.Interpreter
{
    public interface IExpr { int Interpret(); }

    // Terminal expression — leaf node, evaluates directly.
    public class Num : IExpr
    {
        private readonly int _v;
        public Num(int v) => _v = v;
        public int Interpret() => _v;
    }

    // Non-terminal expression — composed of other expressions.
    public class Add : IExpr
    {
        private readonly IExpr _l, _r;
        public Add(IExpr l, IExpr r) { _l = l; _r = r; }
        public int Interpret() => _l.Interpret() + _r.Interpret();
    }
}

// =====================================================================
// DEMO RUNNER
//   Call DesignPatternsDemo.Run() from Program.cs to see every pattern
//   in action with labeled console output.
// =====================================================================
public static class DesignPatternsDemo
{
    public static void Run()
    {
        // ----- Creational -----
        Console.WriteLine("\n--- Singleton ---");
        // Always the same instance, accessed via .Instance
        DP.Singleton.Logger.Instance.Log("hello");

        Console.WriteLine("\n--- Factory ---");
        // Client asks for a "car" without knowing the Car class exists.
        DP.Factory.VehicleFactory.Create("car").Drive();

        Console.WriteLine("\n--- Abstract Factory ---");
        // Switching factories swaps the WHOLE family of products at once.
        DP.AbstractFactory.IGUIFactory gui = new DP.AbstractFactory.MacFactory();
        gui.CreateButton().Render();
        gui.CreateCheckbox().Render();

        Console.WriteLine("\n--- Builder ---");
        // Fluent chain reads almost like English.
        var pizza = new DP.Builder.PizzaBuilder()
            .WithDough("thin").WithSauce("tomato").WithTopping("cheese").Build();
        Console.WriteLine(pizza);

        Console.WriteLine("\n--- Prototype ---");
        // Clone() gives a new object identical to the source.
        var p1 = new DP.Prototype.Person { Name = "Alice", Age = 30 };
        var p2 = (DP.Prototype.Person)p1.Clone();
        Console.WriteLine($"{p1} | clone: {p2}");

        // ----- Structural -----
        Console.WriteLine("\n--- Adapter ---");
        // Client only knows ITarget; the Adapter hides the Adaptee.
        DP.Adapter.ITarget t = new DP.Adapter.Adapter();
        Console.WriteLine(t.Request());

        Console.WriteLine("\n--- Decorator ---");
        // Wrap a SimpleCoffee with a MilkDecorator to add behavior.
        DP.Decorator.ICoffee c = new DP.Decorator.MilkDecorator(new DP.Decorator.SimpleCoffee());
        Console.WriteLine($"{c.Desc()} = {c.Cost()}");

        Console.WriteLine("\n--- Facade ---");
        // One call hides three subsystem calls.
        new DP.Facade.Computer().Start();

        Console.WriteLine("\n--- Proxy ---");
        // First Display() creates the real object; second reuses it.
        DP.Proxy.IImage img = new DP.Proxy.ImageProxy();
        img.Display(); img.Display();

        Console.WriteLine("\n--- Composite ---");
        // A tree with leaves and a sub-folder — Show() is uniform.
        var root = new DP.Composite.Composite("root");
        root.Add(new DP.Composite.Leaf("file1"));
        var sub = new DP.Composite.Composite("folder");
        sub.Add(new DP.Composite.Leaf("file2"));
        root.Add(sub);
        root.Show();

        Console.WriteLine("\n--- Bridge ---");
        // Same Circle, different renderers — two dimensions vary independently.
        new DP.Bridge.Circle(new DP.Bridge.VectorRenderer()).Draw();
        new DP.Bridge.Circle(new DP.Bridge.RasterRenderer()).Draw();

        Console.WriteLine("\n--- Flyweight ---");
        // Same (name,color) request returns the SAME instance.
        var t1 = DP.Flyweight.TreeFactory.Get("Oak", "Green");
        var t2 = DP.Flyweight.TreeFactory.Get("Oak", "Green");
        Console.WriteLine($"same? {ReferenceEquals(t1, t2)} cache={DP.Flyweight.TreeFactory.CacheSize}");

        // ----- Behavioral -----
        Console.WriteLine("\n--- Strategy ---");
        // Swap QuickSort for BubbleSort with no change to Sorter.
        var arr = new[] { 3, 1, 2 };
        new DP.Strategy.Sorter(new DP.Strategy.QuickSort()).Run(arr);
        Console.WriteLine(string.Join(",", arr));

        Console.WriteLine("\n--- Observer ---");
        // Subscribe via +=, then publish — all subscribers get notified.
        var pub = new DP.Observer.Publisher();
        pub.OnNews += msg => Console.WriteLine($"Sub1 got: {msg}");
        pub.Publish("breaking news");

        Console.WriteLine("\n--- Command ---");
        // The "what to do" is now an object you can queue or undo.
        DP.Command.ICommand cmd = new DP.Command.LightOnCommand(new DP.Command.Light());
        cmd.Execute();

        Console.WriteLine("\n--- Iterator ---");
        // foreach uses GetEnumerator() — internals stay hidden.
        foreach (var n in new DP.Iterator.NumberCollection()) Console.Write(n + " ");
        Console.WriteLine();

        Console.WriteLine("\n--- State ---");
        // Each Request flips the internal state — behavior changes accordingly.
        var ctx = new DP.State.Context(new DP.State.StateA());
        ctx.Request(); ctx.Request(); ctx.Request();

        Console.WriteLine("\n--- Template ---");
        // Header/Footer come from the base; Body is the subclass's choice.
        new DP.Template.SalesReport().Generate();

        Console.WriteLine("\n--- Chain ---");
        // 3 is handled by Low; 10 escalates to High.
        var low = new DP.Chain.LowHandler();
        low.SetNext(new DP.Chain.HighHandler());
        low.Handle(3); low.Handle(10);

        Console.WriteLine("\n--- Mediator ---");
        // All communication is routed through ChatRoom.
        new DP.Mediator.ChatRoom().Notify("Alice", "Hi");

        Console.WriteLine("\n--- Memento ---");
        // Save -> change -> restore: classic undo.
        var ed = new DP.Memento.Editor { Text = "v1" };
        var snap = ed.Save();
        ed.Text = "v2";
        Console.WriteLine($"before restore: {ed.Text}");
        ed.Restore(snap);
        Console.WriteLine($"after restore:  {ed.Text}");

        Console.WriteLine("\n--- Visitor ---");
        // PricePrinter is a new operation; Book wasn't modified.
        new DP.Visitor.Book().Accept(new DP.Visitor.PricePrinter());

        Console.WriteLine("\n--- Interpreter ---");
        // Build "2 + 3" as a tree of expressions, then evaluate it.
        DP.Interpreter.IExpr expr =
            new DP.Interpreter.Add(new DP.Interpreter.Num(2), new DP.Interpreter.Num(3));
        Console.WriteLine($"2 + 3 = {expr.Interpret()}");
    }
}
