using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using JetBrains.Annotations;
using NFive.Debug.Client.Commands;
using NFive.Debug.Shared;
using NFive.SDK.Client.Commands;
using NFive.SDK.Client.Communications;
using NFive.SDK.Client.Events;
using NFive.SDK.Client.Input;
using NFive.SDK.Client.Interface;
using NFive.SDK.Client.Services;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Input;
using NFive.SDK.Core.Models.Player;
using Font = CitizenFX.Core.UI.Font;

namespace NFive.Debug.Client
{
	[PublicAPI]
	public class DebugService : Service
	{
		private readonly Hotkey targetKey = new Hotkey(InputControl.Aim);
		private Configuration config;
		private Hotkey activateKey;
		private bool enabled;
		private Entity tracked;

		public DebugService(ILogger logger, ITickManager ticks, ICommunicationManager comms, ICommandManager commands, IOverlayManager overlay, User user) : base(logger, ticks, comms, commands, overlay, user) { }

		public override async Task Started()
		{
			this.config = await this.Comms.Event(DebugEvents.Configuration).ToServer().Request<Configuration>();
			this.activateKey = new Hotkey(this.config.ActivateKey);

			this.Logger.Debug($"Activate key set to {this.activateKey.UserKeyboardKeyDisplayName}");

			this.Commands.On("ipl-load", a => IplCommands.Load(this.Logger, a));
			this.Commands.On("ipl-unload", a => IplCommands.Unload(this.Logger, a));
			this.Commands.On("inv", a => PlayerCommands.Invincible(this.Logger, a.ToList()));
			this.Commands.On("veh", a => VehicleCommands.Run(this.Logger, a.ToList()));

			this.Ticks.On(OnTick);
		}

		private async Task OnTick()
		{
			if (this.activateKey.IsJustPressed())
			{
				this.enabled = !this.enabled;

				Screen.ShowNotification($"Debug tools {(this.enabled ? "~g~enabled" : "~r~disabled")}");
			}

			if (!this.enabled) return;

			if (this.targetKey.IsPressed())
			{
				DrawCrosshair();

				this.tracked = GetEntityInCrosshair();
			}

			if (this.tracked != null)
			{
				HighlightObject(this.tracked);
				DrawData(this.tracked);
			}

			var waypoint = World.WaypointPosition;
			if (waypoint == Vector3.Zero) return;

			var entity = Game.PlayerPed.IsInVehicle() ? (Entity)Game.PlayerPed.CurrentVehicle : Game.PlayerPed;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			while (waypoint.Z == 0f)
			{
				entity.PositionNoOffset = waypoint;

				await Delay(0);

				waypoint.Z = World.GetGroundHeight(new Vector2(waypoint.X, waypoint.Y));
			}

			entity.PositionNoOffset = waypoint;
			GameplayCamera.RelativeHeading = 0;

			World.RemoveWaypoint();
		}
		
		private void DrawCrosshair()
		{
			API.DrawRect(0.5f, 0.5f, 0.008333333f, 0.001851852f, 255, 0, 0, 255);
			API.DrawRect(0.5f, 0.5f, 0.001041666f, 0.014814814f, 255, 0, 0, 255);
		}

		private Entity GetEntityInCrosshair()
		{
			var raycast = World.Raycast(GameplayCamera.Position, GameplayCameraForwardVector(), 100f, IntersectOptions.Everything, Game.PlayerPed);

			if (!raycast.DitHit || !raycast.DitHitEntity || raycast.HitPosition == default(Vector3)) return null;
			return raycast.HitEntity;
		}

		private void HighlightObject(Entity entity)
		{
			var res = API.GetEntityCoords(entity.Handle, true);

			API.DrawLine(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, res.X, res.Y, res.Z, 0, 255, 0, 255);

			API.SetDrawOrigin(res.X, res.Y, res.Z, 0);

			API.RequestStreamedTextureDict("helicopterhud", false);

			API.DrawSprite("helicopterhud", "hud_corner", -0.02f / 1.777f, -0.02f, 0.008f, 0.008f, 0.0f, 255, 0, 0, 200);
			API.DrawSprite("helicopterhud", "hud_corner", 0.02f / 1.777f, -0.02f, 0.008f, 0.008f, 90.0f, 255, 0, 0, 200);
			API.DrawSprite("helicopterhud", "hud_corner", -0.02f / 1.777f, 0.02f, 0.008f, 0.008f, 270.0f, 255, 0, 0, 200);
			API.DrawSprite("helicopterhud", "hud_corner", 0.02f / 1.777f, 0.02f, 0.008f, 0.008f, 180.0f, 255, 0, 0, 200);

			API.ClearDrawOrigin();
		}

		private void DrawData(Entity entity)
		{
			const float lineHeight = 0.024f;
			var data = GetDataFor(entity);
			var pos = new Vector2(0.9f, 0.5f);
			var size = new Vector2(0.16f, data.Count * lineHeight + 0.04f);

			API.DrawRect(pos.X, pos.Y, size.X, size.Y, 0, 0, 0, 127);

			pos.Y -= size.Y / 2;
			pos.X -= size.X / 2;

			pos.Y += 0.02f;
			pos.X += 0.01f;

			foreach (var entry in data)
			{
				if (!string.IsNullOrEmpty(entry.Value)) DrawText($"{entry.Key}: {entry.Value}", new Vector2(pos.X, pos.Y));
				pos.Y += lineHeight;
			}
		}

