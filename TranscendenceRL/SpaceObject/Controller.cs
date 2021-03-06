﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using static TranscendenceRL.Weapon;
using Helper = Common.Main;
namespace TranscendenceRL {
    public interface IOrder {
        bool Active { get; }
        void Update(AIShip owner);
        public bool CanTarget(SpaceObject other) => false;
    }
    public class CompoundOrder : IOrder {
        public List<IOrder> orders;
        public CompoundOrder(params IOrder[] orders) {
            this.orders = new List<IOrder>(orders);
        }
        public void Update(AIShip owner) {
            if(orders.Count == 0) {
                return;
            }
            IOrder first = orders.First();
            first.Update(owner);
            if(!first.Active) {
                orders.RemoveAt(0);
            }
        }
        public bool Active => orders.Any();
    }
    public class EscortOrder : IOrder {
        public SpaceObject attacker;
        public IShip target;
        public XY offset;
        public EscortOrder(IShip target, XY offset) {
            this.target = target;
            this.offset = offset;
        }

        public bool CanTarget(SpaceObject other) => other == attacker;
        public void Update(AIShip owner) {

            attacker = ((attacker?.active == true) ? attacker : null) ?? owner.world.entities.all
                .OfType<AIShip>()
                .FirstOrDefault(s => (s.position - owner.position).magnitude < 100
                            && (s.controller.CanTarget(target) || s.controller.CanTarget(owner)));
            if (attacker != null) {
                new AttackOrder(attacker).Update(owner);
            } else {
                new FollowOrder(target, offset).Update(owner);
            }
        }
        public bool Active => target.active;
    }
    public class FollowOrder : IOrder {
        public IShip target;
        public XY offset;
        public FollowOrder(IShip target, XY offset) {
            this.target = target;
            this.offset = offset;
        }
        public void Update(AIShip owner) {
            var offset = this.offset.Rotate(target.stoppingRotation * Math.PI / 180);
#if DEBUG
            Heading.Crosshair(owner.world, target.position + offset);
#endif
            new ApproachOrder(target, this.offset).Update(owner);
        }
        public bool Active => target.active;
    }
    public class ApproachOrder : IOrder {
        public IShip target;
        public XY offset;
        public ApproachOrder(IShip target, XY offset) {
            this.target = target;
            this.offset = offset;
        }

        public void Update(AIShip owner) {
            //Remove dock
            owner.dock = null;

            var velDiff = owner.velocity - target.velocity;
            double decel = owner.shipClass.thrust * Program.TICKS_PER_SECOND / 2;
            double stoppingTime = velDiff.magnitude / decel;
            double stoppingDistance = owner.velocity.magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
            var stoppingPoint = owner.position;
            if (!owner.velocity.isZero) {
                stoppingPoint += owner.velocity.normal * stoppingDistance;
            }
            var dest = target.position + (target.velocity * stoppingTime) + this.offset.Rotate(target.stoppingRotation * Math.PI / 180);
            var offset = dest - stoppingPoint;

#if DEBUG
            Heading.Crosshair(owner.world, dest);
#endif
            var velProjection = velDiff * velDiff.Dot(offset.normal) / velDiff.Dot(velDiff);
            var velRejection = velDiff - velProjection;

            //Make sure we're going the right way
            if (velDiff.magnitude > 5 && velRejection.magnitude > velProjection.magnitude/2) {
                //Decelerate
                var backAngle = Math.PI + velRejection.angleRad;
                var faceBack = new FaceOrder(backAngle);
                faceBack.Update(owner);
                var angleDiff = Math.Abs(Helper.AngleDiff(owner.rotationDeg, backAngle * 180 / Math.PI));
                if (angleDiff < 5) {
                    owner.SetThrusting(true);
                }
            } else {
                //Prepare to decelerate
                if (offset.magnitude < 1) {
                    //If we're close enough to dest, then just teleport there
                    if((dest - owner.position).magnitude > 1) {
                        owner.SetDecelerating(true);
                    } else {
                        owner.velocity = target.velocity;
                        owner.position = dest;

                        //Match the target's facing
                        var Face = new FaceOrder(target.rotationDeg * Math.PI / 180);
                        Face.Update(owner);
                    }

                } else {
                    //Approach the target

                    //Face the target
                    var Face = new FaceOrder(offset.angleRad);
                    //var Face = new FaceOrder(Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, owner.ShipClass.thrust * 30, out _));
                    Face.Update(owner);

                    //If we're facing close enough
                    if (Math.Abs(Helper.AngleDiff(owner.rotationDeg, offset.angleRad * 180 / Math.PI)) < 10 && (velProjection.magnitude < offset.magnitude/2 || velDiff.magnitude == 0)) {

                        //Go
                        owner.SetThrusting(true);
                    }
                }

                
            }
            
        }
        public bool Active => true;
    }
    public class GuardOrder : IOrder {
        public SpaceObject GuardTarget;
        public AttackOrder attackOrder;
        public int attackTime;
        public int lazyTicks;
        public GuardOrder(SpaceObject guard) {
            this.GuardTarget = guard;
            attackOrder = null;
            attackTime = 0;
        }

