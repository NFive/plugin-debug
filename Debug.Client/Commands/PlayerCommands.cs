using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using NFive.Debug.Client.Extensions;
using NFive.Notifications.Client;
using NFive.Notifications.Shared;

namespace NFive.Debug.Client.Commands
{
	public class PlayerCommands
	{
		private readonly NotificationManager notifications;

		public PlayerCommands(NotificationManager notifications)
		{
			this.notifications = notifications;
		}

		public void Invincible(List<string> args)
		{
			if (!args.Any())
			{
				Game.Player.IsInvincible = !Game.Player.IsInvincible;
			}
			else
			{
				Game.Player.IsInvincible = args[0].IsTruthy();
			}

			this.notifications.Show(new Notification
			{
				Text = $"You are now{(Game.Player.IsInvincible ? string.Empty : " not")} invincible",
				Type = Game.Player.IsInvincible ? "success" : "warning"
			});
		}
	}
}
