﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace IslandHopper {
	public static class Helper {
		public static bool CalcAim(Point3 difference, double speed, out double lower, out double higher) {
			double horizontal = difference.xy.Magnitude;
			double vertical = difference.z;
			const double g = 9.8;
			double part1 = speed * speed;
			double part2 = Math.Sqrt(Math.Pow(speed, 4) - g * ((g * horizontal * horizontal) + (2 * vertical * speed * speed)));

			if(double.IsNaN(part2)) {
				lower = higher = 0;
				return false;
			} else {
				lower = Math.Atan2(part1 - part2, (g * horizontal));
				higher = Math.Atan2(part1 + part2, (g * horizontal));
				return true;
			}
		}
		public static bool InRange(double n, double center, double maxDistance) {
			return n > center - maxDistance && n < center + maxDistance;
		}
		public static bool AreKeysPressed(this SadConsole.Input.Keyboard keyboard, params Keys[] keys) {
			foreach(var key in keys) {
				if (!keyboard.IsKeyPressed(key))
					return false;
			}
			return true;
		}
		/*
		public static bool InRange(double n, double min, double max) {
			return n > min && n < max;
		}
		*/
		public static int LineLength(this string lines) {
			return lines.IndexOf('\n');
		}
		public static int LineCount(this string lines) {
			/*
			int i = 0;
			int result = 1;
			while((i = lines.IndexOf('\n', i)) != -1) {
				result++;
			}
			return result;
			*/
			return lines.Split('\n').Length;
		}
		public static T LastItem<T>(this List<T> list) => list[list.Count - 1];
		public static T FirstItem<T>(this List<T> list) => list[0];
		public static string FlipLines(this string s) {
			var lines = new List<string>(s.Split('\n'));
			lines.Reverse();
			StringBuilder result = new StringBuilder(s.Length - s.LineCount());
			for(int i = 0; i < lines.Count-1; i++) {
				result.AppendLine(lines[i]);
			}
			result.Append(lines.LastItem());
			return result.ToString();
		}
		public static void PrintLines(this SadConsole.Console console, int x, int y, string lines, Color? foreground = null, Color? background = null, SpriteEffects? mirror = null) {
			foreach (var line in lines.Replace("\r\n", "\n").Split('\n')) {
				console.Print(x, y, line, foreground, background, mirror);
				y++;
			}
		}
		public static int Amplitude(this Random random, int amplitude) => random.Next(-amplitude, amplitude);
		public static bool HasElement(this XElement e, string key, out XElement result) {
			return (result = e.Element(key)) != null;
		}
		public static string TryAttribute(this XElement e, string attribute, string fallback = "") {
			return e.Attribute(attribute)?.Value ?? fallback;
		}
		public static int TryAttributeInt(this XElement e, string attribute, int fallback = 0) {
			return e.TryAttribute(attribute).ParseInt(fallback);
		}

		public static int ParseInt(this string s, int fallback = 0) {
			return int.TryParse(s, out int result) ? result : fallback;
		}
		public static int ParseIntMin(this string s, int min, int fallback = 0) {
			return Math.Max(s.ParseInt(fallback), min);
		}
		public static int ParseIntMax(this string s, int max, int fallback = 0) {
			return Math.Min(s.ParseInt(fallback), max);
		}
		public static int ParseIntBounded(this string s, int min, int max, int fallback = 0) {
			return Range(min, s.ParseInt(fallback), max);
		}
		public static int Range(int min, int max, int n) {
			return Math.Min(max, Math.Max(min, n));
		}
		public static bool ParseBool(this string s, bool fallback = false) {
			return s == "true" ? true : s == "false" ? false : fallback;
		}
		public static bool TryAttributeBool(this XElement e, string attribute, bool fallback = false) {
			return e.TryAttribute("attribute").ParseBool(fallback);
		}
		/*
		public static bool? ParseBool(this string s, bool? fallback = null) {
			switch(s) {
				case "true":
					return true;
				case "false":
					return false;
				default:
					return null;
			}
		}
		*/
		/*
		public static Func<int> ParseIntGenerator(string s) {

		}
		*/
		public static TEnum ParseEnum<TEnum>(this XAttribute a, TEnum fallback = default) where TEnum : struct {
			if(a == null) {
				return fallback;
			} else if(Enum.TryParse<TEnum>(a.Value, out TEnum result)) {
				return result;
			} else {
				return fallback;
			}
			
		}
		public static TValue TryLookup<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key, TValue fallback = default) {
			if(d.ContainsKey(key)) {
				return d[key];
			} else {
				return fallback;
			}
		}

		public static int CalcAccuracy(int difficulty, int skill, Random karma) {
			if(skill > difficulty) {
				return 100;
			} else {
				int miss = difficulty - skill;
				return 100 - karma.Next(miss);
			}
		}
		//Chance that the shot is blocked by an obstacle
		public static bool CalcBlocked(int coverage, int accuracy, Random karma) {
			return karma.Next(coverage) > karma.Next(accuracy);
		}
	}
}

