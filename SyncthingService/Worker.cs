namespace SyncthingService
{
	public class Worker(ILogger<Worker> logger, string secret) : BackgroundService()
	{
		private SyncthingHelper.SyncthingHelper? helper;
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			this.helper = new(logger, secret, stoppingToken);
			await helper.Run();
		}

		public override async Task StopAsync(CancellationToken stoppingToken)
		{
			if (this.helper != null)
			{ 
				await this.helper.Stop();
			}
			if (logger.IsEnabled(LogLevel.Information))
			{ 
				logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);
			}
			await base.StopAsync(stoppingToken);
		}

	}
}
