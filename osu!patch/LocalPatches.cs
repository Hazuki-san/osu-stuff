using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;

namespace osu_patch
{
	static class LocalPatches
	{
		public static readonly Patch[] PatchList =
		{
			new Patch("\"Unsigned executable\" fix", (patch, exp) =>
			{
				/*	--   REMOVES THIS   \/ \/ \/ \/ \/ \/ \/
					if (!AuthenticodeTools.IsTrusted(OsuMain.get_Filename()))
					{
						new ErrorDialog(new Exception("Unsigned executable!"), false).ShowDialog();
						Environment.Exit(0);
					}
				*/
				exp["osu.OsuMain"]["Main"].Editor.LocateAndNop(new[]
				{
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Brtrue_S,
					null, // obfuscator's string stuff, may be either in decrypted or encrypted state.
					null, // --
					OpCodes.Newobj,
					OpCodes.Ldc_I4_0,
					OpCodes.Newobj,
					OpCodes.Call,
					OpCodes.Pop,
					OpCodes.Ldc_I4_0,
					OpCodes.Call
				});

				var InitOption = exp["osu.GameModes.Options.Options"]["InitializeOptions"].Editor;
				var InitOPLoc = InitOption.Locate(new[]{
					OpCodes.Ldarg_0,
					OpCodes.Call,
					OpCodes.Newobj,
					OpCodes.Call,
					OpCodes.Ldarg_0,
					OpCodes.Call,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldarg_0,
					OpCodes.Ldftn,
					OpCodes.Newobj,
					OpCodes.Call,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Ldfld,
					OpCodes.Callvirt,
					OpCodes.Ldc_I4_1,
					OpCodes.Sub,
					OpCodes.Stloc_S,
					OpCodes.Br_S,
					OpCodes.Ldarg_0
				});
					InitOption.ReplaceAt(InitOPLoc+1, Instruction.Create(OpCodes.Ldstr, "Modded by Aoba Suzukaze | Made for Ainu!"));
				return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!direct server patch", (patch, exp) =>
			{
				// Patch download server to storage.ainu.pw (that's me)
				var osuDirectServer = exp["osu.Online.OsuDirectDownload"].FindMethodRaw(".ctor",
					MethodSig.CreateInstance(exp.CorLibTypes.Void,
						exp.CorLibTypes.Int32,
						exp.CorLibTypes.String,
						exp.CorLibTypes.String,
						exp.CorLibTypes.Boolean,
						exp.CorLibTypes.Int32)).Editor;
				var serverLoc = osuDirectServer.Locate(new[]
				{
					OpCodes.Ldarg_S,
					OpCodes.Brfalse_S,
					null,
					null,
					OpCodes.Br_S,
					null,
					null,
					OpCodes.Stloc_1,
				});
				osuDirectServer.NopAt(serverLoc + 2, 1);
				osuDirectServer.NopAt(serverLoc + 5, 1);
				osuDirectServer.ReplaceAt(serverLoc + 3, Instruction.Create(OpCodes.Ldstr, "https://storage.ainu.pw/d/{0}n"));
				osuDirectServer.ReplaceAt(serverLoc + 6, Instruction.Create(OpCodes.Ldstr, "https://storage.ainu.pw/d/{0}"));

				var downloadServerBackup = exp["osu.Online.OsuDirectDownload"]["DownloadFallback"].Editor;
				var downloadServerLoc = downloadServerBackup.Locate(new[]
				{
					null,
					null,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Ldfld,
					OpCodes.Box,
					OpCodes.Call,
					OpCodes.Ldnull,
					OpCodes.Call,
					OpCodes.Ret,
				});
				downloadServerBackup.NopAt(serverLoc, 1);
				downloadServerBackup.ReplaceAt(downloadServerLoc + 1, Instruction.Create(OpCodes.Ldstr, "https://storage.ainu.pw/d/{0}"));

				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Connect to server patch", (patch, exp) =>
			{
				// Method 01
				// Patch set_Url to my server (most likely to get laggier?)
				// osu.ppy.sh -> ainu.pw
				var setURL = exp["osu_common.Helpers.pWebRequest"]["set_Url"].Editor;
				var setURLLoc = setURL.Locate(new[]
				{
					OpCodes.Ldarg_1,
					null,
					null,
					OpCodes.Callvirt,
					OpCodes.Brtrue_S,
					null,
					null,
					OpCodes.Ldarg_1,
					null,
					null,
					OpCodes.Ldsfld,
					OpCodes.Callvirt,
					OpCodes.Call,
					OpCodes.Starg_S,
				});
				setURL.LocateAndRemove(new[]
				{
					OpCodes.Ldarg_1,
					null,
					null,
					OpCodes.Callvirt,
					OpCodes.Brtrue_S,
					null,
					null,
					OpCodes.Ldarg_1,
					null,
					null,
					OpCodes.Ldsfld,
					OpCodes.Callvirt,
					OpCodes.Call,
					OpCodes.Starg_S,
				});

				// The code below got help from xxCherry!
				// Thank you so much for helping me! :D
				var importer = new Importer(exp.Module);
				var startswith = exp.Module.CreateMethodRef(false, typeof(String), "StartsWith", typeof(bool), typeof(string));
				var contains = exp.Module.CreateMethodRef(false, typeof(String), "Contains", typeof(bool), typeof(string));
				var replace = exp.Module.CreateMethodRef(false, typeof(String), "Replace", typeof(string),  typeof(string), typeof(string));
				var concat = exp.Module.CreateMethodRef(true, typeof(String), "Concat", typeof(string),  typeof(string), typeof(string));
				var parameter = setURL.Parent.Method.Parameters[1];
				var instructions = new[] {
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "ppy.sh"),
					Instruction.Create(OpCodes.Callvirt, contains),
					Instruction.Create(OpCodes.Nop), //Brfalse 15
					Instruction.Create(OpCodes.Ldsfld, importer.Import(typeof(string).GetField("Empty"))),
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "osu.ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Ldstr, "ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Call, concat),
					Instruction.Create(OpCodes.Starg_S, parameter),
					Instruction.Create(OpCodes.Nop), //Br.S 26
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "https://"),
					Instruction.Create(OpCodes.Callvirt, startswith),
					Instruction.Create(OpCodes.Nop), //Brtrue.S 26
					Instruction.Create(OpCodes.Ldstr, "https://"),
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "http://"),
					Instruction.Create(OpCodes.Ldsfld, importer.Import(typeof(string).GetField("Empty"))),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Call, concat),
					Instruction.Create(OpCodes.Starg_S, parameter),
					Instruction.Create(OpCodes.Ldarg_0),
				};
				instructions[3] = Instruction.Create(OpCodes.Brfalse_S, instructions[15]);
				instructions[14] = Instruction.Create(OpCodes.Br_S, instructions[26]);
				instructions[18] = Instruction.Create(OpCodes.Brfalse_S, instructions[26]);
				setURL.RemoveAt(setURLLoc);
				setURL.Insert(instructions);

