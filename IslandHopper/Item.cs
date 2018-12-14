﻿using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	interface IItem : Entity {
		Gun Gun { get; set; }
	}
	interface Gun {

	}
	class Item : IItem, IGravity {
		public World World { get; set; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }

		public Gun Gun { get; set; }

		public ColoredString SymbolCenter => throw new NotImplementedException();

		public ColoredString Name => throw new NotImplementedException();

		public bool Active => true;
		public void UpdateRealtime() { }
		public void UpdateStep() { }
	}

	class Gun1 : IItem, IGravity {
		public World World { get; set; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }

		public Gun Gun { get; set; }

		public Gun1(World World, Point3 Position) {
			this.World = World;
			this.Position = Position;
			this.Velocity = new Point3();
		}

		public bool Active => true;

		public void UpdateRealtime() { }

		public void UpdateStep() {
			this.UpdateGravity();
			this.UpdateMotion();
		}


		public ColoredString Name => new ColoredString("Gun", new Cell(Color.Gray, Color.Transparent));
		public ColoredString SymbolCenter => new ColoredString("r", new Cell(Color.Gray, Color.Transparent));
	}
	class Parachute : Entity {
		public Entity user { get; private set; }
		public bool Active { get; private set; }
		public Point3 Position { get; set; }
		public Point3 Velocity { get; set; }
		public Parachute(Entity user) {
			this.user = user;
			UpdateFromUser();
			Active = true;

		}

		public void UpdateRealtime() {
		}
		public void UpdateFromUser() {
			Position = user.Position + new Point3(0, 0, 1);
			Velocity = user.Velocity;
		}
		public void UpdateStep() {
			Debug.Print(nameof(UpdateStep));
			UpdateFromUser();
			Point3 down = user.Position - Position;
			double speed = down * user.Velocity.Magnitude;
			if (speed > 3.8 / 30) {
				double deceleration = speed * 0.4;
				user.Velocity -= down * deceleration;
			}
		}
		public readonly ColoredString symbol = new ColoredString("*", Color.White, Color.Transparent);
		public ColoredString SymbolCenter => symbol;
		public ColoredString Name => new ColoredString("Parachute", Color.White, Color.Black);
	}
}