        public bool CanTarget(SpaceObject other) => other == attackOrder?.target;
        public void Update(AIShip owner) {
            if (attackTime > 0) {
                attackTime--;
                if (attackTime == 0) {
                    attackOrder = null;
                }
            }

            //If we're docked, then don't check for enemies every tick
            if(owner.dock?.docked == true) {
                lazyTicks++;
                if(lazyTicks%120 > 0) {
                    return;
                }
            }


            if (attackOrder?.target?.active == true) {
                attackOrder.Update(owner);
                return;
            }

            var target = owner.world.entities.GetAll(p => (GuardTarget.position - p).magnitude < 20).OfType<SpaceObject>().Where(o => !o.IsEqual(owner) && GuardTarget.CanTarget(o)).GetRandomOrDefault(owner.destiny);

            if (target != null) {
                attackOrder = new AttackOrder(target);
                attackOrder.Update(owner);
                return;
            }
            
            if ((owner.position - GuardTarget.position).magnitude < 6) {
                //If no enemy in range of station, dock at station
                owner.dock = new Docking(GuardTarget);
            } else {
                new ApproachOrbitOrder(GuardTarget).Update(owner);
            }
        }
        public bool Active => GuardTarget.active;
    }
    public class AttackAllOrder : IOrder {
        public int sleepTicks;
        public SpaceObject target;
        public bool CanTarget(SpaceObject other) => other == target;
        public void Update(AIShip owner) {
            if(sleepTicks > 0) {
                sleepTicks--;
                return;
            }

            if (owner.devices.Weapons.Count == 0) {
                sleepTicks = 150;
                return;
            }
            if (target?.active == true) {
                new AttackOrder(target).Update(owner);
                return;
            }
            //currentRange is variable and minRange is constant, so weapon dynamics may affect attack range
            target = owner.world.entities.all.OfType<SpaceObject>().Where(o => !owner.IsEqual(o)).Where(o => owner.IsEnemy(o)).GetRandomOrDefault(owner.destiny);

            //If we can't find a target, then give up for a while
            if (target == null) {
                sleepTicks = 150;
            }
        }
        public bool Active => true;
    }
    public class AttackOrder : IOrder {
        public SpaceObject target;
        public Weapon weapon;
        public List<Weapon> omni;
        public AimOrder aim;
        public AttackOrder(SpaceObject target) {
            this.target = target;
        }
        public bool CanTarget(SpaceObject other) => other == target;
        private void Set(Weapon w) => w.SetFiring(true, target);
        public void Update(AIShip owner) {
            var weapons = owner.devices.Weapons;
            if (weapon?.AllowFire != true) {
                var w = weapons.Where(w => w.AllowFire);
                weapon = w.FirstOrDefault(w => w.aiming == null) ?? w.FirstOrDefault();
                if (weapon == null) {
                    omni = null;
                    return;
                }
                aim = new AimOrder(target, weapon.missileSpeed);
                omni = w
                   .Where(w => w.aiming != null)
                   .Where(w => w != weapon)
                   .ToList();
            } else if (!weapon.CanFire && weapons.Count > 1) {
                var w = weapons.Where(w => w.CanFire);
                weapon = w.FirstOrDefault(w => w.aiming == null) ?? weapon;
            }
            bool RangeCheck() => (owner.position - target.position).magnitude < weapon.currentRange;
            void SetFiring() {
                Set(weapon);
            }

            //Remove dock
            owner.dock = null;

            var offset = (target.position - owner.position);
            var dist = offset.magnitude;

            omni.ForEach(w => {
                if (dist < w.currentRange) {
                    Set(w);
                }
            });

            if (dist < 10) {
                //If we are too close, then move away

                //Face away from the target
                new FaceOrder(offset.angleRad + Math.PI).Update(owner);

                //Get moving!
                owner.SetThrusting(true);
            } else {
                bool freeAim = weapon.aiming != null && dist < weapon.currentRange;

                if (dist < weapon.currentRange / 2) {
                    //If we are in range, then aim and fire

                    //Aim at the target
                    aim.Update(owner);

                    if (Math.Abs(aim.GetAngleDiff(owner)) < 10
                        && (owner.velocity - target.velocity).magnitude < 5) {
                        owner.SetThrusting(true);
                    }
                    //Fire if we are close enough
                    if (freeAim
                        || Math.Abs(aim.GetAngleDiff(owner)) * dist < 3) {
                        SetFiring();
                    }
                } else {
                    //Otherwise, get closer

                    new ApproachOrbitOrder(target).Update(owner);
                    //Fire if our angle is good enough
                    if (freeAim
                        || Math.Abs(aim.GetAngleDiff(owner)) * dist < 3 && RangeCheck()) {
                        SetFiring();
                    }

                }
            }
        }
        public bool Active => target.active && weapon != null;
    }

