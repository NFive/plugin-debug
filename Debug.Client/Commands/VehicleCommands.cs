using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using NFive.Notifications.Client;
using NFive.Notifications.Shared;

namespace NFive.Debug.Client.Commands
{
	public class VehicleCommands
	{
		private readonly NotificationManager notifications;

		public VehicleCommands(NotificationManager notifications)
		{
			this.notifications = notifications;
		}

		public void Run(List<string> args)
		{
			if (!args.Any())
			{
				this.notifications.Show(new Notification
				{
					Text = "<strong>/veh</strong> &middot; Missing command argument",
					Type = "error"
				});

				return;
			}

			switch (args[0].Trim().ToLower())
			{
				case "spawn":
					if (args.Count < 2)
					{
						this.notifications.Show(new Notification
						{
							Text = "<strong>/veh spawn</strong> &middot; Missing vehicle name argument",
							Type = "error"
						});

						return;
					}

					SpawnVehicle(args[1].Trim());

					break;

				case "repair":
					RepairVehicle();

					break;

				case "clean":
					CleanVehicle();

					break;

				case "mod":
					if (args.Count < 3)
					{
						this.notifications.Show(new Notification
						{
							Text = "<strong>/veh mod</strong> &middot; Missing mod argument",
							Type = "error"
						});

						return;
					}

					var field = args[1];
					var value = float.Parse(args[2]);
					var veh = Game.Player.Character.CurrentVehicle;
					var before = API.GetVehicleHandlingFloat(veh.Handle, "CHandlingData", field);

					API.SetVehicleHandlingFloat(veh.Handle, "CHandlingData", field, value);

					this.notifications.Show(new Notification
					{
						Text = $"Vehicle mod <strong>{field}</strong> changed from {before} to {API.GetVehicleHandlingFloat(veh.Handle, "CHandlingData", field)}",
						Type = "success"
					});

					break;

				default:
					this.notifications.Show(new Notification
					{
						Text = $"<strong>/veh</strong> &middot; Unknown command \"<strong>{args[0].Trim().ToLower()}</strong>\"",
						Type = "error"
					});

					break;
			}
		}

		private async void SpawnVehicle(string modelName)
		{
			var model = new Model(API.GetHashKey(modelName));

			if (!model.IsValid || !model.IsVehicle)
			{
				this.notifications.Show(new Notification
				{
					Text = $"<strong>/veh spawn</strong> &middot; Invalid vehicle model \"<strong>{modelName}</strong>\"",
					Type = "error"
				});

				return;
			}

			var player = Game.Player.Character;
			var vehicle = await World.CreateVehicle(model, player.Position);

			// Set fancy license plate name
			vehicle.Mods.LicensePlate = " N5 Dev ";

			// Set fancy license plate style
			vehicle.Mods.LicensePlateStyle = LicensePlateStyle.BlueOnWhite3;

			// Warp player in to driver seat
			player.SetIntoVehicle(vehicle, VehicleSeat.Driver);

			this.notifications.Show(new Notification
			{
				Text = $"Spawned vehicle <strong>{modelName}</strong>",
				Type = "success"
			});
		}

		private void RepairVehicle()
		{
			var vehicle = Game.Player.Character.CurrentVehicle;

			if (vehicle == null)
			{
				this.notifications.Show(new Notification
				{
					Text = "<strong>/veh repair</strong> &middot; Not inside a vehicle",
					Type = "error"
				});

				return;
			}

			vehicle.EngineHealth = 1000;
			vehicle.IsEngineRunning = true;
			vehicle.Repair();

			this.notifications.Show(new Notification
			{
				Text = "Vehicle repaired",
				Type = "success"
			});
		}

		private void CleanVehicle()
		{
			var vehicle = Game.Player.Character.CurrentVehicle;

			if (vehicle == null)
			{
				this.notifications.Show(new Notification
				{
					Text = "<strong>/veh clean</strong> &middot; Not inside a vehicle",
					Type = "error"
				});

				return;
			}

			vehicle.DirtLevel = 0f;

			this.notifications.Show(new Notification
			{
				Text = "Vehicle cleaned",
				Type = "success"
			});
		}
	}
}
