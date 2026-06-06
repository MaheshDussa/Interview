using FunctionApp.Host.Extensions;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
	.ConfigureFunctionsWorkerDefaults()
	.ConfigureServices(services => services.AddFunctionAppObservability())
	.Build();

host.Run();
