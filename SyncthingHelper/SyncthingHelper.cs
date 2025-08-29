using Microsoft.Extensions.Logging;
using System.Diagnostics;
namespace SyncthingHelper
{
	public class SyncthingHelper
	{
		private readonly CancellationToken _cancellationToken;
		private readonly ILogger _logger;
		private Process? process;
		bool disposed = false;
		readonly string secretKey;
		public SyncthingHelper(ILogger logger, string secretKey, CancellationToken cancellationToken)
		{
			_cancellationToken = cancellationToken;
			_logger = logger;
			this._cancellationToken.Register(() =>
			{
				if (!disposed)
				{
					try
					{
						Stop().Wait();
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error killing syncthing process");
						throw;
					}
				}
			});
			this.secretKey = secretKey;
		}

		public async Task Run()
		{
			if (!IsSyncthingInPath(out string syncthingPath))
			{
				_logger.LogError("Syncthing executable not found in PATH. Please ensure that it is installed and registered on your path. ");
				throw new Exception("Syncthing executable not found in PATH.Please ensure that it is installed and registered on your path. ");
			}
			ProcessStartInfo info = new(syncthingPath)
			{
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				ErrorDialog = false,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			process = Process.Start(info);
			if (process == null)
			{
				_logger.LogError("Failed to start syncthing process.");
				throw new Exception("Failed to start syncthing process.");
			}
			while (!process.HasExited)
			{
				await Task.Delay(100000, _cancellationToken);
			}
		}

		private const string Path = "Path";

		private bool IsSyncthingInPath(out string syncthingPath)
		{
			syncthingPath = string.Empty;
			var environmentVariables = Environment.GetEnvironmentVariables();
			if (environmentVariables == null)
			{
				return false;
			}

			HashSet<System.Collections.DictionaryEntry> setOfEnvironmentVariables = [];
			foreach (var item in environmentVariables)
			{
				if (item == null)
				{
					continue;
				}
				System.Collections.DictionaryEntry currentItem = (System.Collections.DictionaryEntry)item;
				if (currentItem.Key == null || currentItem.Value == null)
				{
					continue;
				}
				setOfEnvironmentVariables.Add(currentItem);
			}
			Dictionary<string, object> dictionaryOfEnvironments = setOfEnvironmentVariables
				.Where(x => x.Key != null && x.Value != null)
				.ToDictionary(x => x.Key?.ToString() ?? string.Empty, z => z.Value ?? new());
			if (!dictionaryOfEnvironments.TryGetValue(Path, out object? pathObject))
			{
				return false;
			}
			if (pathObject == null)
			{
				return false;
			}
			string? pathString = pathObject.ToString();
			if (string.IsNullOrEmpty(pathString))
			{
				return false;
			}
			string[] paths = pathString.Split(';');
			if (paths == null || paths.Length == 0)
			{
				return false;
			}
			string syncthingPathLocal = string.Empty;
			if (this._cancellationToken == default)
			{
				return false;
			}
			Parallel.ForEachAsync(paths, this._cancellationToken, async (path, token) =>
			{
				await Task.Run(() =>
				{
					if (this._cancellationToken == default)
					{
						return;
					}
					if (this._cancellationToken.IsCancellationRequested)
					{
						return;
					}
					if (string.IsNullOrEmpty(path))
					{
						return;
					}
					string potentialSyncthingPath = System.IO.Path.Combine(path, "syncthing.exe");
					if (System.IO.File.Exists(potentialSyncthingPath))
					{
						syncthingPathLocal = potentialSyncthingPath;
						if (this._cancellationToken == default)
						{
							return;
						}
						this._cancellationToken.ThrowIfCancellationRequested();
					}
				}, token);
			}).Wait(_cancellationToken);
			if (!string.IsNullOrEmpty(syncthingPathLocal))
			{
				syncthingPath = syncthingPathLocal;
				return true;
			}
			return false;
		}

		public async Task Stop()
		{
			disposed = true;
			try
			{
				if (process != null && !process.HasExited)
				{
					HttpClient client = new();
					HttpRequestMessage request = new(HttpMethod.Post, new Uri("http://127.0.0.1:8384/rest/system/shutdown"));
					request.Headers.Add("X-API-Key", this.secretKey);
					HttpResponseMessage response = await client.SendAsync(request);
					if (!response.IsSuccessStatusCode)
					{
						if (_logger.IsEnabled(LogLevel.Error))
						{
							string statusCode = response.StatusCode.ToString();
							_logger.LogError("Failed to send shutdown request to syncthing. Status code: {statusCode}", statusCode);
						}
					}
					client.Dispose();
					try
					{
						process.Close();
					}
					finally
					{
						process.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error stopping syncthing process");
			}
		}
	}
}