    public class PatrolOrder : IOrder {
        public SpaceObject patrolTarget;
        public double patrolRadius;
        public double attackRadius;
        public AttackOrder attackOrder;
        
        public PatrolOrder(SpaceObject patrolTarget, double patrolRadius) {
            this.patrolTarget = patrolTarget;
            this.patrolRadius = patrolRadius;
            this.attackRadius = 2 * patrolRadius;
        }
        public void Update(AIShip owner) {
            if(attackOrder?.target?.active == true) {
                attackOrder.Update(owner);
                return;
            }

            List<SpaceObject> except = new List<SpaceObject> {
                owner, patrolTarget
            };
            var target = owner.world.entities.all
                .OfType<SpaceObject>()
                .Where(p => (patrolTarget.position - p.position).magnitude < attackRadius)
                .Where(p => (owner.position - p.position).magnitude < 50)
                .Where(o => owner.IsEnemy(o))
                .Where(o => !SSpaceObject.IsEqual(o, owner) && !SSpaceObject.IsEqual(o, patrolTarget))
                .GetRandomOrDefault(owner.destiny);

            if (target != null) {
                attackOrder = new AttackOrder(target);
                attackOrder.Update(owner);
                return;
            }

            var offsetFromTarget = (owner.position - patrolTarget.position);
            var dist = offsetFromTarget.magnitude;

            var deltaDist = patrolRadius - dist;

            var nextDist = Math.Abs(deltaDist) > 10 ?
                dist + Math.Sign(deltaDist) * 10 :
                patrolRadius;

            var nextOffset = offsetFromTarget
                .Rotate(2 * Math.PI / 16)
                .WithMagnitude(nextDist);

            var deltaOffset = nextOffset - offsetFromTarget;

            var Face = new FaceOrder(deltaOffset.angleRad);
            Face.Update(owner);
            owner.SetThrusting(true);
        }
        public bool Active => patrolTarget.active;
    }

