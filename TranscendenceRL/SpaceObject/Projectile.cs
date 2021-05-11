﻿using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static TranscendenceRL.Weapon;

namespace TranscendenceRL {
    public interface ITrail {
        Effect GetTrail(XY Position);
    }
    public class SimpleTrail : ITrail {
        public StaticTile Tile;
        public SimpleTrail(StaticTile Tile) {
            this.Tile = Tile;
        }
        public Effect GetTrail(XY Position) => new EffectParticle(Position, Tile, 3);
    }
    public class Projectile : MovingObject {
        [JsonProperty]
        public World world { get; private set; }
        [JsonProperty] 
        public SpaceObject Source;
        [JsonProperty] 
        public XY position { get; private set; }
        [JsonProperty] 
        public XY velocity { get; set; }
        [JsonProperty]
        public int lifetime { get; set; }
        [JsonProperty] 
        public ColoredGlyph tile { get; private set; }
        [JsonProperty]
        public ITrail trail;
        [JsonProperty]
        public FragmentDesc desc;
        [JsonProperty]
        public Maneuver maneuver;

        [JsonProperty]
        public bool hitProjectile;

        public bool active => lifetime > 0;

        public Projectile(SpaceObject Source, FragmentDesc desc, XY Position, XY Velocity, Maneuver maneuver = null) {
            this.Source = Source;
            this.world = Source.world;
            this.tile = desc.effect.Glyph;
            this.position = Position;
            this.velocity = Velocity;
            this.lifetime = desc.lifetime;
            this.desc = desc;
            this.trail = (ITrail)desc.trail ?? new SimpleTrail(desc.effect.Glyph);
            this.maneuver = maneuver;
        }
        public void Update() {
            if(lifetime > 1) {
                lifetime--;
                UpdateMove();
                foreach (var f in desc.fragments) {
                    if (f.fragmentInterval > 0 && lifetime % f.fragmentInterval == 0) {
                        Fragment(f);
                    }
                }
            } else if(lifetime == 1) {
                lifetime--;
                UpdateMove();
                Fragment();
            }


            void UpdateMove() {
                HashSet<Entity> exclude = new HashSet<Entity> { null, Source, this };
                if(Source is PlayerShip ps) {
                    exclude.Add(ps.dock?.Target);
                }
                if(Source is AIShip s) {
                    exclude.UnionWith(s.avoidHit);
                }

                maneuver?.Update(this);

                var dest = position + velocity / Program.TICKS_PER_SECOND;
                var inc = velocity.Normal * 0.5;
                var steps = velocity.Magnitude * 2 / Program.TICKS_PER_SECOND;
                for (int i = 0; i < steps; i++) {
                    position += inc;

                    
                    
                    foreach(var other in world.entities[position].Except(exclude)) {
                        switch(other) {
                            case Segment seg when exclude.Contains(seg.parent):
                                continue;
                            case SpaceObject hit:
                                lifetime = 0;
                                hit.Damage(Source, desc.damageHP);

                                if(desc.disruptor != null) {
                                    if (hit is PlayerShip sh) {
                                        sh.ship.controlHijack = desc.disruptor.GetHijack();
                                    } else if(hit is AIShip ai) {
                                        ai.ship.controlHijack = desc.disruptor.GetHijack();
                                    }
                                }

                                Fragment();
                                var angle = (hit.position - position).Angle;
                                world.AddEffect(new EffectParticle(hit.position + XY.Polar(angle, -1), hit.velocity, new ColoredGlyph(Color.Yellow, Color.Transparent, 'x'), 5));
                                return;
                            case ProjectileBarrier barrier:
                                barrier.Interact(this);
                                break;
                            case Projectile p when hitProjectile:
                                p.lifetime = 0;
                                lifetime = 0;
                                break;
                        }
                    }
                    CollisionDone:
                    world.AddEffect(trail.GetTrail(position));
                }

                position = dest;
            }
        }
        public void Fragment() {
            foreach (var f in desc.fragments) {
                Fragment(f);
            }
        }
        public void Fragment(FragmentDesc fragment) {
            if(fragment.requiresLockStatus != null
                && fragment.requiresLockStatus != (maneuver.target != null)) {
                return;
            }
            

            double angleInterval = fragment.spreadAngle / fragment.count;
            double centerAngle;

            if(fragment.omnidirectional
                && maneuver?.target?.active == true
                && Aiming.CalcFireAngle(this, maneuver.target, fragment.missileSpeed, out var result)) {
                centerAngle = result;
            } else {
                centerAngle = velocity.Angle;
            }
            for (int i = 0; i < fragment.count; i++) {
                double angle = centerAngle + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
                Projectile p = new Projectile(Source, fragment, position + XY.Polar(angle, 0.5), velocity + XY.Polar(angle, fragment.missileSpeed));
                world.AddEntity(p);
            }
        }
    }

    public class Maneuver {
        public SpaceObject target;
        public double maneuver;
        public double maneuverDistance;
        public Maneuver(SpaceObject target, double maneuver, double maneuverDistance) {
            this.target = target;
            this.maneuver = maneuver;
            this.maneuverDistance = maneuverDistance;
        }
        public void Update(Projectile p) {
            if(target == null || maneuver == 0) {
                return;
            }
            var vel = p.velocity;
            var offset = target.position - p.position;
            var velLeft = vel.Rotate(maneuver);
            var velRight = vel.Rotate(-maneuver);
            var distLeft = (offset - velLeft).Magnitude;
            var distRight = (offset - velRight).Magnitude;

            if(maneuverDistance == 0) {
                if (distLeft < distRight) {
                    p.velocity = velLeft;
                } else if (distRight < distLeft) {
                    p.velocity = velRight;
                }
            } else {
                (var closer, var farther) = distLeft < distRight ? (velLeft, velRight) : (velRight, velLeft);
                if (offset.Magnitude > maneuverDistance) {
                    p.velocity = closer;
                } else {
                    p.velocity = farther;
                }
            }
        }
    }

}
