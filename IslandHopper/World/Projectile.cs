﻿using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Common.Main;
using static IslandHopper.ItemType;

namespace IslandHopper {
	public class ThrownItem : Entity {
        public Entity Thrower { get; private set; }
        public IItem Thrown { get; private set; }
        public Island World { get => Thrown.World; }
        public XYZ Position { get => Thrown.Position; set => Thrown.Position = value; }
        public XYZ Velocity { get => Thrown.Velocity; set => Thrown.Velocity = value; }
        public bool Active => flying && Thrown.Active;
        private bool flying;
        private int tick = 0;
        public void OnRemoved() {
            if (Thrown.Active) {
                Thrown.World.AddEntity(Thrown);
            }
        }
        public ThrownItem(Entity thrower, IItem source) {
            this.Thrower = thrower;
            this.Thrown = source;
            flying = true;
        }
        public ColoredGlyph SymbolCenter => tick % 20 < 10 ? Thrown.SymbolCenter : Thrown.SymbolCenter.Brighten(51);
        public ColoredString Name => tick % 20 < 10 ? Thrown.Name : Thrown.Name.Brighten(51);

        public void UpdateRealtime(TimeSpan delta) {
            tick++;
            Thrown.UpdateRealtime(delta);
        }
        public void UpdateStep() {
            this.DebugInfo("UpdateStep()");
            Thrown.UpdateGravity();
            Thrown.UpdateMotionCollision(e => {
				if (e == this) {
					this.DebugInfo("Ignore collision with self");
					return true;
				} else if (e == Thrower) {
                    this.DebugInfo("Ignore collision with thrower");
                    return true;
                } else {
                    this.DebugInfo("Flying collision with object");
					Thrower.Witness(new InfoEvent($"Thrown {Thrown.Name} hits {e.Name}"));
                    flying = false;
                    return false;
                }
            });
            if (this.OnGround() && Velocity.Magnitude < 0.2) {

                this.DebugInfo("Landed on ground");
                Thrower.Witness(new InfoEvent(new ColoredString($"Thrown {Thrown.Name} lands on the ground.")));
                //this.DebugExit();
                flying = false;
            }
        }
    }
    public class LaunchedGrenade : Entity {
        public Island World { get; set; }
        public Entity Source { get; set; }
        public GrenadeType grenadeType;
        public int Countdown;
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public ColoredString Name => new ColoredString("Grenade", Color.White, Color.Black);
        public ColoredGlyph SymbolCenter => new ColoredGlyph(Color.Green, Color.Black, 'g');
        public bool Active { get; set; } = true;
        public LaunchedGrenade(Island World, Entity Source, GrenadeType grenadeType) {
            this.World = World;
            this.Source = Source;
            this.grenadeType = grenadeType;
            this.Countdown = grenadeType.fuseTime;
        }
        public void Detonate() {
            Active = false;
            World.AddEffect(new ExplosionSource(World, Position, 6));
            foreach (var offset in Main.GetWithin(grenadeType.explosionRadius)) {
                var pos = Position + offset;
                var displacement = pos - Position;
                var dist2 = displacement.Magnitude2;
                var radius2 = grenadeType.explosionRadius * grenadeType.explosionRadius;
                if (dist2 > radius2) {
                    continue;
                }
                foreach (var hit in World.entities[pos]) {
                    if (hit is ICharacter d && hit != this) {
                        var multiplier = (radius2 - displacement.Magnitude2) / radius2;
                        ExplosionDamage damage = new ExplosionDamage() {
                            damage = (int)(grenadeType.explosionDamage * multiplier),
                            knockback = displacement.Normal * grenadeType.explosionForce * multiplier
                        };
                        Source.Witness(new InfoEvent(Name + new ColoredString(" explosion damages ", Color.White, Color.Black) + d.Name));
                        d.OnDamaged(damage);
                    }
                }
            }
        }
        public void UpdateRealtime() { }
        public void UpdateStep() {
            this.UpdateGravity();
            this.UpdateMotionCollision();
            if (Countdown > 0) {
                Countdown--;
            } else {
                Detonate();
            }
        }

        public void OnRemoved() {
        }

        public void UpdateRealtime(TimeSpan delta) {
        }
    }
    public class Beam : Entity {
		public Island World { get; }
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredGlyph(tick % 20 < 10 ? Color.White : Color.Gray, Color.Black, '~');
		public ColoredString Name => new ColoredString("Beam", tick % 20 < 10 ? new Color(255, 0, 0, 255) : new Color(204, 0, 0, 255), Color.Black);

		private Entity Source;
		private Entity Target;
		private int tick;   //Used for sprite flashing