		private Dictionary<string, string> GetDataFor(Entity entity)
		{
			var list = new Dictionary<string, string>();

			if (!entity.Exists())
			{
				this.tracked = null;

				return list;
			}

			try
			{
				list["Model Name"] = GetModelName(entity.Model);
				list[""] = "";

				var players = ((List<object>)API.GetActivePlayers()).Cast<byte>().Select(p => new Player(p));
				var player = players.FirstOrDefault(p => p.Character == entity);

				if (player != null)
				{
					list["Player Name"] = player.Name;
					list["Server ID"] = $"{player.ServerId}";
					list["Is Talking"] = $"{(Function.Call<bool>(Hash.NETWORK_IS_PLAYER_TALKING, player) ? "~g~True" : "~r~False")}~s~";
					list["  "] = "";
					list["Health"] = $"{player.Character.Health} / {player.Character.MaxHealth}";
					list["Invincible"] = $"{(player.IsInvincible ? "~g~True" : "~r~False")}~s~";
				}
				else if (entity is Vehicle veh)
				{
					list["Engine Health"] = $"{veh.EngineHealth:N1} / 1,000.0";
					list["Body Health"] = $"{veh.BodyHealth:N1} / 1,000.0";
					list["Speed"] = $"{veh.Speed / 0.621371f:N3}MP/H";
					list["RPM"] = $"{veh.CurrentRPM:N3}";
					list["Current Gear"] = $"{veh.CurrentGear}";
					list["Acceleration"] = $"{veh.Acceleration:N3}";
				}
				else
				{
					list["Health"] = $"{entity.Health} / {entity.MaxHealth}";
				}

				list[" "] = "";
				list["Distance"] = $"{Math.Sqrt(Game.PlayerPed.Position.DistanceToSquared(entity.Position)):N3}m";
				list["Heading"] = $"{entity.Heading:N3}";

				var pos = entity.Position;
				var rot = entity.Rotation;
				var vel = entity.Velocity;
				list["Position"] = $"{pos.X:N5} {pos.Y:N5} {pos.Z:N5}";
				list["Rotation"] = $"{rot.X:N5} {rot.Y:N5} {rot.Z:N5}";
				list["Velocity"] = $"{vel.X:N5} {vel.Y:N5} {vel.Z:N5}";
			}
			catch (Exception ex)
			{
				this.Logger.Error(ex);
			}

			return list;
		}

		private void DrawText(string text, Vector2 pos, Color? color = null, float scale = 0.25f, bool shadow = false, float shadowOffset = 1f, Alignment alignment = Alignment.Left, Font font = Font.ChaletLondon)
		{
			var col = color ?? Color.FromArgb(255, 255, 255);

			Function.Call(Hash.SET_TEXT_FONT, font);
			Function.Call(Hash.SET_TEXT_PROPORTIONAL, 0);
			Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
			if (shadow) Function.Call(Hash.SET_TEXT_DROPSHADOW, shadowOffset, 0, 0, 0, 255);
			Function.Call(Hash.SET_TEXT_COLOUR, col.R, col.G, col.B, col.A);
			Function.Call(Hash.SET_TEXT_EDGE, 1, 0, 0, 0, 255);
			Function.Call(Hash.SET_TEXT_JUSTIFICATION, alignment);
			Function.Call(Hash._SET_TEXT_ENTRY, "STRING");
			Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
			Function.Call(Hash._DRAW_TEXT, pos.X, pos.Y);
		}

		private string GetModelName(Model model)
		{
			var name = string.Empty;

			if (model.IsVehicle)
			{
				name = Enum.GetName(typeof(VehicleHash), (VehicleHash)model.Hash) ?? string.Empty;
			}

			if (model.IsProp)
			{
				name = Enum.GetName(typeof(ObjectHash), (ObjectHash)model.Hash) ?? string.Empty;
			}

			// model.IsPed isn't implemented in FiveM
			if (string.IsNullOrEmpty(name))
			{
				name = Enum.GetName(typeof(PedHash), (PedHash)model.Hash) ?? string.Empty;
			}

			return string.IsNullOrEmpty(name) ? model.Hash.ToString() : name;
		}

		// GameplayCamera.ForwardVector is stubbed out so this is necessary
		// TODO: Still needed?
		private static Vector3 GameplayCameraForwardVector()
		{
			var rotation = (float)(Math.PI / 180.0) * GameplayCamera.Rotation;

			return Vector3.Normalize(new Vector3((float)-Math.Sin(rotation.Z) * (float)Math.Abs(Math.Cos(rotation.X)), (float)Math.Cos(rotation.Z) * (float)Math.Abs(Math.Cos(rotation.X)), (float)Math.Sin(rotation.X)));
		}
	}
}
