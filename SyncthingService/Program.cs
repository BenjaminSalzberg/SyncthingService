using SyncthingService;
namespace SyncthingService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}
		public static HostApplicationBuilder CreateHostBuilder(string[] args) {
			HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
			builder.Services.AddHostedService<Worker>();
			builder.Services.AddLogging(configure => configure.AddEventLog());
			builder.Configuration.AddEnvironmentVariables();
			builder.Services.AddSingleton(sp =>
			{
				IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
				string secret = configuration["SyncthingAPIKey"] ?? throw new Exception("APIKey not found in environment variables");
				return secret;
			});
			return builder;
		}
	}
}
