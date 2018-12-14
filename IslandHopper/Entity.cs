﻿using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IslandHopper.Constants;

namespace IslandHopper {
	interface Entity {
		Point3 Position { get; set; }
		Point3 Velocity { get; set; }
		bool Active { get; }					//	When this is inactive, we remove it
		void UpdateRealtime();				//	For step-independent effects
		void UpdateStep();					//	The number of steps per one in-game second is defined in Constants as STEPS_PER_SECOND
		ColoredString SymbolCenter { get; }
		ColoredString Name { get; }
	}
	static class SGravity {
		public static bool OnGround(this IGravity g) => (g.World.voxels[g.Position] is Floor || g.World.voxels[g.Position.PlusZ(-1)] is Grass);
		public static void UpdateGravity(this IGravity g) {
			//	Fall or hit the ground
			if (g.Velocity.z < 0 && g.OnGround()) {
				g.Velocity.z = 0;
			} else {
				Debug.Print("fall");
				g.Velocity += new Point3(0, 0, -9.8 / STEPS_PER_SECOND);
			}
		}
		public static void UpdateMotion(this IGravity g) {
			Point3 normal = g.Velocity.Normal;
			Point3 dest = g.Position;
			for (Point3 p = g.Position + normal; (g.Position - p).Magnitude < g.Velocity.Magnitude; p += normal) {
				if (g.World.voxels[p] is Air) {
					dest = p;
				} else {
					break;
				}
			}
			g.Position = dest;
		}
	}
	interface IGravity {
		World World { get; set; }
		Point3 Position { get; set; }
		Point3 Velocity { get; set; }
	}
	class Player : Entity, IGravity {
		public Point3 Velocity { get; set; }
		public Point3 Position { get; set; }
		public World World { get; set; }
		public HashSet<EntityAction> Actions { get; private set; }
		public HashSet<IItem> inventory { get; private set; }

		public int frameCounter = 0;

		public Player(World World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3(0, 0, 0);
			Actions = new HashSet<EntityAction>();
			inventory = new HashSet<IItem>();

			World.AddEntity(new Parachute(this));
		}
		public bool AllowUpdate() => Actions.Count > 0 && frameCounter == 0;
		public bool Active => true;
		public void UpdateRealtime() {
			if(frameCounter > 0)
				frameCounter--;
		}
		public void UpdateStep() {
			this.UpdateGravity();
			this.UpdateMotion();
			Actions.ToList().ForEach(a => a.Update());
			Actions.RemoveWhere(a => a.Done());
			foreach(var i in inventory) {
				i.Position = Position;
				i.Velocity = Velocity;
			}
			if(!this.OnGround())
				frameCounter = 20;
		}
		
		public ColoredString SymbolCenter => new ColoredString("@", Color.White, Color.Transparent);
		public ColoredString Name => new ColoredString("Player", Color.White, Color.Black);
	}

	/*
	class Human : Entity {
		public Point3 Velocity { get; set; }
		public Point3 Position { get; set; }
		public GameConsole World { get; private set; }
		public HashSet<PlayerAction> Actions;
		public Human(GameConsole World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3(0, 0, 0);
			Actions = new HashSet<PlayerAction>();
		}
		public bool IsActive() => true;
		public bool OnGround() => (World.voxels[Position] is Floor || World.voxels[Position.PlusZ(-1)] is Grass);
		public void UpdateRealtime() {

		}
		public void UpdateStep() {
			//	Fall or hit the ground
			if(Velocity.z < 0 && OnGround()) {
				Velocity.z = 0;
			} else {
				System.Console.WriteLine("fall");
				Velocity += new Point3(0, 0, -9.8 / 30);
			}
			Point3 normal = Velocity.Normal();
			Point3 dest = Position;
			for(Point3 p = Position + normal; (Position - p).Magnitude() < Velocity.Magnitude(); p += normal) {
				if(World.voxels[p] is Air) {
					dest = p;
				} else {
					break;
				}
			}
			Position = dest;
			Actions.ToList().ForEach(a => a.Update());
			Actions.RemoveWhere(a => a.Done());
		}

		public static readonly ColoredString symbol = new ColoredString("U", Color.White, Color.Transparent);
		public virtual ColoredString GetSymbolCenter() => symbol;
	}
	class Player : Human {

		public Player(GameConsole World, Point3 Position) : base(World, Position) { }
		public bool AllowUpdate() => Actions.Count > 0;
		public static ColoredString symbol_player = new ColoredString("@", Color.White, Color.Transparent);
		public override ColoredString GetSymbolCenter() => symbol_player;
	}
	*/
}
