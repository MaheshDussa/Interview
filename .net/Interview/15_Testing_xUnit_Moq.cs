// =====================================================================
//  15) TESTING (xUnit, Moq, Integration, BenchmarkDotNet) — Q&A
// =====================================================================
namespace Interview.Testing
{
    // ---------------------------------------------------------------------
    //  TYPES OF TESTS
    // ---------------------------------------------------------------------
    // • Unit          - one class, dependencies mocked. Milliseconds.
    // • Integration   - real DB / HTTP / queue, but in-process or container.
    // • End-to-end    - full system via UI/API. Slow, flaky if overused.
    // • Contract      - service A expects shape X from service B (Pact).
    // • Performance   - BenchmarkDotNet (micro), k6/JMeter (load).
    //
    // Test Pyramid: many unit < some integration < few E2E.
    // ---------------------------------------------------------------------

    // Q1: xUnit vs NUnit vs MSTest?
    // A : Functionally similar. xUnit is the de-facto choice for .NET Core+
    //     (clean syntax, parallel by default). Attributes:
    //         [Fact]               - single test
    //         [Theory] + [InlineData] - parameterized
    //         IClassFixture<T>     - shared setup per class
    //         ICollectionFixture<T> - shared setup across classes
    //         [Trait("cat","db")]  - filter at run time

    // Q2: AAA pattern?
    // A : Arrange - set up data + mocks.
    //     Act     - call the method under test.
    //     Assert  - verify outcome (state OR interactions).

    // Q3: FluentAssertions — why?
    // A : Readable assertions:
    //         result.Should().Be(42);
    //         users.Should().ContainSingle(u => u.IsAdmin);
    //         action.Should().Throw<ArgumentNullException>();

    // ---------------------------------------------------------------------
    //  MOCKING (Moq)
    // ---------------------------------------------------------------------

    // Q4: Stub vs Mock vs Fake vs Spy?
    // A : Stub  - returns canned data.
    //     Mock  - verifies interactions (Verify(...)).
    //     Fake  - working impl with shortcuts (InMemoryDb).
    //     Spy   - records calls for inspection.

    // Q5: Moq basics.
    //   /// var repo = new Mock<IUserRepo>();
    //   /// repo.Setup(r => r.GetAsync(1)).ReturnsAsync(new User { Id = 1 });
    //   /// var svc = new UserService(repo.Object);
    //   /// var u   = await svc.LoadAsync(1);
    //   /// u.Id.Should().Be(1);
    //   /// repo.Verify(r => r.GetAsync(1), Times.Once);

    // Q6: When NOT to mock?
    // A : Don't mock types you don't own (3rd-party SDKs) -> wrap them
    //     behind your own interface, mock that. Don't mock value objects.
    //     Don't mock the system under test.

    // Q7: NSubstitute / FakeItEasy?
    // A : Alternative mocking libraries with simpler syntax than Moq.

    // ---------------------------------------------------------------------
    //  TESTING ASP.NET CORE
    // ---------------------------------------------------------------------

    // Q8: WebApplicationFactory<TProgram> — what does it do?
    // A : Boots your real ASP.NET Core app in-memory for integration tests.
    //     Test client calls real endpoints; you can override services.
    //
    //   /// public class MyApiFactory : WebApplicationFactory<Program>
    //   /// {
    //   ///     protected override void ConfigureWebHost(IWebHostBuilder b) =>
    //   ///         b.ConfigureServices(s => {
    //   ///             s.RemoveAll<DbContextOptions<AppDb>>();
    //   ///             s.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=:memory:"));
    //   ///         });
    //   /// }
    //   ///
    //   /// public class OrdersTests : IClassFixture<MyApiFactory>
    //   /// {
    //   ///     private readonly HttpClient _client;
    //   ///     public OrdersTests(MyApiFactory f) => _client = f.CreateClient();
    //   ///
    //   ///     [Fact] public async Task Get_Returns_200()
    //   ///     {
    //   ///         var r = await _client.GetAsync("/orders");
    //   ///         r.StatusCode.Should().Be(HttpStatusCode.OK);
    //   ///     }
    //   /// }