		public Beam(Entity Source, Entity Target, XYZ Velocity) {
			this.Source = Source;
			this.Target = Target;
			this.World = Source.World;
			this.Position = Source.Position;
			this.Velocity = Velocity;
			Active = true;
			tick = 0;
		}
		public void UpdateRealtime(TimeSpan delta) {
			tick++;
		}

		public void UpdateStep() {
            /*
			Func<Entity, bool> ignoreSource = e => e == Source;
			Func<Entity, bool> filterTarget = e => e != Target;
			//We only reach this if the other conditions were not met
			Func<Entity, bool> onHit = e => {
				Active = false;
				Source.Witness(new InfoEvent($"The {Name} hits {e.Name}"));
				return false;
			};

			Func<Entity, bool> collisionFilter = Helper.Or(Source.Elvis(ignoreSource), Target.Elvis(filterTarget), onHit);

			this.UpdateMotionCollision(collisionFilter);
            */
		}
	}

    public class Flame : Entity, Damager {
        public Island World { get; }
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public bool Active { get; private set; }
        public void OnRemoved() { }
        public char glyph;
        public ColoredGlyph SymbolCenter {
            get {
                var background = (Math.Max(0, 10 - lifetime) * 51) / 10;
                return new ColoredGlyph(new Color(
                    (int)(Math.Cos(tick * 3 / Math.PI) * 51) + 204,
                    (int)(Math.Sin(tick * 3 / Math.PI) * 51) + 102,
                    51,
                    Math.Min(6, lifetime) * 255 / 6
                    ), new Color(background,background,background, 255),
                    glyph);
            }
        }
        public ColoredString Name => new ColoredString("Flame", SymbolCenter.Foreground, Color.Black);

        private Entity Source;
        private IItem Item;
        private int tick;   //Used for sprite flashing
        public int lifetime = 30;
        public int damage { get; } = 5;

        public Flame(Entity Source, IItem Item, XYZ Position, XYZ Velocity, int lifetime) {
            this.Source = Source;
            this.Item = Item;
            this.World = Source.World;
            this.Position = Position;
            this.Velocity = Velocity;
            Active = true;
            tick = 0;
            this.lifetime = lifetime;

            glyph = new char[] { 'v', 'w', 'f', 'j' }[World.karma.NextInteger(4)];
        }
        public void UpdateRealtime(TimeSpan delta) {
            tick++;
        }

        public void UpdateStep() {
            if (lifetime > 0) {
                lifetime--;
            } else {
                Active = false;
            }
            Func<Entity, bool> collisionFilter = e => {

                if(e is Flame || e is Fire) {
                    return true;
                }
                Source.Witness(new InfoEvent($"The {Name} hits {e.Name}"));
                if (e is Damageable d) {
                    d.OnDamaged(this);
                    Item?.Gun?.OnHit(this, d);
                }

                Active = false;
                return false;
            };
            this.UpdateMotionCollisionTrail(out HashSet<XYZ> trail, collisionFilter);
            Velocity -= Velocity * 0.5 / 30;

            Func<int, int, int> rnd = World.karma.NextInteger;
            var count = trail.Count;
            if (rnd(0, 5) == 0) {
                foreach (var point in trail) {
                    if (World.voxels[point.PlusZ(-1)] is Grass g) {
                        if (rnd(0, count * 5) == 0) {
                            World.AddEntity(new Fire(World) { Position = point, Velocity = new XYZ() });
                            break;
                        }
                    }

                }
            }

            foreach (var point in trail.Reverse().Take(3)) {
                World.AddEffect(new FlameTrail(point, 3, SymbolCenter));
            }
            if(World.karma.NextInteger(2) == 0) {
                World.AddEffect(new Mirage(World, Position + new XYZ(rnd(-2, 3), rnd(-2, 3)), 5));
            }
            
            
        }
    }
    public class Fire : Entity {
        public Island World { get; set; }
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public ColoredGlyph SymbolCenter => new ColoredGlyph(Color.Orange, Color.Transparent, 'v');
        public bool Active { get; set; } = true;
        public ColoredString Name => new ColoredString("Fire", Color.Red, Color.Black);
        public Fire(Island World) {
            this.World = World;
        }
        public void UpdateRealtime(TimeSpan delta) { }
        public void UpdateStep() {
            var below = Position.PlusZ(-1);
            if(World.voxels.InBounds(below) && World.voxels[below] is Grass g) {
                Func<int, int, int> rnd = World.karma.NextInteger;

                if(rnd(0, 150) == 0) {
                    World.voxels[below] = new Dirt();
                    Active = false;
                }
                if (rnd(0, 150) == 0) {
                    var adjacent = new XY[] { new XY(-1, 0), new XY(1, 0), new XY(0, -1), new XY(0, 1) }
                                    .Select(p => Position + p)
                                    .Where(p => World.voxels.InBounds(p))
                                    .Where(p => World.voxels[p.PlusZ(-1)] is Grass g)
                                    .Where(p => World.entities[p].OfType<Fire>().Count() == 0);
                    if(adjacent.Any()) {
                        var p = adjacent.ElementAt(rnd(0, adjacent.Count())).PlusZ(-1);
                        World.AddEntity(new Fire(World) { Position = p });
                    }
                }
            } else {
                Active = false;
            }
        }
        public void OnRemoved() {

        }
    }

