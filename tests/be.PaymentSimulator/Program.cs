using be.Tests.Simulation;

// This separate Development executable is never referenced by the business API.
Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_BE", AppContext.BaseDirectory);
try { await SimulatorServer.RunAsync(args); }
catch (InvalidOperationException error)
{
    // Fail closed without invoking Windows crash reporting on an expected guard.
    Console.Error.WriteLine(error.Message);
    Environment.ExitCode = 2;
}
