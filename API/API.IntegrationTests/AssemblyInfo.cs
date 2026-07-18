using Xunit;

// Program.cs wires a handful of Library static classes (CallTracker.cs, Tools.cs, ApiStatistic.cs,
// UserData.cs, LocationManager.cs) to their Postgres-backed repositories through a single
// process-static `VedAstro.Library.Repositories` locator (see Program.cs's "Wire the Library's
// static Repositories locator" comment) - it assigns `Repositories.Person = ...` etc. once per
// host build, from a single long-lived root DI scope/AppDbContext instance.
//
// xunit runs different test classes (= different test collections) in parallel by default. Each
// ApiWebApplicationFactory in this project builds its own fully independent host - including its
// own Postgres Testcontainer and its own AppDbContext - but Program.Main's assignment into that
// static `Repositories` locator is process-global, so two hosts built concurrently (e.g. two test
// classes' factories starting up back-to-back) stomp on each other's static field values, and a
// single shared AppDbContext instance also can't safely serve two requests at once ("A second
// operation was started on this context instance before a previous operation completed."). This
// was observed empirically running this suite unrestricted (flaky Pass/Fail across classes).
//
// Disabling parallelization here is a test-project-only workaround for that legacy static-locator
// design - fixing it "properly" would mean changing Program.cs to resolve Repositories.* per-scope
// instead of once from a shared root scope, which is out of scope for this test-writing task.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