    public class Bullet : Entity, Damager {
		public Island World { get; }
        public XYZ Position { get; set; }
        public XYZ Velocity { get; set; }
        public bool Active { get; private set; }
		public void OnRemoved() { }
        public char GetSymbol() {
            var angle = Velocity.xyAngle * 180 / Math.PI;
            angle = ((int)(360 + angle + 22.5) % 360) / 45;
            char[] chars = {
                '-', '/', '|', '\\', '-', '/', '|', '\\'
            };
            return chars[(int)angle];
        }
		public ColoredGlyph SymbolCenter => new ColoredGlyph(tick % 20 < 10 ? Color.White : Color.Gray, Color.Transparent, GetSymbol());
        public ColoredString Name => new ColoredString("Bullet", tick % 20 < 10 ? Color.White : Color.Gray, Color.Transparent);

        public Entity Source;
        private IItem Item;
        private HashSet<Entity> ignore;
		private Entity Target;
		private int tick;   //Used for sprite flashing
        public int lifetime = 30;
        public double knockback { get; } = 2;
        public int damage { get; }

		public Bullet(Entity Source, IItem Item, Entity Target, XYZ Velocity, int damage) {
            this.Source = Source;
            this.Item = Item;
            ignore = new HashSet<Entity>();
            ignore.Add(Source);
            ignore.Add(Item);
            this.Target = Target;
            this.World = Source.World;
            this.Position = Source.Position;
            this.Velocity = Velocity;
            Active = true;
            tick = 0;
            lifetime = 30;
            this.damage = damage;
        }
        public void UpdateRealtime(TimeSpan delta) {
            tick++;
        }

        public void UpdateStep() {
            if(lifetime > 0) {
                lifetime--;
            } else {
                Active = false;
            }
            //this.UpdateGravity();
            /*
            Func<Entity, bool> ignoreSource = e => {
                bool result = ignore.Contains(e);
                if(result)
                    Source.Witness(new InfoEvent($"The {Name} ignores source {e.Name}"));
                else
                    Source.Witness(new InfoEvent($"The {Name} does not ignore non-source {e.Name}"));
                return result;
            };
            Func<Entity, bool> filterTarget = e => {
                bool result = e != Target;
                if(result)
                    Source.Witness(new InfoEvent($"The {Name} ignores non-target {e.Name}"));
                else
                    Source.Witness(new InfoEvent($"The {Name} does not ignore target {e.Name}"));
                return result;
            };
			Func<Entity, bool> onHit = e => {
				Active = false;
				Source.Witness(new InfoEvent($"The {Name} hits {e.Name}"));
				return false;
			};
            */
            //Why do I waste my life trying to fix this goddamned bug?
            //Func<Entity, bool> collisionFilter = Helper.Or(Source.Elvis(ignoreSource), Target.Elvis(filterTarget), onHit);

            Func<Entity, bool> collisionFilter = e => {
                //IGNORE if the entity is our Source
                if(Source != null && e == Source) {
                    return true;
                }

                //IGNORE if the entity is a bullet from the same item
                if(e is Bullet b && b.Item == Item) {
                    return true;
                }

                //IGNORE if the entity is NOT our target
                if (Target != null && e != Target) {
                    return true;
                }
                Source.Witness(new InfoEvent($"The {Name} hits {e.Name}"));


                if(e is Damageable d) {
                    d.OnDamaged(this);
                    Item?.Gun?.OnHit(this, d);
                }


                Active = false;
                return false;
            };
            this.UpdateMotionCollisionTrail(out HashSet<XYZ> trail, collisionFilter);
            foreach(var point in trail) {

                World.AddEffect(new Trail(point, 10, SymbolCenter.GlyphCharacter));
            }
        }
    }
	public class Missile : Entity {
		public Island World { get; }
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredGlyph(tick % 8 < 4 ? Color.White : Color.Gray, Color.Black, 'M');
		public ColoredString Name => new ColoredString("Missile", tick % 8 < 4 ? Color.White : Color.Gray, Color.Black);

