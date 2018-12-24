﻿using System.Collections.Generic;

namespace IslandHopper {
	public static class Debug {
		public static List<string> log = new List<string>();
		public static string state = "";
		public static bool printing = true;
		public static void Print(string state) {
			log.Add(state);
			Debug.state = state;
			if(printing)
				System.Console.WriteLine(state);
		}
		public static void Print(bool condition, string state) {
			if (condition)
				Print(state);
		}
		public static void Info(this object o, string message) {
			Print(o.GetType().Name + ">" + message);
		}
		public static void Info(this object o, params string[] message) {
			Print(o.GetType().Name + ">");
			foreach(string s in message) {
				Print("\t" + s);
			}
		}
		/*
		public static void Info(this object o, params string[] message) {
			System.Console.Write(o.GetType().Name + ">");
			WriteLine(message);
		}
		public static void WriteLine(params string[] s) {
			System.Console.WriteLine(string.Join(">", s));
		}
		*/
	}
}