    public class SnipeOrder : IOrder {
        public SpaceObject target;
        public Weapon weapon;
        public SnipeOrder(SpaceObject target) {
            this.target = target;
        }
        public bool CanTarget(SpaceObject other) => other == target;
        public void Update(AIShip owner) {
            var weapons = owner.devices.Weapons;
            if (weapon?.AllowFire != true) {
                weapon = weapons.FirstOrDefault(w => w.AllowFire);
                if (weapon == null) {
                    return;
                }
            } else if(!weapon.CanFire && weapons.Count > 1) {
                weapon = weapons.FirstOrDefault(w => w.CanFire) ?? weapon;
            }
            //Aim at the target
            var aim = new AimOrder(target, weapon.missileSpeed);
            aim.Update(owner);

            //Fire if we are close enough
            if (weapon.desc.shot.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 30) {
                weapon.SetFiring(true, target);
            }
        }
        public bool Active => target?.active == true && weapon?.AllowFire == true;
    }
    public class ApproachOrbitOrder : IOrder {
        public SpaceObject target;
        public ApproachOrbitOrder(SpaceObject target) {
            this.target = target;
        }

        public void Update(AIShip owner) {
            //Remove dock
            owner.dock = null;

            //Find the direction we need to go
            var offset = (target.position - owner.position);

            var randomOffset = new XY((2 * owner.destiny.NextDouble() - 1) * offset.x, (2 * owner.destiny.NextDouble() - 1) * offset.y) / 5;

            offset += randomOffset;

            var speedTowards = (owner.velocity - target.velocity).Dot(offset.normal);
            if (speedTowards < 0) {
                //Decelerate
                var Face = new FaceOrder(Math.PI + owner.velocity.angleRad);
                Face.Update(owner);
                owner.SetThrusting(true);
            } else {
                //Approach

                //Face the target
                var Face = new FaceOrder(offset.angleRad);
                Face.Update(owner);

                //If we're facing close enough
                if (Math.Abs(Helper.AngleDiff(owner.rotationDeg, offset.angleRad * 180 / Math.PI)) < 10 && speedTowards < 10) {

                    //Go
                    owner.SetThrusting(true);
                }
            }
        }
        public bool Active => true;
    }

    public class AimOnceOrder : IOrder {
        public AimOrder order;
        public AimOnceOrder(BaseShip owner, BaseShip target, double missileSpeed) {
            this.order = new AimOrder(target, missileSpeed);
            Active = true;
        }
        public void Update(AIShip owner) {
            order.Update(owner);
            Active = Math.Abs(order.GetAngleDiff(owner)) > 1;
        }

        public bool Active { get; private set; }
    }
    public class AimOrder : IOrder {
        public SpaceObject target;
        public double missileSpeed;
        public double GetTargetRads(AIShip owner) => Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, missileSpeed, out var _);
        public double GetAngleDiff(AIShip owner) => Helper.AngleDiff(owner.rotationDeg, GetTargetRads(owner) * 180 / Math.PI);
        public AimOrder(SpaceObject target, double missileSpeed) {
            this.target = target;
            this.missileSpeed = missileSpeed;
        }
        public bool Active => true;
        public void Update(AIShip owner) {
            var targetRads = this.GetTargetRads(owner);
            var facingRads = owner.stoppingRotation * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude;
            var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude;
            if (ccw < cw) {
                owner.SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                owner.SetRotating(Rotating.CW);
            }
        }
    }
    public class FaceOrder : IOrder {
        public double targetRads;
        public FaceOrder(double targetRads) {
            this.targetRads = targetRads;
        }
        public void Update(AIShip owner) {
            var facingRads = owner.ship.stoppingRotationWithCounterTurn * Math.PI / 180;

            var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude;
            var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude;
            if (ccw < cw) {
                owner.SetRotating(Rotating.CCW);
            } else if (cw < ccw) {
                owner.SetRotating(Rotating.CW);
            } else {
                if (owner.ship.rotatingVel > 0) {
                    owner.SetRotating(Rotating.CW);
                } else {
                    owner.SetRotating(Rotating.CCW);
                }
            }
        }
        public bool Active => true;
    }

}
