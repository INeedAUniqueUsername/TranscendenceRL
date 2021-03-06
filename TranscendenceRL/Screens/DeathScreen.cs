﻿
using ArchConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.Linq;
using Console = SadConsole.Console;

namespace TranscendenceRL {
    class DeathScreen : Console {
        World world;
        PlayerShip playerShip;
        Epitaph epitaph;
        public DeathScreen(PlayerMain playerMain, Epitaph epitaph) : base(playerMain.Width, playerMain.Height) {
            var world = playerMain.world;
            this.playerShip = playerMain.playerShip;
            
            this.epitaph = epitaph;

            this.Children.Add(new LabelButton("Resurrect", () => {
                //Restore mortality chances
                playerShip.mortalChances = 3;

                //To do: Restore player HP
                playerShip.ship.damageSystem.Restore();

                //Resurrect the player; remove wreck and restore ship + heading
                if (epitaph.wreck != null) {
                    epitaph.wreck.Destroy(null);
                    world.entities.all.Remove(epitaph.wreck);
                }
                playerShip.ship.active = true;
                playerShip.AddMessage(new InfoMessage("A vision of disaster flashes before your eyes"));
                world.entities.all.Add(playerShip);
                world.effects.all.Add(new Heading(playerShip));
                GameHost.Instance.Screen = new TitleSlideOpening(new Pause(playerMain, Resume, 4)) { IsFocused = true };
                void Resume() {
                    GameHost.Instance.Screen = playerMain;
                    playerMain.IsFocused = true;
                    playerMain.ShowUI();
                }
            }) { Position = new Point(1, Height/2 - 4), FontSize = playerMain.FontSize * 2 });

            this.Children.Add(new LabelButton("Title Screen", () => {
                SadConsole.Game.Instance.Screen = new TitleSlideOpening(new TitleScreen(Width, Height, new World(world.universe))) { IsFocused = true };
            }) { Position = new Point(1, Height/2 - 2), FontSize = playerMain.FontSize * 2 });
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
            var player = playerShip.player;
            var str =
@$"
{player.name}
{player.Genome.name}
{playerShip.shipClass.name}
{epitaph.desc}

Final Devices
{string.Join('\n', playerShip.devices.Installed.Select(device => $"    {device.source.type.name}"))}

Final Cargo
{string.Join('\n', playerShip.cargo.Select(item => $"    {item.type.name}"))}

Ships Destroyed
{string.Join('\n', playerShip.shipsDestroyed.GroupBy(sc => sc.shipClass).Select(pair => $"    {pair.Key.name, -16}{pair.Count(), 4}"))}
".Replace("\r", "");
            int y = 2;
            foreach(var line in str.Split('\n')) {
                this.Print(2, y++, line);
            }

            if(epitaph.deathFrame != null) {
                var size = epitaph.deathFrame.GetLength(0);
                for (y = 0; y < size; y++) {
                    for (int x = 0; x < size; x++) {
                        this.SetCellAppearance(Width - x - 2, y + 1, epitaph.deathFrame[x, y]);
                    }
                }
            }
            

            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            return base.ProcessKeyboard(keyboard);
        }
    }
}
