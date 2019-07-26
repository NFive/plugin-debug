using JetBrains.Annotations;
using NFive.Debug.Shared;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Rcon;
using NFive.SDK.Server.Rpc;
using System.Threading.Tasks;

namespace NFive.Debug.Server
{
	[PublicAPI]
	public class DebugController : ConfigurableController<Configuration>
	{
		public DebugController(ILogger logger, IEventManager events, IRpcHandler rpc, IRconManager rcon, Configuration configuration) : base(logger, events, rpc, rcon, configuration) { }

		public override Task Loaded()
		{
			this.Rpc.Event(DebugEvents.Configuration).On(e => e.Reply(this.Configuration));

			return base.Loaded();
		}

		public override void Reload(Configuration configuration)
		{
			this.Rpc.Event(DebugEvents.Configuration).Trigger(configuration);
		}
	}
}
