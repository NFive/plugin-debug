using System;
using System.Collections.Generic;
using CitizenFX.Core.Native;
using NFive.Notifications.Client;
using NFive.Notifications.Shared;
using NFive.SDK.Core.Diagnostics;

namespace NFive.Debug.Client.Commands
{
	public class IplCommands
	{
		private readonly NotificationManager notifications;
		private readonly ILogger logger;

		public IplCommands(NotificationManager notifications, ILogger logger)
		{
			this.notifications = notifications;
			this.logger = logger;
		}

		public void Load(IEnumerable<string> args)
		{
			foreach (var arg in args)
			{
				try
				{
					this.notifications.Show(new Notification
					{
						Text = $"Loading IPL \"{arg}\"...",
						Type = "success"
					});

					API.RequestIpl(arg);
				}
				catch (Exception ex)
				{
					this.logger.Error(ex, $"Error loading IPL \"{arg}\"");

					this.notifications.Show(new Notification
					{
						Text = $"Error loading IPL \"{arg}\"",
						Type = "error"
					});
				}
			}
		}

		public void Unload(IEnumerable<string> args)
		{
			foreach (var arg in args)
			{
				try
				{
					this.notifications.Show(new Notification
					{
						Text = $"Unloading IPL \"{arg}\"...",
						Type = "warning"
					});

					API.RemoveIpl(arg);
				}
				catch (Exception ex)
				{
					this.logger.Error(ex, $"Error unloading IPL \"{arg}\"");

					this.notifications.Show(new Notification
					{
						Text = $"Error unloading IPL \"{arg}\"",
						Type = "error"
					});
				}
			}
		}
	}
}
