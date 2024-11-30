using RDR2;
using RDR2.Native;
using RDR2.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PolkaSurvival {
	public class PolkaSurvival : Script {

		private readonly int healingThreshold;
		//16 is when the core turns red.
		private readonly int hurtingThreshold = 16;
		private readonly int hurtingTimeToDieMax;
		private readonly int hurtingTimeToDieMin;
		private int hurtTimer;

		/* Passive core drain stuff */
		//This is based off in-game time instead of Game.GameTime;
		private readonly float coresToDrainPerDay;
		//In game day ms
		// 8am-9am: 156752
		// 2pm-3pm: 154965
		// 11pm-12am: 64968
		// 3am-4am: 88320
		// One in-game day: 48 real world minutes;
		private readonly int millisecondsPerDay = 48 * 60 * 1000;
		private readonly int secondsPerDay = 24 * 60 * 60;
		private int dayDrainTimer;
		private int dayDrainInterval;


		//16 is when the core turns red.
		private readonly bool staminaHurtsHealth;
		private readonly int staminaThreshold = 16;
		private readonly int staminaHurtIntervalMax = 20000;
		private readonly int staminaHurtIntervalMin = 6000;
		private int staminaHurtTimer;

		private bool enableHypothermia;
		private bool isHalfWet = false;
		private bool waterTouched = false;
		private int waterTouchedTime;
		private int timeTillHalfWet;
		private int lastHalfWetTime;
		private int halfWetDryTime;

		/*
		 * Hot:  27+ >80F
		 * Warm: 27 / 80F
		 * Mild: 21 / ~70F
		 * Cold: 10 / 50f
		 * Freezing: 0 /32f
		 */

		private TemperatureBandIntervals halfWetIntervals = new TemperatureBandIntervals();
		private int halfWetTimer;

		private bool isFullWet = false;
		private int lasFullfWetTime;
		private int fullWetDryTime = 15000;
		private TemperatureBandIntervals fullWetIntervals = new TemperatureBandIntervals();
		private int fullWetTimer;


		private string _debug;
		private bool showDebug = false;
		private bool disbleAll = false;

		public PolkaSurvival() {
			
			disbleAll = Settings.GetValue("MISC", "DISABLE_ALL_FEATURES", false);

			if (disbleAll) {
				PLAYER._ENABLE_EAGLEEYE(Game.Player, true);
				PLAYER._SET_SPECIAL_ABILITY_DURATION_COST(Game.Player, 4.27f);
				PLAYER._SET_SPECIAL_ABILITY_ACTIVATION_COST(Game.Player, 4.25f, 0);
			} else {
				PLAYER._ENABLE_EAGLEEYE(Game.Player, Settings.GetValue("Misc", "ENABLE_EAGLE_EYE", false));
				PLAYER._SET_SPECIAL_ABILITY_DURATION_COST(Game.Player, Settings.GetValue("DEADEYE", "DEADEYE_DURATION_COST", 10f));
				PLAYER._SET_SPECIAL_ABILITY_ACTIVATION_COST(Game.Player, Settings.GetValue("DEADEYE", "DEADEYE_ACTIVATION_COST", 10f), 0);
			}

			healingThreshold = Settings.GetValue("HEALTH CORE", "HEALING_THRESHHOLD", 50);
			coresToDrainPerDay = Settings.GetValue("HEALTH_CORE", "HEALTH_CORE_DRAINS_DAILY", 2);

			hurtingTimeToDieMax = (int)(Settings.GetValue("HURTING", "HURTING_TIME_TO_DIE_MAX", 3f) * 60 * 1000);
			hurtingTimeToDieMin = (int)(Settings.GetValue("HURTING", "HURTING_TIME_TO_DIE_MAX", 1.5f) * 60 * 1000);

			staminaHurtsHealth = Settings.GetValue("HEALTH CORE", "STAMINA_HURTS_HEALTH_CORE", true);

			enableHypothermia = Settings.GetValue("HYPOTHERMINA", "ENABLE_HYPOTHERMINA", true);
			timeTillHalfWet = (int)Settings.GetValue("HYPOTHERMINA", "TIME_TO_GET_WET", 5f) * 1000;
			halfWetDryTime = (int)Settings.GetValue("HYPOTHERMINA", "TIME_TO_DRY", 30f) * 1000;
			fullWetDryTime = halfWetDryTime / 2;

			hurtTimer = Game.GameTime;
			staminaHurtTimer = Game.GameTime;
			halfWetTimer = Game.GameTime;
			fullWetTimer = Game.GameTime;

			// 50 ticks at 2 hp per core;
			dayDrainInterval = (int)Math.Round(millisecondsPerDay / 50 / coresToDrainPerDay);
			dayDrainTimer = Game.GameTime + dayDrainInterval;


			halfWetIntervals.Warm = 10000;
			halfWetIntervals.Mild = 8000;
			halfWetIntervals.Cold = 6000;
			halfWetIntervals.Freezing = 3000;

			fullWetIntervals.Warm = 5000;
			fullWetIntervals.Mild = 4000;
			fullWetIntervals.Cold = 3000;
			fullWetIntervals.Freezing = 1000;

			Tick += OnTick;
			KeyDown += OnKeyDown;
			Interval = 1;

			if (!disbleAll) {
				RDR2.UI.Screen.DisplaySubtitle($"Polka Survival Activated");
			}

		}



		private void OnTick(object sender, EventArgs evt) {
			if (disbleAll) {
				return;
			}
			_debug = string.Empty;

			AddDebugMessage(() => $"Clock: {CLOCK.GET_CLOCK_HOURS()}:{CLOCK.GET_CLOCK_MINUTES()}:{CLOCK.GET_CLOCK_SECONDS()} - {CLOCK.GET_CLOCK_MONTH()}\n");

			// Our current player ped
			// Note: "Ped" is a class, not a type alias like it is in C++
			Ped myPlayerPed = Game.Player.Character;
			var playerTemperaure = Math.Round(MISC._GET_TEMPERATURE_AT_COORDS(myPlayerPed.Position), 2);


			//Health Core Changes
			if (myPlayerPed.Cores.Health.Value >= healingThreshold || myPlayerPed.Cores.Health.IsOverpowered) {
				var scale = myPlayerPed.Cores.Health.IsOverpowered ? 1f : ScaleToRange(myPlayerPed.Cores.Health.Value, healingThreshold, 100);
				AddDebugMessage(() => $"Healing Scale: {scale}\n");
				PLAYER.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER(Game.Player, scale);
			} else {
				PLAYER.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER(Game.Player, 0f);
				AddDebugMessage(() => $"Healing Scale: {0}\n");
			}

			//Start hurting player if health core is in the red.
			if (myPlayerPed.Cores.Health.Value <= hurtingThreshold) {
				AddDebugMessage(() => $"Hurting due to health core\n");
				if (Game.GameTime >= hurtTimer) {

					//Max HP adjusted for current core damage. (-1hp for every 1% percent core damage)
					var maxHealth = ENTITY.GET_ENTITY_MAX_HEALTH(PLAYER.PLAYER_PED_ID(), false) - (100 - myPlayerPed.Cores.Health.Value);

					var hurtTimeToDie = MapToRange(myPlayerPed.Cores.Health.Value, 0, hurtingThreshold, hurtingTimeToDieMin, hurtingTimeToDieMax);
					int hurtInterval = (int)Math.Ceiling(hurtTimeToDie * .01f);
					hurtTimer = Game.GameTime + hurtInterval;
					ENTITY._CHANGE_ENTITY_HEALTH(PLAYER.PLAYER_PED_ID(), (maxHealth * .01f) * -1, 0, 0);

				}
			}

			//Hurt health core if stamina core is drained
			if (staminaHurtsHealth && myPlayerPed.Cores.Stamina.Value <= staminaThreshold) {
				int staminaHurtInterval = MapToRange(myPlayerPed.Cores.Stamina.Value, 0, staminaThreshold, staminaHurtIntervalMin, staminaHurtIntervalMax);
				AddDebugMessage(() => $"Health Drain due to stamina: {staminaHurtInterval}\n");
				AddDebugMessage(() => $"Next Drain in {staminaHurtTimer - Game.GameTime}\n");
				if (Game.GameTime >= staminaHurtTimer) {
					staminaHurtTimer = Game.GameTime + staminaHurtInterval;
					myPlayerPed.Cores.Health.Value -= 2;
				}
			}


			AddDebugMessage(() => $"Day Drain Intereval: {dayDrainInterval}\n");
			AddDebugMessage(() => $"Next Drain in {dayDrainTimer - Game.GameTime}\n");
			//Extra core drains
			//Note: Depleting by 1 seems to cause an issue where it will occasionally jump back up by 1, causing it to never drain below that value.
			if (Game.GameTime >= dayDrainTimer) {
				myPlayerPed.Cores.Health.Value -= 2;
				myPlayerPed.Cores.Stamina.Value -= 2;
				dayDrainTimer = Game.GameTime + dayDrainInterval;
			}

			//Watery Stuff
			if (enableHypothermia) {
				if (myPlayerPed.IsInWater) {
					AddDebugMessage(() => "In Water (half)\n");
					if (!waterTouched) {
						waterTouched = true;
						waterTouchedTime = Game.GameTime + timeTillHalfWet;
					} else if (waterTouched && Game.GameTime >= waterTouchedTime) {
						lastHalfWetTime = Game.GameTime;
						isHalfWet = true;
					}
				} else if (!myPlayerPed.IsInWater && waterTouched && Game.GameTime <= waterTouchedTime) {
					waterTouched = false;
				}


				if (myPlayerPed.IsSwimmingUnderwater || myPlayerPed.IsUnderwater || myPlayerPed.IsSwimming) {
					AddDebugMessage(() => "In Water (Full)\n");
					lasFullfWetTime = Game.GameTime;
					lastHalfWetTime = Game.GameTime;
					isFullWet = true;
					isHalfWet = true;
				}

				if (Game.GameTime >= waterTouchedTime) {
					waterTouched = false;
				}

				int halfWetDryDown = (lastHalfWetTime + halfWetDryTime) - Game.GameTime;
				if (halfWetDryDown <= 0) {
					isHalfWet = false;
				} else {
					AddDebugMessage(() => $"Half Wet Dry Down in {(lastHalfWetTime + halfWetDryTime) - Game.GameTime}\n");
				}

				int fullWetDryDown = lasFullfWetTime + fullWetDryTime - Game.GameTime;
				if (fullWetDryDown < 0) {
					isFullWet = false;
				} else {
					AddDebugMessage(() => $"Full Wet Dry Down in {(lasFullfWetTime + fullWetDryTime) - Game.GameTime}\n");
				}

				if (isHalfWet) {
					AddDebugMessage(() => $"[Half Wet]\n");

					if (playerTemperaure <= 27) {
						_debug += $"Draining due to Half Wet in {halfWetTimer - Game.GameTime}\n";
						if (Game.GameTime >= halfWetTimer) {
							AddDebugMessage(() => "Half Draining\n");
							myPlayerPed.Cores.Health.Value -= 1;
							halfWetTimer = Game.GameTime + halfWetIntervals.GetIntervalByTemp(playerTemperaure);
						}
					}
				}

				if (isFullWet) {
					AddDebugMessage(() => $"[Full Wet]\n");
					AddDebugMessage(() => $"Drain due to full wet in {fullWetTimer - Game.GameTime}\n");
					if (playerTemperaure <= 27) {
						if (Game.GameTime >= fullWetTimer) {
							myPlayerPed.Cores.Health.Value -= 1;
							fullWetTimer = Game.GameTime + fullWetIntervals.GetIntervalByTemp(playerTemperaure);
						}
					}
				}

				if (!isFullWet && !isHalfWet) {
					myPlayerPed.WetnessHeight = 0f;
				}
				AddDebugMessage(() => $"[Water Touched] {waterTouched}\n");
			}

			AddDebugMessage(() => $"[Cores] Health {myPlayerPed.Cores.Health.Value}, Stamina {myPlayerPed.Cores.Stamina.Value}, DeadEye {myPlayerPed.Cores.DeadEye.Value}\n");
			AddDebugMessage(() => $"Temp {playerTemperaure}c");

			if (showDebug) {
				TextElement textElement = new TextElement($"{_debug}", new PointF(200.0f, 200.0f), 0.35f);
				textElement.Draw();
			}
		}

		private void OnKeyDown(object sender, KeyEventArgs e) {
			// (Keyboard Only)
			//if (e.KeyCode == Keys.F10) {

			//}
		}

		public float ScaleToRange(float value, int minValue, int maxValue) {
			// Ensure maxValue is greater than minValue to avoid division by zero or negative ranges
			if (maxValue <= minValue)
				throw new ArgumentException("maxValue must be greater than minValue.");

			// Clamp the value between minValue and maxValue to prevent out-of-bounds inputs
			float clampedValue = Math.Max(minValue, Math.Min(value, maxValue));

			// Scale the clamped value to the range [0, 1]
			return (clampedValue - minValue) / (maxValue - minValue);
		}

		public int MapToRange(int value, int inMin, int inMax, int outMin, int outMax) {
			// Ensure that the input value is within the input range
			if (value < inMin || value > inMax) {
				throw new ArgumentOutOfRangeException(nameof(value), "Value must be within the input range.");
			}

			// Calculate the proportion of value within the input range
			double proportion = (double)(value - inMin) / (inMax - inMin);

			// Scale it to the output range and round the result to an int
			return (int)Math.Round(outMin + (proportion * (outMax - outMin)));
		}

		public int InGameClockToSeconds() {
			return (CLOCK.GET_CLOCK_HOURS() * 60 * 60) + (CLOCK.GET_CLOCK_MINUTES() * 60) + CLOCK.GET_CLOCK_SECONDS();
		}

		public string TimerToInGameClock() {
			TimeSpan time = TimeSpan.FromSeconds(dayDrainTimer);
			return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
		}

		public void AddDebugMessage(Func<string> message) {
			if (showDebug) {
				_debug += message();
			}
		}
		public void SetNextClockInterval() {
			dayDrainTimer += dayDrainInterval;
			if (dayDrainTimer > secondsPerDay) {
				dayDrainTimer -= secondsPerDay;
			}
		}

	}

	public class TemperatureBandIntervals {

		public int Freezing { get; set; }
		public int Cold { get; set; }
		public int Mild { get; set; }
		public int Warm { get; set; }

		/*
		 * Hot:  27+ >80F
		 * Warm: 27 / 80F
		 * Mild: 21 / ~70F
		 * Cold: 10 / 50f
		 * Freezing: 0 / 32f
		 */

		public int GetIntervalByTemp(double tempc) {
			if (tempc <= 0) {
				return Freezing;
			} else if (tempc > 0 && tempc <= 10) {
				return Cold;
			} else if (tempc > 10 && tempc <= 21) {
				return Mild;
			} else if (tempc > 21 && tempc <= 27) {
				return Warm;
			}
			return 0;
		}
	}

}
