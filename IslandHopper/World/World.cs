﻿using Common;
using IslandHopper;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	public class Island {
		public Rand karma;
		public LocatorDict<Entity, (int, int, int)> entities { get; set; }     //	3D entity grid used for collision detection
        public LocatorDict<Effect, (int, int, int)> effects { get; set; }
		public ArraySpace<Voxel> voxels { get; set; }   //	3D voxel grid used for collision detection
		public XYZ camera { get; set; }              //	Point3 representing the location of the center of the screen
		public Player player { get; set; }              //	Player object that controls the game
        public TypeCollection types;

        public void AddEffect(Effect e) => effects.PlaceNew(e);
		public void AddEntity(Entity e) => entities.PlaceNew(e);
        public void RemoveEntity(Entity e) {
            entities.Remove(e);
        }
	}
}