    // Q9: Make Program.cs testable.
    // A : Top-level Program in .NET 6+ — add `public partial class Program {}`
    //     at the bottom so WebApplicationFactory<Program> can see it.

    // Q10: Testing EF Core — options?
    // A : - UseInMemoryDatabase: fast but NOT real SQL (no transactions,
    //                            no relational constraints).
    //     - SQLite in-memory   : closer to real SQL, supports transactions.
    //     - Testcontainers     : real SQL Server/Postgres in Docker for tests.
    //     Prefer Testcontainers for parity with production.

    // Q11: Snapshot testing?
    // A : Verify (Verify.Xunit) - assert object/JSON matches a stored
    //     snapshot file. Great for response shape regressions.

    // ---------------------------------------------------------------------
    //  ASYNC TESTING
    // ---------------------------------------------------------------------

    // Q12: How to test exceptions in async code?
    //   /// var act = async () => await svc.DoAsync();
    //   /// await act.Should().ThrowAsync<InvalidOperationException>();

    // Q13: Time in tests?
    // A : Inject ITimeProvider / TimeProvider (.NET 8+). Avoid DateTime.UtcNow
    //     directly so tests can control "now".

    // ---------------------------------------------------------------------
    //  BEST PRACTICES
    // ---------------------------------------------------------------------

    // Q14: Test naming?
    // A : Method_State_ExpectedBehavior. Examples:
    //         Login_WithInvalidPassword_Returns401
    //         ApplyDiscount_WhenAmountNegative_Throws

    // Q15: One assertion per test — strict rule?
    // A : Not strict; multiple related assertions (Should().BeEquivalentTo)
    //     are fine. The point: each test should fail for ONE reason.

    // Q16: How to keep tests fast?
    // A : - Avoid network calls.
    //     - Use in-memory or reused DB per fixture.
    //     - Parallelize ([Collection] groups things that conflict).
    //     - Avoid Thread.Sleep; use polling with timeouts.

    // Q17: Code coverage — meaningful?
    // A : Useful directionally. 100% coverage ≠ correct. Focus on:
    //     critical paths, branches, business logic.
    //     Tools: Coverlet + ReportGenerator.

    // Q18: BenchmarkDotNet for performance.
    //   /// [MemoryDiagnoser]
    //   /// public class StringConcatBench
    //   /// {
    //   ///     [Benchmark] public string Concat()  => "a"+"b"+"c";
    //   ///     [Benchmark] public string Builder() => new StringBuilder().Append("a").Append("b").Append("c").ToString();
    //   /// }
    //   /// // BenchmarkRunner.Run<StringConcatBench>();

    // ---------------------------------------------------------------------
    //  SCENARIOS
    // ---------------------------------------------------------------------

    // [Scenario] Q19: A test passes alone but fails in the suite.
    // A : Shared mutable state (static fields, singletons, DB rows).
    //     Reset state per test; use isolated DB / unique tenant keys.

    // [Scenario] Q20: Tests are flaky against the message bus.
    // A : Don't depend on real ordering/timing. Use Testcontainers + poll
    //     with deadlines; or test handlers directly with synthetic messages.

    // [Scenario] Q21: You inherit untested legacy code. Where to start?
    // A : Add characterization tests at the seams (top-level API, public
    //     methods) -> refactor with safety net -> extract pure functions to
    //     test in isolation.

    // [Scenario] Q22: A bug recurs after a fix.
    // A : Add a failing regression test FIRST, fix, ensure it passes.
    //     Keep it forever.

    // [Scenario] Q23: How to test logging?
    // A : Inject ILogger<T>; in tests use a FakeLogger / TestLogger (Moq an
    //     ILogger and verify level + message contains substrings). Don't
    //     over-assert exact log strings.

    // [Scenario] Q24: How to test middleware?
    // A : Build a minimal RequestDelegate pipeline:
    //         var ctx = new DefaultHttpContext();
    //         await new MyMiddleware(next: _ => Task.CompletedTask).InvokeAsync(ctx);
    //     Assert on ctx.Response / written body.

    internal static class _Tests { }
}
