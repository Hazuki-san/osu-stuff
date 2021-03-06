﻿//#define DISABLE_PLUGIN

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Xna.Framework;
using osu.GameModes.Options;
using osu.GameModes.Play.Rulesets;
using osu.Graphics.Sprites;
using osu_common.Bancho.Objects;
using osu_common.Helpers;
using osu_patch;
using osu_patch.Editors;
using osu_patch.Explorers;
using osu_patch.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OsuPatchPlugin.Misc
{
	public class MiscPlugin : IOsuPatchPlugin
	{
		private const string OSU_BASE_URL = "osu.ppy.sh";


#if DISABLE_PLUGIN
		public IEnumerable<Patch> GetPatches() => new List<Patch>();
#else
		public IEnumerable<Patch> GetPatches() => new[]
		{
			new Patch("Local offset change while paused", (patch, exp) =>
			{
				// literally first 10 instructions
				exp["osu.GameModes.Play.Player"]["ChangeCustomOffset"].Editor.LocateAndNop(new[]
				{
					OpCodes.Ldsfld, // Player::Paused
					OpCodes.Brtrue, // ret
					OpCodes.Ldsfld, // Player::Unpausing
					OpCodes.Brtrue, // ret
					OpCodes.Ldsfld, // --
					OpCodes.Ldarg_0, // --
					OpCodes.Ldfld, // -- 
					OpCodes.Ldc_I4, // --
					OpCodes.Add, // --
					OpCodes.Ble_S, // ^^ AudioEngine.Time > this.firstHitTime + 10000 && ...
					OpCodes.Ldsfld, // --
					OpCodes.Brtrue_S, // --
					OpCodes.Ldsfld, // --
					OpCodes.Brtrue_S, // ^^ ... !GameBase.TestMode && !EventManager.BreakMode
					OpCodes.Ret
				});

				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Saving failed replays", (patch, exp) =>
			{
				var Player = exp["osu.GameModes.Play.Player"];
				var onKeyPressed = Player["onKeyPressed"];

				var loc = onKeyPressed.Editor.Locate(new[]
				{
					OpCodes.Ldsfld,    // 0 
					OpCodes.Brfalse,   // 1
					OpCodes.Call,	   // 2
					OpCodes.Brtrue,	   // 3
					OpCodes.Ldsfld,    // 4 
					OpCodes.Brfalse_S, // 5
					OpCodes.Ldarg_2,   // 6
					OpCodes.Ldc_I4_S,  // 7
					OpCodes.Bne_Un_S,  // 8
					OpCodes.Ldsfld,	   // 9
					OpCodes.Brfalse_S, // 10
					OpCodes.Ldarg_0,   // 11 // NOP
					OpCodes.Call,	   // 12 // NOP // this.HandleScoreSubmission();
					OpCodes.Ldsfld,	   // 13
					OpCodes.Stsfld,	   // 14
					OpCodes.Ldc_I4_1,  // 15
					OpCodes.Call,	   // 16
					OpCodes.Ldc_I4_1,  // 17
					OpCodes.Stsfld,	   // 18
					OpCodes.Ldc_I4_2,  // 19
					OpCodes.Ldc_I4_1,  // 20
					OpCodes.Ldc_I4_0,  // 21
					OpCodes.Call,	   // 22
					OpCodes.Ldc_I4_1,  // 23
					OpCodes.Ret		   // 24 
				}, false);

				var ExportReplay = exp["osu.GameplayElements.Scoring.ScoreManager"]["ExportReplay"].Method;
				var currentScore = Player.FindField("currentScore");

				onKeyPressed.Editor.NopAt(loc + 11, 2);
				onKeyPressed.Editor.InsertAt(loc + 15, new[]
				{
					Instruction.Create(OpCodes.Ldsfld, currentScore),
					Instruction.Create(OpCodes.Ldc_I4_1),
					Instruction.Create(OpCodes.Call, ExportReplay),
					Instruction.Create(OpCodes.Pop)
				});

				/*
				if (Player.Failed && k == Keys.F1 && Player.currentScore != null)
				{
					this.HandleScoreSubmission(); <<< removing (noping) this
					InputManager.ReplayScore = Player.currentScore;
					ScoreManager.ExportReplay(Player.currentScore, true); <<< Inserts this
					InputManager.set_ReplayMode(true);
					InputManager.ReplayToEnd = true;
					GameBase.ChangeModeInstant(OsuModes.Play, true, 0);
					return true;
				} 
				*/
				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Smooth cursor trail", (patch, exp) =>
			{
				var add = exp["osu.Graphics.Renderers.CursorTrailRenderer"]["add"].Editor;
				var loc = add.Locate(new[]
				{
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Ldc_R4,
					OpCodes.Add
				});

				add.InsertAt(loc + 2, Instruction.Create(OpCodes.Ldc_R4, 0.8f), InsertMode.Overwrite);

				var trailUpdate = exp["osu.Input.InputManager"]["updateCursorTrail"].Editor;

				var trailLocation = trailUpdate.Locate(new[]
				{
					OpCodes.Ldloc_0,  // 0
					OpCodes.Callvirt, // 1
					OpCodes.Conv_R4,  // 2 <- nop by patch
					OpCodes.Ldc_R4,   // 3 <- nop by patch
					OpCodes.Mul,	  // 4 <- changed to conv.r4
					OpCodes.Ldsfld,   // 5
					OpCodes.Callvirt, // 6
					OpCodes.Mul,	  // 7
					OpCodes.Ldsfld,	  // 8
					OpCodes.Ldfld,	  // 9
					OpCodes.Mul,	  // 10
					OpCodes.Ldc_R4,   // 11 <- replaced by 10.5f
					OpCodes.Div,	  // 12
					OpCodes.Stloc_S	  // 13
				});

				trailUpdate.NopAt(trailLocation + 2, 2);
				trailUpdate.ReplaceAt(trailLocation + 4, Instruction.Create(OpCodes.Conv_R4)); // change mul to conv.r4
				trailUpdate.ReplaceAt(trailLocation + 11, Instruction.Create(OpCodes.Ldc_R4, 10.5f));

				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("No minimum delay before pausing again", (patch, exp) =>
			{
				// first 27 instructions
				exp["osu.GameModes.Play.Player"]["togglePause"].Editor.LocateAndNop(new[]
				{
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Call,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Brfalse_S,
					OpCodes.Ldsfld,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Sub,
					OpCodes.Ldc_I4,
					OpCodes.Bge_S,
					OpCodes.Ldc_I4,
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Ret
				});

				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("No \"mouse buttons are disabled\" message", (patch, exp) =>
			{
				/*
				 *	if (!warningMouseButtonsDisabled && ConfigManager.sMouseDisableButtons)
				 *	{
				 *		warningMouseButtonsDisabled = true;
				 *		NotificationManager.ShowMessage(string.Format(LocalisationManager.GetString(OsuString.InputManager_MouseButtonsDisabledWarning), BindingManager.For(Bindings.DisableMouseButtons)), Color.Beige, 10000);
				 *	}
				 *
				 */

				exp["osu.GameModes.Play.Player"]["Initialize"].Editor.LocateAndNop(new[]
				{
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Call,
					OpCodes.Brfalse_S,
					OpCodes.Ldc_I4_1,
					OpCodes.Stsfld,
					OpCodes.Ldc_I4,
					OpCodes.Call,
					OpCodes.Ldc_I4_S,
					OpCodes.Call,
					OpCodes.Box,
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Ldc_I4,
					OpCodes.Ldnull,
					OpCodes.Call
				});

				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Don't send frames to spectators (NoSpectator)", (patch, exp) =>
			{
				/*
				 * exp.Options.Add();
				 */

				var ConfigManager = exp["osu.Configuration.ConfigManager"].Type;

				var BindableBool = exp["osu.Helpers.BindableBool"].Type;

				var noSpec = new FieldDefUser("noSpec", new FieldSig(BindableBool.ToTypeSig()), FieldAttributes.Assembly | FieldAttributes.Static); // <-- creating new field (internal static BindableBool noSpec)

				ConfigManager.Fields.Add(noSpec); // <-- add field to class

				var ConfigEditor = exp["osu.Configuration.ConfigManager"]["Initialize"].Editor;

				var ReadBool = exp["osu.Configuration.ConfigManager"]["ReadBool"].Method;

				var ConfigLoc = ConfigEditor.Locate(new[]
				{
					OpCodes.Ldsfld,		// 1
					OpCodes.Callvirt,	// 2
					OpCodes.Br_S,		// 3
					OpCodes.Call,		// 4
					OpCodes.Ldsfld,		// 5
					OpCodes.Callvirt,   // 6 
					OpCodes.Call,		// 7
					OpCodes.Stsfld,		// 8
										// ?? <-- start of our own
				}, false);
				ConfigEditor.InsertAt(ConfigLoc + 8, new[] {
					Instruction.Create(OpCodes.Ldstr, "noSpec"), // <-- name of our option in config
					Instruction.Create(OpCodes.Ldc_I4_0),
					Instruction.Create(OpCodes.Call, ReadBool),
					Instruction.Create(OpCodes.Stsfld, noSpec),
				});

				var OptionsEditor = exp["osu.GameModes.Options.Options"]["InitializeOptions"].Editor;
				var OptionsLoc = OptionsEditor.Locate(new[]
				{
					OpCodes.Callvirt, // 0
					OpCodes.Ldloc_1,  // 1
					OpCodes.Call,	  // 2
					OpCodes.Ldarg_0,  // 3		
					OpCodes.Call,	  // 4		from 4 to 6 { this.Add(new OptionVersion(General.get_BUILD_NAME())); }
					OpCodes.Newobj,   // 5		
					OpCodes.Call	  // 6
									  // <-- Insert our option
				}, false);
				var OptionCategory = exp["osu.GameModes.Options.OptionCategory"].FindMethodRaw(".ctor").Method;
				var OptionSection = exp["osu.GameModes.Options.OptionSection"].Type;
				var OptionElement = exp["osu.GameModes.Options.OptionElement"].Type;
				var OptionSectionCtor = exp["osu.GameModes.Options.OptionSection"].FindMethodRaw(".ctor", MethodSig.CreateInstance(exp.CorLibTypes.Void, exp.CorLibTypes.String, exp.ImportAsTypeSig(typeof(IEnumerable<string>)))).Method;
				var optionCheckbox = exp["osu.GameModes.Options.OptionCheckbox"].FindMethodRaw(".ctor", MethodSig.CreateInstance(exp.CorLibTypes.Void, exp.CorLibTypes.String, exp.CorLibTypes.String, BindableBool.ToTypeSig(), exp.ImportAsTypeSig(typeof(EventHandler)))).Method;
				var set_Children = exp["osu.GameModes.Options.OptionElement"]["set_Children"].Method;
				var optionAdd = exp["osu.GameModes.Options.Options"]["Add"].Method; 

				//OptionsEditor.NopAt(OptionsLoc, 6); // removing build name -- 

				// Null operands, idk what causing this atm

				/*OptionsEditor.InsertAt(OptionsLoc + 6, (Options @this) =>
				{
					// Putting something as OsuString will causing that compiler won't want to give us the dll, 
				    // maybe if you make your own OsuString it will work, idk
					@this.Add(new OptionCategory(0, (FontAwesome)0xF09B)
					{
						children = new[]
						{
							new OptionSection("Custom")
							{
								children = new OptionElement[]
								{
									new OptionCheckbox("No Spectators", "Players can't spectate you", true)
								}
							}
						}
					}); 
				}); */

				OptionsEditor.InsertAt(OptionsLoc + 6, new[]
				{
					Instruction.Create(OpCodes.Ldarg_0),							 //						|
					Instruction.Create(OpCodes.Ldstr, "Custom"),					 // name of category    |
					Instruction.Create(OpCodes.Ldc_I4, 0xF09B),						 // github icon			|	our own optionCategory
					Instruction.Create(OpCodes.Newobj, OptionCategory),				 //						|
					Instruction.Create(OpCodes.Stloc_1),							 //						|
					Instruction.Create(OpCodes.Ldloc_1),
					Instruction.Create(OpCodes.Ldc_I4_1),
					Instruction.Create(OpCodes.Newarr, OptionSection),
					Instruction.Create(OpCodes.Dup),
					Instruction.Create(OpCodes.Ldc_I4_0),
					Instruction.Create(OpCodes.Ldstr, "Some stuff"),				 // name of our section	| <-- option section starts here
					Instruction.Create(OpCodes.Ldnull),								 // passed null as keywords
					Instruction.Create(OpCodes.Newobj, OptionSectionCtor),
					Instruction.Create(OpCodes.Stloc_0),						     // end of initializing optionSection
					Instruction.Create(OpCodes.Ldloc_0),							 // start of optionElement (array with options)
					Instruction.Create(OpCodes.Ldc_I4_1),
					Instruction.Create(OpCodes.Newarr, OptionElement),
					Instruction.Create(OpCodes.Dup),								 // our option starts here
					Instruction.Create(OpCodes.Ldc_I4_0),
					Instruction.Create(OpCodes.Ldstr, "No Spectators"),				 // name of our option
					Instruction.Create(OpCodes.Ldstr, "Players can't spectate you"), // tooltip of our option
					Instruction.Create(OpCodes.Ldsfld, noSpec),						 // reference to ConfigManager.noSpec
					Instruction.Create(OpCodes.Ldnull),
					Instruction.Create(OpCodes.Newobj, optionCheckbox),				 // add checkbox [new OptionCheckbox("No Spectators", ConfigManager.noSpec, null)]
					Instruction.Create(OpCodes.Stelem_Ref),							 // end of optionSection
					Instruction.Create(OpCodes.Callvirt, set_Children),
					Instruction.Create(OpCodes.Ldloc_0),
					Instruction.Create(OpCodes.Stelem_Ref),
					Instruction.Create(OpCodes.Callvirt, set_Children),
					Instruction.Create(OpCodes.Ldloc_1),
					Instruction.Create(OpCodes.Call, optionAdd)						// add above to options -- Yeey, everything is done!
				});

				var specEditor = exp["osu.Online.StreamingManager"]["PurgeFrames"].Editor;
				var specLoc = specEditor.Locate(new[] 
				{
					OpCodes.Ldc_I4_S,			
					OpCodes.Ldsfld,
					OpCodes.Ldarg_0,
					OpCodes.Ldloc_3,
					OpCodes.Ldarg_1,
					OpCodes.Stloc_S,
					OpCodes.Ldloca_S,
					OpCodes.Call,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Br_S,
					OpCodes.Ldloca_S,
					OpCodes.Call,
					OpCodes.Ldsfld,
					OpCodes.Dup,
					OpCodes.Ldc_I4_1,
					OpCodes.Add,
					OpCodes.Stsfld,
					OpCodes.Newobj,
					OpCodes.Call,
				}, false);
				var op_Implicit = BindableBool.FindMethod("op_Implicit", MethodSig.CreateStatic(exp.CorLibTypes.Boolean, BindableBool.ToTypeSig()));

				// check if noSpectator option is enabled
				specEditor.Insert(new[]
				{
					Instruction.Create(OpCodes.Ldsfld, noSpec),
					Instruction.Create(OpCodes.Call, op_Implicit),
					Instruction.Create(OpCodes.Brtrue, specEditor[specEditor.Count - 1])
				}); 

				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Don't send anti-cheat flags to Bancho", (patch, exp) =>
			{
				// Startup flags 
				// Remove this part -> (OsuMain.startupValue > 0) ? ("a" + OsuMain.startupValue) // And leave this one -> Scrobbler.last.BeatmapId.ToString()
				exp["osu.Helpers.Scrobbler"]["sendCurrentTrack"].Editor.LocateAndNop(new[]
				{
					OpCodes.Ldsfld,
					OpCodes.Ldc_I4_0,
					OpCodes.Bgt_S
				});

				/* Submit flags // Remove this \/ \/ \/
				 * for (int i = 0; i < (int)Player.flag; i++)
				 * {
				 *      text += " ";
				 * }
				 */
				exp["osu.GameplayElements.Scoring.Score"]["get_onlineFormatted"].Editor.LocateAndNop(new[]
				{
					OpCodes.Ldloc_0,
					OpCodes.Nop,
					OpCodes.Ldstr,
					OpCodes.Call,
					OpCodes.Stloc_0,
					OpCodes.Ldloc_1,
					OpCodes.Ldc_I4_1,
					OpCodes.Add,
					OpCodes.Stloc_1,
					OpCodes.Ldloc_1,
					OpCodes.Ldsfld,
					OpCodes.Blt_S
				});

				return new PatchResult(patch, PatchStatus.Success);;
			}),
			new Patch("Remove check if filename is \"osu!.exe\"", (patch, exp) =>
			{
				exp["osu.OsuMain"]["Main"].Editor.LocateAndNop(new[]
				{
					OpCodes.Call,
					null, // ezstr
					null, // --
					OpCodes.Call,
					OpCodes.Brfalse_S,
					null, // ezstr
					null, // --
					OpCodes.Newobj,
					OpCodes.Ldc_I4_0,
					OpCodes.Newobj,
					OpCodes.Call,
					OpCodes.Pop,
					OpCodes.Ldc_I4_0,
					OpCodes.Call
				});

				return new PatchResult(patch, PatchStatus.Success);;
			}),
			new Patch("Switch servers to Astellia", (patch, exp) =>
			{
				var set_Url = exp["osu_common.Helpers.pWebRequest"]["set_Url"].Editor;
				set_Url.NopAt(0, 17);
				set_Url.InsertAt(0, (pWebRequest @this, string value) =>
				{
					value = value
						.Replace("osu.ppy.sh", "astellia.club")
						.Replace("c.ppy.sh", "c.astellia.club")
						.Replace("c4.ppy.sh", "c.astellia.club")
						.Replace("c5.ppy.sh", "c.astellia.club")
						.Replace("c6.ppy.sh", "c.astellia.club")
						.Replace("a.ppy.sh", "a.astellia.club");
					@this.url = value;
				});
				return new PatchResult(patch, PatchStatus.Success);
			}),
			// Can't find name.
			/*new Patch("Pippi", true, (patch, exp) => 
			{
				var autoPlay = exp["osu.GameModes.Play.Rulesets.Osu.RulesetOsu"]["AddFrameToReplay"].Editor;
				autoPlay.InsertAt(0, (List<bReplayFrame> replay, bReplayFrame frame) =>
				{
					var addAngleAmount = Ruleset.Instance.hitObjectManager.HitObjectRadius * 0.98f;
					var angle = frame.time / 200f;
					Vector2 vector = addAngleAmount * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
					frame.mouseX += vector.X;
					frame.mouseY += vector.Y;
				});
				return new PatchResult(patch, PatchStatus.Success);
			}) */
		}; 
#endif
		public void Load(ModuleDef originalObfOsuModule) { }

#region AsukiAddon
		private class IlStringBuilder
		{
			public List<Instruction> Instructions = new List<Instruction>();

			private byte _stackCounter;

			private static MemberRef _stringConcat;

			public IlStringBuilder(ModuleDef module)
			{
				var mod = _stringConcat?.Module;

				if (mod != null && mod == module)
					return;

				_stringConcat = module.CreateMethodRef(true, typeof(String), "Concat", typeof(string), typeof(string), typeof(string));
			}

			public void Add(Instruction ins)
			{
				Instructions.Add(ins);
				_stackCounter++;

				if (_stackCounter >= 2)
				{
					Instructions.Add(Instruction.Create(OpCodes.Call, _stringConcat));
					_stackCounter = 1;
				}
			}

			public void Add(string str) =>
				Add(Instruction.Create(OpCodes.Ldstr, str));
		}

		private static IList<Instruction> AsukiPatch_CreateServersArrayInitializer(ModuleExplorer exp, IList<string> addrList)
		{
			var ret = new List<Instruction>();

			ret.AddRange(new[]
			{
				Instruction.CreateLdcI4(addrList.Count),
				Instruction.Create(OpCodes.Newarr, exp.Module.CorLibTypes.String)
			});

			for (int i = 0; i < addrList.Count; i++)
			{
				ret.AddRange(new[]
				{
					Instruction.Create(OpCodes.Dup),
					Instruction.CreateLdcI4(i),
					Instruction.Create(OpCodes.Ldstr, addrList[i]),
					Instruction.Create(OpCodes.Stelem_Ref)
				});
			}

			return ret;
		}

		private static IList<Instruction> AsukiPatch_UniversalizeOsuURL(ModuleExplorer exp, string str, FieldDef baseUrlField, string baseUrl = OSU_BASE_URL)
		{
			var parts = str.Split(new[] { baseUrl }, StringSplitOptions.None);

			if (parts.Length == 2)
			{
				var sb = new IlStringBuilder(exp.Module);

				if (!String.IsNullOrEmpty(parts[0]))
					sb.Add(parts[0]);

				sb.Add(Instruction.Create(OpCodes.Ldsfld, baseUrlField));

				if (!String.IsNullOrEmpty(parts[1]))
					sb.Add(parts[1]);

				return sb.Instructions;
			}

			return new List<Instruction> { Instruction.Create(OpCodes.Ldstr, str) };
		}
#endregion
	}
}