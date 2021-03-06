﻿using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using TranscendenceRL.Types;

namespace TranscendenceRL {

    public interface InvokeAction {
        string GetDesc(PlayerShip player, Item item);
        void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) { }
    }
    
    public class InstallWeapon : InvokeAction {
        public string GetDesc(PlayerShip player, Item item) {
            if (player.cargo.Contains(item)) {
                return "Install this weapon";
            } else {
                return "Remove this weapon";
            }
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
            if(player.cargo.Contains(item)) {
                player.AddMessage(new InfoMessage($"Installed weapon {item.type.name}"));

                player.cargo.Remove(item);
                item.InstallWeapon();
                player.devices.Install(item.weapon);
            } else {
                player.AddMessage(new InfoMessage($"Removed weapon {item.type.name}"));

                player.devices.Remove(item.weapon);
                item.RemoveWeapon();
                player.cargo.Add(item);
            }
            callback?.Invoke();
        }
    }
    public class RepairArmor : InvokeAction {
        public int repairHP;
        public string GetDesc(PlayerShip player, Item item) {
            return "Use this patch to repair armor";
        }
        public RepairArmor() {}
        public RepairArmor(XElement e) {
            this.repairHP = e.ExpectAttributeInt(nameof(repairHP));
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback) {
            var p = prev.Parent;
            p.Children.Remove(prev);
            p.Children.Add(SListScreen.RepairArmorScreen(prev, player, item, this, callback));
        }
    }
    public class InvokePower : InvokeAction {
        PowerType powerType;
        int charges;
        public InvokePower() { }
        public InvokePower(TypeCollection tc, XElement e) {
            powerType = tc.Lookup<PowerType>(e.ExpectAttribute("powerType"));
            charges = e.ExpectAttributeInt("charges");
        }
        public string GetDesc(PlayerShip player, Item item) {
            return "Invoke this charm";
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
            player.AddMessage(new InfoMessage($"Invoked the power of {item.type.name}"));

            charges--;
            if (charges == 0) {
                player.cargo.Remove(item);
            }
            new Power(powerType).Effect.Invoke(player);

            callback?.Invoke();
        }
    }
    public class Refuel : InvokeAction {
        public int energy;
        public Refuel() { }
        public Refuel(TypeCollection tc, XElement e) {
            energy = e.ExpectAttributeInt("energy");
        }
        public string GetDesc(PlayerShip player, Item item) {
            return "Refuel reactor";
        }
        public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
            var p = prev.Parent;
            p.Children.Remove(prev);
            p.Children.Add(SListScreen.RefuelReactor(prev, player, item, this, callback));
        }
    }

    public class ItemType : DesignType {
        public string codename;
        public string name;
        public string desc;
        public int level;
        public int mass;
        public int value;

        public ArmorDesc armor;
        public WeaponDesc weapon;
        public ShieldDesc shield;
        public ReactorDesc reactor;
        public MiscDesc misc;

        public InvokeAction invoke;


        public void Initialize(TypeCollection tc, XElement e) {
            codename = e.ExpectAttribute(nameof(codename));
            name = e.ExpectAttribute(nameof(name));
            desc = e.TryAttribute(nameof(desc), "");
            level = e.ExpectAttributeInt(nameof(level));
            mass = e.ExpectAttributeInt(nameof(mass));
            value = e.TryAttributeInt(nameof(value), 0);

            switch(e.TryAttribute(nameof(invoke), "none")) {
                case "none":
                    invoke = null;
                    break;
                case "installWeapon":
                    invoke = new InstallWeapon();
                    break;
                case "repairArmor":
                    invoke = new RepairArmor(e);
                    break;
                case "invokePower":
                    invoke = new InvokePower(tc, e);
                    break;
                case "refuel":
                    invoke = new Refuel(tc, e);
                    break;
            }

            if (e.HasElement("Weapon", out var xmlWeapon)) {
                weapon = new WeaponDesc(tc, xmlWeapon);
            }
            if (e.HasElement("Armor", out var xmlArmor)) {
                armor = new ArmorDesc(xmlArmor);
            }
            if (e.HasElement("Shield", out var xmlShield)) {
                shield = new ShieldDesc(xmlShield);
            }
            if(e.HasElement("Reactor", out var xmlReactor)) {
                reactor = new ReactorDesc(xmlReactor);
            }
            if(e.HasElement("Misc", out var xmlMisc)) {
                misc = new MiscDesc(xmlMisc);
            }
        }
    }
    public class ArmorDesc {
        public int maxHP;
        public Armor GetArmor(Item i) => new Armor(i, this);
        public ArmorDesc() { }
        public ArmorDesc(XElement e) {
            maxHP = e.ExpectAttributeInt("maxHP");
        }
    }
    public class WeaponDesc {
        public int powerUse;
        public int fireCooldown;
        public int repeat;
        public FragmentDesc shot;

        public int initialCharges;
        public ItemType ammoType;

        public bool targetProjectile;
        public bool autoFire;

        public int missileSpeed => shot.missileSpeed;
        public int damageType => shot.damageType;
        public int damageHP => shot.damageHP;
        public int lifetime => shot.lifetime;

        public int minRange => shot.missileSpeed * shot.lifetime / (Program.TICKS_PER_SECOND * Program.TICKS_PER_SECOND); //DOES NOT INCLUDE CAPACITOR EFFECTS
        public StaticTile effect;
        public CapacitorDesc capacitor;
        public Weapon GetWeapon(Item i) => new Weapon(i, this);
        public WeaponDesc() { }
        public WeaponDesc(TypeCollection types, XElement e) {
            powerUse = e.ExpectAttributeInt(nameof(powerUse));
            fireCooldown = e.ExpectAttributeInt(nameof(fireCooldown));
            repeat = e.TryAttributeInt(nameof(repeat), 0);
            shot = new FragmentDesc(e);


            if(e.TryAttributeBool("pointDefense", false)) {
                targetProjectile = true;
                autoFire = true;
            }

            initialCharges = e.TryAttributeInt(nameof(initialCharges), -1);
            if(e.TryAttribute(nameof(ammoType), out string at)) {
                if(!types.itemType.TryGetValue(at, out ammoType)) {
                    throw new Exception($"ItemType codename expected: ammoType=\"{at}\" ### {e} ### {e.Parent}");
                }
            }

            effect = new StaticTile(e);
            if(e.HasElement("Capacitor", out var xmlCapacitor)) {
                capacitor = new CapacitorDesc(xmlCapacitor);
            }
        }
    }
    public class FragmentDesc {
        public int count;
        public bool omnidirectional;
        public bool? targetLocked;
        public double spreadAngle;
        public int missileSpeed;
        public int damageType;
        public int damageHP;
        public int lifetime;
        public double maneuver;
        public double maneuverRadius;
        public int fragmentInterval;
        public DisruptorDesc disruptor;
        public HashSet<FragmentDesc> fragments;
        public StaticTile effect;
        public TrailDesc trail;
        public FragmentDesc() {}
        public FragmentDesc(XElement e) {
            count = e.TryAttributeInt(nameof(count), 1);
            if(e.TryAttributeBool("spreadOmni")) {
                spreadAngle = (2 * Math.PI) / count;
            } else {
                spreadAngle = e.TryAttributeDouble(nameof(spreadAngle), count == 1 ? 0 : 3) * Math.PI / 180;
            }
            omnidirectional = e.TryAttributeBool(nameof(omnidirectional));
            targetLocked = e.TryAttributeBoolOptional(nameof(targetLocked));
            missileSpeed = e.ExpectAttributeInt(nameof(missileSpeed));
            damageType = e.ExpectAttributeInt(nameof(damageType));
            damageHP = e.ExpectAttributeInt(nameof(damageHP));
            lifetime = e.ExpectAttributeInt(nameof(lifetime));
            maneuver = e.TryAttributeDouble(nameof(maneuver), 0) * Math.PI / (180);
            maneuverRadius = e.TryAttributeDouble(nameof(maneuverRadius), 0);
            fragmentInterval = e.TryAttributeInt(nameof(fragmentInterval), 0);
            fragments = new HashSet<FragmentDesc>();

            if (e.HasElement("Disruptor", out var xmlDisruptor)) {
                disruptor = new DisruptorDesc(xmlDisruptor);
            }

            if (e.HasElements("Fragment", out var fragmentsList)) {
                fragments.UnionWith(fragmentsList.Select(f => new FragmentDesc(f)));
            }
            if(e.HasElement("Trail", out var trail)) {
                this.trail = new TrailDesc(trail);
            }
            effect = new StaticTile(e);
        }
    }
    public class TrailDesc : ITrail {
        public int lifetime;


        public char glyph;
        public Color foreground;
        public Color background;
        public TrailDesc() { }
        public TrailDesc(XElement e) {
            lifetime = e.ExpectAttributeInt(nameof(lifetime));
            
            foreground = e.ExpectAttributeColor("foreground");
            background = e.ExpectAttributeColor("background");
            glyph = e.ExpectAttribute("char")[0];
        }
        public Effect GetTrail(XY Position) => new FadingTile(Position, new ColoredGlyph(foreground, background, glyph), lifetime);
    }
    public class DisruptorDesc {
        HijackMode thrustMode, turnMode, brakeMode, fireMode;
        public int lifetime;
        public DisruptorDesc() { }
        public DisruptorDesc(XElement e) {
            thrustMode = GetMode(e.TryAttribute(nameof(thrustMode), null));
            turnMode = GetMode(e.TryAttribute(nameof(turnMode), null));
            brakeMode = GetMode(e.TryAttribute(nameof(brakeMode), null));
            fireMode = GetMode(e.TryAttribute(nameof(fireMode), null));
            lifetime = e.TryAttributeInt(nameof(lifetime), 60);
        }
        public ControlHijack GetHijack() => new ControlHijack() {
            thrustMode = thrustMode,
            turnMode = turnMode,
            brakeMode = brakeMode,
            fireMode = fireMode,
            ticksLeft = lifetime
        };
        public HijackMode GetMode(string str) {
            switch(str) {
                case "on":
                    return HijackMode.FORCE_ON;
                case "off":
                    return HijackMode.FORCE_OFF;
                case "none":
                case null:
                    return HijackMode.NONE;
                default:
                    throw new Exception($"Invalid value {str}");

            }
        }
    }
    public class CapacitorDesc {
        public double minChargeToFire;
        public double dischargePerShot;
        public double chargePerTick;
        public double maxCharge;
        public double bonusSpeedPerCharge;
        public double bonusDamagePerCharge;
        public double bonusLifetimePerCharge;
        public CapacitorDesc() { }
        public CapacitorDesc(XElement e) {
            minChargeToFire = e.TryAttributeDouble(nameof(minChargeToFire));
            dischargePerShot = e.ExpectAttributeDouble(nameof(dischargePerShot));
            chargePerTick = e.ExpectAttributeDouble(nameof(chargePerTick));
            maxCharge = e.ExpectAttributeDouble(nameof(maxCharge));
            bonusSpeedPerCharge = e.ExpectAttributeDouble(nameof(bonusSpeedPerCharge));
            bonusDamagePerCharge = e.ExpectAttributeDouble(nameof(bonusDamagePerCharge));
            bonusLifetimePerCharge = e.ExpectAttributeDouble(nameof(bonusLifetimePerCharge));
        }
    }
    public class ShieldDesc {
        public int maxHP;
        public int depletionDelay;
        public double hpPerSecond;
        public Shields GetShields(Item i) => new Shields(i, this);
        public ShieldDesc() { }
        public ShieldDesc(XElement e) {
            maxHP = e.ExpectAttributeInt(nameof(maxHP));
            depletionDelay = e.ExpectAttributeInt(nameof(depletionDelay));
            hpPerSecond = e.ExpectAttributeDouble(nameof(hpPerSecond));
        }
    }
    public class ReactorDesc {
        public int maxOutput;
        public int capacity;
        public double efficiency;
        public bool battery;        //If true, then we recharge using power from other reactors when available

        public Reactor GetReactor(Item i) => new Reactor(i, this);
        public ReactorDesc() { }
        public ReactorDesc(XElement e) {
            maxOutput = e.ExpectAttributeInt(nameof(maxOutput));
            capacity = e.ExpectAttributeInt(nameof(capacity));
            efficiency = e.TryAttributeDouble(nameof(efficiency), 1);
            battery = e.TryAttributeBool(nameof(battery), false);
        }
    }
    public class MiscDesc {
        public bool missileJack;
        public int interval;
        public MiscDevice GetMisc(Item i) => new MiscDevice(i, this);
        public MiscDesc() { }
        public MiscDesc(XElement e) {
            missileJack = e.TryAttributeBool(nameof(missileJack), false);
            interval = e.ExpectAttributeInt(nameof(interval));
        }
    }
}
