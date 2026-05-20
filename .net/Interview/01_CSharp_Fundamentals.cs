// =====================================================================
//  01) C# FUNDAMENTALS — Interview Q&A
// =====================================================================
using System;
using System.Text;

namespace Interview.CSharpFundamentals
{
    // ---------------------------------------------------------------------
    // Q1: What are the 4 pillars of OOP?
    // A : Encapsulation, Abstraction, Inheritance, Polymorphism.
    //     - Encapsulation : hide state, expose behavior (private + props)
    //     - Abstraction   : expose "what", hide "how" (interfaces, abstract)
    //     - Inheritance   : derive class from another (code reuse)
    //     - Polymorphism  : same call, different behavior (virtual/override)
    // ---------------------------------------------------------------------

    // Q2: Difference between abstract class and interface?
    // A :  abstract class  | interface
    //      can have impl?  | (default methods possible from C#8)
    //      single inherit  | multiple inherit
    //      can have fields | no instance fields
    //      use when "is-a" | use when "can-do"
    public abstract class Animal { public abstract void Speak(); public void Sleep() { } }
    public interface IFlyable   { void Fly(); }
    public class Bird : Animal, IFlyable
    {
        public override void Speak() => Console.WriteLine("Tweet");
        public void Fly() => Console.WriteLine("Flying");
    }

    // ---------------------------------------------------------------------
    // Q3: Value type vs Reference type?
    // A : Value (struct, int, enum)   -> stored on stack, copied by value
    //     Reference (class, string,   -> stored on heap, copied by reference
    //                array, delegate)
    //     string is reference type but IMMUTABLE -> behaves like value.
    // ---------------------------------------------------------------------

    // Q4: What is boxing/unboxing? Cost?
    // A : Boxing   = wrapping a value type into object (heap allocation).
    //     Unboxing = extracting value type back from object (cast).
    //     Costly in hot loops; prefer generics to avoid it.
    public static class Boxing
    {
        public static void Demo()
        {
            int i = 5;
            object o = i;          // boxing
            int j = (int)o;        // unboxing
            Console.WriteLine(j);
        }
    }

    // ---------------------------------------------------------------------
    // Q5: ref vs out vs in?
    // A : ref - must be initialized before, can be modified
    //     out - need not be initialized, MUST be assigned inside method
    //     in  - passed by reference but READ-ONLY (perf for big structs)
    // ---------------------------------------------------------------------
    public static class RefOutIn
    {
        public static void DoRef(ref int x) => x++;
        public static void DoOut(out int x) => x = 10;
        public static int  DoIn(in int x) => x + 1;   // cannot modify x
    }

    // ---------------------------------------------------------------------
    // Q6: const vs readonly vs static readonly?
    // A : const           - compile-time, baked into callers (versioning trap)
    //     readonly        - set in ctor, per-instance
    //     static readonly - set in static ctor, shared across instances
    // ---------------------------------------------------------------------

    // Q7: Why string concatenation in a loop is slow?
    // A : string is immutable -> each "+" creates a NEW string + copies.
    //     Use StringBuilder for many concatenations.
    public static class StringPerf
    {
        public static string Bad(int n)
        {
            string s = "";
            for (int i = 0; i < n; i++) s += i;   // O(n^2)
            return s;
        }
        public static string Good(int n)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < n; i++) sb.Append(i);   // O(n)
            return sb.ToString();
        }
    }

    // ---------------------------------------------------------------------
    // Q8: == vs Equals vs ReferenceEquals?
    // A : == : for value types compares values; for reference types compares
    //          references (UNLESS operator== is overloaded, e.g. string).
    //     Equals          : virtual; classes override for value semantics.
    //     ReferenceEquals : always compares references, ignores overrides.
    // ---------------------------------------------------------------------

    // Q9: What is sealed?
    // A : Prevents further inheritance / override. Also enables JIT
    //     optimizations (devirtualization).

    // Q10: Why override GetHashCode when overriding Equals?
    // A : Hash-based collections (Dictionary, HashSet) require:
    //     "if a.Equals(b) then a.GetHashCode() == b.GetHashCode()".
    //     Skipping it -> lost items in dictionaries.

    // ---------------------------------------------------------------------
    // [Scenario] Q11: You override Equals in a class but Dictionary still
    //   returns "not found" for an equal key. Why?
    // A : You forgot to override GetHashCode (or hash uses mutable fields).
    // ---------------------------------------------------------------------

    // [Scenario] Q12: A struct is being passed around and changes "aren't
    //   sticking". Why?
    // A : Structs are copied by value. Either return the modified struct,
    //     pass by `ref`, or use a class.

    // [Scenario] Q13: Two strings created differently compare equal with ==
    //   but ReferenceEquals returns false. Why?
    // A : string overloads ==; if not interned, they live at different
    //     addresses but contents match.

    // [Scenario] Q14: A `const string ApiVersion = "1.0"` is in a NuGet
    //   library. You release v2.0 of the lib but consumers still see "1.0".
    //   Why?
    // A : const is INLINED at consumer's compile time. Use `static readonly`
    //     for values that may change across versions.

    // [Scenario] Q15: Why are exceptions expensive? When to use them?
    // A : Stack walk + object allocation + JIT deoptimization.
    //     Use for EXCEPTIONAL conditions, not for control flow.
}