				/*
				// Method 02
				// Patch pWebRequest to my server (patch pWebRequest hardcoded connection.)
				// osu.ppy.sh -> ainu.pw
				var pWebReq = exp["osu_common.Helpers.pWebRequest"].FindMethodRaw(".ctor").Editor;
				var pWebLoc = pWebReq.Locate(new[]
				{
					OpCodes.Ldarg_0,
					OpCodes.Call,
					OpCodes.Ldarg_0
				});
				// The code below got help from xxCherry!
				// Thank you so much for helping me! :D
				var importer = new Importer(exp.Module);
				var contains = exp.Module.CreateMethodRef(false, typeof(String), "Contains", typeof(bool), typeof(string));
				var replace = exp.Module.CreateMethodRef(false, typeof(String), "Replace", typeof(string),  typeof(string), typeof(string));
				var concat = exp.Module.CreateMethodRef(true, typeof(String), "Concat", typeof(string),  typeof(string), typeof(string));
				var parameter = pWebReq.Parent.Method.Parameters[1];
				var instructions = new[] {
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "ppy.sh"),
					Instruction.Create(OpCodes.Callvirt, contains),
					Instruction.Create(OpCodes.Nop),
					Instruction.Create(OpCodes.Ldsfld, importer.Import(typeof(string).GetField("Empty"))),
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "osu.ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Ldstr, "ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Call, concat),
					Instruction.Create(OpCodes.Starg_S, parameter),
					Instruction.Create(OpCodes.Ldarg_0),
				};

				instructions[3] = Instruction.Create(OpCodes.Brfalse_S, instructions[14]);
				pWebReq.RemoveAt(pWebLoc + 2, 1);
				pWebReq.InsertAt(pWebLoc + 2, instructions);
				pWebReq.SimplifyBranches();
				pWebReq.OptimizeBranches();
				*/