		private Entity Source;
		private Entity Target;
		private int tick;   //Used for sprite flashing

		public Missile(Entity Source, Entity Target, XYZ Velocity) {
			this.Source = Source;
			this.Target = Target;
			this.World = Source.World;
			this.Position = Source.Position;
			this.Velocity = Velocity;
			Active = true;
			tick = 0;
		}
		public void UpdateRealtime(TimeSpan delta) {
			tick++;
		}

		public void UpdateStep() {
			this.UpdateGravity();

			Func<Entity, bool> ignoreSource = e => e == Source;
			Func<Entity, bool> filterTarget = e => e != Target;

			Func<Entity, bool> collisionFilter = Main.Or(Source.Elvis(ignoreSource), Target.Elvis(filterTarget));

			this.UpdateMotionCollision(collisionFilter);
		}
	}
	class ExplosionBlock : Entity {
		public Island World { get; }
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredGlyph(tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black, '*');
		public ColoredString Name => new ColoredString("Explosion", tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black);

		private int tick;
		public int lifetime;
		public ExplosionBlock(Island World, XYZ Position) {
			this.World = World;
			this.Position = Position;
			Velocity = new XYZ(0, 0, 0);
			tick = 0;
			lifetime = 10;
			Active = true;
		}

		public void UpdateRealtime(TimeSpan delta) {
			tick++;
		}

		public void UpdateStep() {
			Active = lifetime --> 0;
		}
	}
    //Explosions are just a visual effect
	class ExplosionSource : Entity {
		public Island World { get; }
		public XYZ Position { get; set; }
		public XYZ Velocity { get; set; }
		public bool Active { get; private set; }
		public void OnRemoved() { }
		public ColoredGlyph SymbolCenter => new ColoredGlyph((int)tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black, '*');
		public ColoredString Name => new ColoredString("Explosion", (int)tick % 4 < 2 ? new Color(255, 255, 0) : new Color(255, 153, 0), Color.Black);

		private double tick;   //Used for sprite flashing

		private List<XYZ> explosionOffsets;	//List of points surrounding our center that we will expand to
		private int tileIndex;
		private int rectRadius;

		private double maxRadius;
		private double currentRadius;
		private double expansionRate;
        private double expansionTime;
		
		public ExplosionSource(Island World, XYZ Position, double maxRadius) {
			this.World = World;
			this.Position = Position;
			Velocity = new XYZ(0, 0, 0);
			Active = true;
			tick = 0;

			//We calculate surrounding tiles as the explosion expands and fill them with explosion effects
			explosionOffsets = new List<XYZ>();
			explosionOffsets.Add(new XYZ(0, 0, 0));
			tileIndex = 0;
			rectRadius = 0;
			//In case we want to pre-calculate everything at once
			/*
			for (int i = 1; i <= maxRadius + 1; i++) {
				explosionPoints.AddRange(Helper.GetSurrounding(Position, i));
			}
			*/
			this.maxRadius = maxRadius;
			currentRadius = 0;
			expansionRate = 1;
            expansionTime = maxRadius / expansionRate;
		}
		public void UpdateRealtime(TimeSpan delta) {
			tick += delta.TotalSeconds;
		}
		public void UpdateStep() {
			currentRadius += expansionRate;
			//Expand to our edge tiles and then self destruct
			if(currentRadius > maxRadius) {
				currentRadius = maxRadius;
				Active = false;
			}

			this.DebugInfo("UpdateStep");
			this.DebugInfo($"Current Radius: {currentRadius}");

			//See if we need to calculate more surrounding tiles now (the farthest tile calculated so far is within the current radius)
			while (explosionOffsets.Last().Magnitude < currentRadius) {
				//Calculate a few rects at a time because the corners prevent us from checking further
				for (int i = 0; i < 6; i++) {
					rectRadius++;
					//Add the surrounding shell of tiles to our list.
					explosionOffsets.AddRange(Main.GetSurrounding(rectRadius));
					this.DebugInfo($"Added surrounding tiles for radius: {rectRadius}");
				}
			}
			while (explosionOffsets[tileIndex].Magnitude < currentRadius) {
                //Expand to this tile
                World.AddEffect(new ExplosionBlock(World, Position + explosionOffsets[tileIndex]) {
                    lifetime = (int)(expansionTime * (1 - currentRadius / maxRadius) * 5 + World.karma.NextInteger(0, 20))
                });

				this.DebugInfo($"Expanded to tile index: {tileIndex}");

				//Increment the index since we covered this tile
				//We do not remove elements because ignoring them is more efficient
				tileIndex++;    //This should not go past the list because we pre-calculated points ahead of our current radius
			}
		}
	}
}
