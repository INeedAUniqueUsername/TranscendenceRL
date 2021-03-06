﻿using Common;
using SadConsole;
using SadConsole.Input;
using System.Linq;
using TranscendenceRL.Screens;
using static SadConsole.Input.Keys;
using static TranscendenceRL.ControlKeys;
using Helper = Common.Main;
namespace TranscendenceRL {
	public enum ControlKeys {
		Thrust,
		TurnRight,
		TurnLeft,
		Brake,
		Autopilot,
		Dock,
		TargetFriendly,
		ClearTarget,
		ShipMenu,
		Gate,
		TargetEnemy,
		Powers,
		NextWeapon,
		FirePrimary,
		AutoAim
    }
    public class PlayerControls {
		PlayerShip playerShip;
		PlayerMain playerMain;
		PowerMenu powerMenu;
		PauseMenu pauseMenu;

		Console sceneContainer;

		public PlayerControls(PlayerShip playerShip, PlayerMain console, PowerMenu powerMenu, PauseMenu pauseMenu, Console sceneContainer) {
			this.playerShip = playerShip;
			this.playerMain = console;
			this.powerMenu = powerMenu;
			this.pauseMenu = pauseMenu;
			this.sceneContainer = sceneContainer;
        }
		public void ProcessArrows(Keyboard info) {
			var controls = playerShip.player.Settings.controls;
			if (info.IsKeyDown(controls[Thrust])) {
				playerShip.SetThrusting();
			}
			if (info.IsKeyDown(controls[TurnLeft])) {
				playerShip.SetRotating(Rotating.CCW);
			}
			if (info.IsKeyDown(controls[TurnRight])) {
				playerShip.SetRotating(Rotating.CW);
			}
			if (info.IsKeyDown(controls[Brake])) {
				playerShip.SetDecelerating();
			}
		}
		public void ProcessTargeting(Keyboard info) {
			var controls = playerShip.player.Settings.controls;
			if (info.IsKeyPressed(controls[TargetFriendly])) {
				playerShip.NextTargetFriendly();
			}
			if (info.IsKeyPressed(controls[ClearTarget])) {
				if (playerShip.targetIndex > -1) {
					playerShip.ClearTarget();
				}
			}
			if (info.IsKeyPressed(controls[TargetEnemy])) {
				if (info.IsKeyDown(LeftShift)) {
					playerMain.TargetMouse();
				} else {
					playerShip.NextTargetEnemy();
				}
			}
			if (info.IsKeyPressed(controls[NextWeapon])) {
				playerShip.NextWeapon();
			}
			if (info.IsKeyDown(controls[FirePrimary])) {
				playerShip.SetFiringPrimary();
			}
			if (info.IsKeyDown(controls[AutoAim])) {
				
				if (playerShip.GetTarget(out SpaceObject target) && playerShip.GetPrimary(out Weapon w)) {
					playerShip.SetRotatingToFace(Helper.CalcFireAngle(target.position - playerShip.position, target.velocity - playerShip.velocity, w.missileSpeed, out _));
				}
			}
		}

		public void ProcessPowerMenu(Keyboard info) {
			var controls = playerShip.player.Settings.controls;

			ProcessArrows(info);
			ProcessTargeting(info);

			if (info.IsKeyPressed(controls[Powers])) {
				if (powerMenu != null) {
					powerMenu.IsVisible = !powerMenu.IsVisible;
				}
			}
			if (!playerMain.autopilotUpdate && info.IsKeyPressed(controls[Autopilot])) {
				playerShip.autopilot = !playerShip.autopilot;
				playerShip.AddMessage(new InfoMessage($"Autopilot {(playerShip.autopilot ? "engaged" : "disengaged")}"));
			}
		}
        public void ProcessKeyboard(Keyboard info) {
			var controls = playerShip.player.Settings.controls;

			//Move the player
			ProcessArrows(info);
			ProcessTargeting(info);
			if(!playerMain.autopilotUpdate && info.IsKeyPressed(controls[Autopilot])) {
				playerShip.autopilot = !playerShip.autopilot;
				playerShip.AddMessage(new InfoMessage($"Autopilot {(playerShip.autopilot ? "engaged" : "disengaged")}"));
			}
			if (info.IsKeyPressed(controls[Dock])) {
				if (playerShip.dock != null) {
					if (playerShip.dock.docked) {
						playerShip.AddMessage(new InfoMessage("Undocked"));
					} else {
						playerShip.AddMessage(new InfoMessage("Docking sequence canceled"));
					}

					playerShip.dock = null;
				} else {
					Dockable dest = null;
					if(playerShip.GetTarget(out var t) && (playerShip.position - t.position).magnitude < 8 && t is Dockable d) {
						dest = d;
                    }
					dest = dest ?? playerShip.world.entities.GetAll(p => (playerShip.position - p).magnitude < 8).OfType<Dockable>().OrderBy(p => (p.position - playerShip.position).magnitude).FirstOrDefault();
					if (dest != null) {
						playerShip.AddMessage(new InfoMessage("Docking sequence engaged"));
						playerShip.dock = new Docking(dest);
					}

				}
			}
			if(info.IsKeyPressed(Escape)) {
				pauseMenu.IsVisible = true;
			}

			if(info.IsKeyPressed(controls[ControlKeys.Gate])) {
				playerShip.DisengageAutopilot();
				playerMain.Gate();
            }

			if (info.IsKeyPressed(controls[ShipMenu])) {
				playerShip.DisengageAutopilot();
				sceneContainer?.Children.Add(new ShipScreen(playerMain, playerShip) { IsFocused = true });
			}
			if (info.IsKeyPressed(controls[Powers])) {
				if(powerMenu != null)
					powerMenu.IsVisible = !powerMenu.IsVisible;
			}


			if (info.KeysDown.Select(d => d.Key).Intersect<Keys>(new Keys[] { Keys.LeftControl, Keys.LeftShift, Keys.Enter }).Count() == 3) {
				playerShip.Destroy(playerShip);
			}

#if DEBUG
			if (info.IsKeyPressed(C)) {
				if(info.IsKeyDown(LeftShift)) {
					playerShip.Destroy(playerShip);
                } else {
					playerShip.Damage(playerShip, playerShip.ship.damageSystem.GetHP() - 5);
				}
			}
#endif
			if (info.IsKeyPressed(V)) {
				playerShip.ship.controlHijack = new ControlHijack() { ticksLeft = 90, thrustMode = HijackMode.FORCE_ON };
			}
		}
    }
}