				// Method 03
				// Patch bancho server with hardcoding pWebRequest patching (less lag)
				// c[4-6].ppy.sh -> c.ainu.pw
				// osu.ppy.sh -> ainu.pw
				/*
				var BanchoServerList = exp["osu.Online.BanchoClient"].FindMethod(".cctor").Editor;
				var BanchoServerLoc = pWebReq.Locate(new[]
				{
					OpCodes.Ldc_I4_3,
					OpCodes.Newarr,
					OpCodes.Dup,
					OpCodes.Ldc_I4_0,
					null,
					null,
					OpCodes.Stelem_Ref,
					OpCodes.Dup,
					OpCodes.Ldc_I4_1,
					null,
					null,
					OpCodes.Stelem_Ref,
					OpCodes.Dup,
					OpCodes.Ldc_I4_2,
					null,
					null,
					OpCodes.Stelem_Ref,
					OpCodes.Stsfld,
				});
				BanchoServerList.ReplaceAt(BanchoServerLoc, Instruction.Create(OpCodes.Ldc_I4_1));
				BanchoServerList.ReplaceAt(BanchoServerLoc + 5, Instruction.Create(OpCodes.Ldstr, "https://c.ainu.pw"));
				BanchoServerList.NopAt(BanchoServerLoc + 6, 9);
				*/
				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Disable osu! update (Stable method)", (patch, exp) =>
			{
				exp["osu.GameBase"]["CheckForUpdates"].Editor.LocateAndNop(new[]{
					OpCodes.Ldarg_0,
					OpCodes.Brtrue_S,
					OpCodes.Ldarg_1,
					OpCodes.Brtrue_S,
					OpCodes.Call,
					OpCodes.Ldsfld,
					OpCodes.Call,
					OpCodes.Stloc_1,
					OpCodes.Ldloca_S,
					OpCodes.Call,
					OpCodes.Ldc_R8,
					OpCodes.Bge_Un_S,
					//OpCodes.Ret,
				});
				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Disable osu! update (CuttingEdge method)", (patch, exp) => { exp["osu.GameBase"]["CheckForUpdates"].Editor.LocateAndNop(new[]{OpCodes.Ldsfld,OpCodes.Brfalse_S}); return new PatchResult(patch, PatchStatus.Success);}),
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

				add.ReplaceAt(loc + 2, Instruction.Create(OpCodes.Ldc_R4, 0.8f));

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
			new Patch("DiscordRPC patch", (patch, exp) =>
			{
				var DiscordRPC = exp["osu.Online.DiscordStatusManager"].FindMethodRaw(".ctor",MethodSig.CreateInstance(exp.CorLibTypes.Void)).Editor;
				var DiscordLoc = DiscordRPC.Locate(new[]{
					OpCodes.Ldarg_0,
					OpCodes.Newobj,
					OpCodes.Dup,
					OpCodes.Newobj,
					OpCodes.Callvirt,
					OpCodes.Dup,
					OpCodes.Newobj,
					OpCodes.Callvirt,
					OpCodes.Stfld,
					OpCodes.Ldarg_0,
					OpCodes.Call,
					OpCodes.Ldarg_0,
					null,
					null,
					OpCodes.Newobj,
					OpCodes.Stfld,
				});
					DiscordRPC.NopAt(DiscordLoc + 12, 1);
					DiscordRPC.ReplaceAt(DiscordLoc + 13, (Instruction.Create(OpCodes.Ldstr, "509036024660492318")));
				return new PatchResult(patch, PatchStatus.Success);
			}),
			new Patch("Relax and Autopilot have miss and combobreak sound", (patch, exp) =>
			{
				/*
				// Combobreak
				var Combobreak = exp["osu.GameModes.Play.Rulesets.Ruleset"]["IncreaseScoreHit"].Editor;
				var CombobreakLoc = Combobreak.Locate(new[]
				{
					OpCodes.Br,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Callvirt,
					OpCodes.Ldc_I4_S,
					OpCodes.Ble_S,
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
					OpCodes.Ldsfld,
					OpCodes.Brtrue_S,
				});
				Combobreak.NopAt(CombobreakLoc+6, 4);
				// Show misses
				var HitObj = exp["osu.GameplayElements.HitObjects.HitObject"].Type;
				var IncScore = exp["osu.GameModes.Play.Rulesets.IncreaseScoreType"].Type;
				var w = exp["osu.GameplayElements.HitObjectManager"].FindMethod("UhIDKWhatThisIsButThisIsWhereMissShowedUp",
					MethodSig.CreateStatic(
						IncScore.ToTypeSig(),
						HitObj.ToTypeSig())).Editor;
				var wLoc = w.Locate(new[]{OpCodes.Ldarg_0});
				*/
				return new PatchResult(patch, PatchStatus.Disabled);
			}),
			new Patch("HardRock and Random mods ranked for Mania", (patch, exp) =>
			{
				/*
				var ModMan = exp["osu.GameplayElements.Scoring"]["AllowRanking"].Editor;
				var HRRDLoc = ModMan.Locate(new[]
				{
					OpCodes.Call,
					OpCodes.Ldc_I4_3,
					OpCodes.Bne_Un_S,
					OpCodes.Ldarg_0,
					OpCodes.Stloc_2,
					OpCodes.Ldc_I4,
					OpCodes.Ldloc_2,
					OpCodes.And,
					OpCodes.Ldc_I4_0,
					OpCodes.Cgt,
					OpCodes.Brfalse_S,
					OpCodes.Ldc_I4_0,
					OpCodes.Ret,
				});
					ModMan.NopAt(HRRDLoc + 3, 10);
				*/
				return new PatchResult(patch, PatchStatus.Disabled);
			}),
			new Patch("Enable fail for Relax and Autopilot", (patch, exp) =>
			{
				return new PatchResult(patch, PatchStatus.Disabled);
			}),
		};
	}
}