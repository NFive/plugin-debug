using JetBrains.Annotations;
using NFive.Debug.Shared;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Communications;
using NFive.SDK.Server.Controllers;

namespace NFive.Debug.Server
{
	[PublicAPI]
	public class DebugController : ConfigurableController<Configuration>
	{
		public DebugController(ILogger logger, Configuration configuration, ICommunicationManager comms) : base(logger, configuration)
		{
			// Send configuration when requested
			comms.Event(DebugEvents.Configuration).FromClients().OnRequest(e => e.Reply(this.Configuration));
		}
	}
}
