using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
namespace SyncthingRunner
{
	partial class Program
	{
		// Declare the SetConsoleCtrlHandler function
		// as external and receiving a delegate.

		[LibraryImport("Kernel32", EntryPoint = "SetConsoleCtrlHandler")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool SetConsoleCtrlHandler(HandlerRoutine Handler, [MarshalAs(UnmanagedType.Bool)] bool Add);
		// A delegate type to be used as the handler routine
		// for SetConsoleCtrlHandler.
		public delegate bool HandlerRoutine(CtrlTypes CtrlType);

		// An enumerated type for the control messages
		// sent to the handler routine.
		public enum CtrlTypes
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT,
			CTRL_CLOSE_EVENT,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT
		}

		static CancellationTokenSource? source;
		static void Main()
		{
			source = new();
			AppDomain.CurrentDomain.ProcessExit += (s, e) => {
				if (!source.IsCancellationRequested)
				{ 
					source.Cancel();
				}
			};
			SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true); 
			CancellationToken cancellationToken = source.Token;
			ILoggerFactory loggerFactory = LoggerFactory.Create(builder => {
				builder.AddConsole();
			});
			IConfigurationRoot config = new ConfigurationBuilder()
				.AddUserSecrets<Program>()
				.Build();
			string userSecrets = config["SyncthingAPIKey"] ?? throw new Exception("API key does not exist. ");
			SyncthingHelper.SyncthingHelper helper = new(loggerFactory.CreateLogger("SyncthingRunner"), userSecrets, cancellationToken);
			Task task = helper.Run();
			var keyPress = Console.ReadKey();
			helper.Stop().Wait();
		}

		private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
		{
			if (source != null && !source.IsCancellationRequested)
			{
				source.Cancel();
			}
			return true;
		}
	}
}