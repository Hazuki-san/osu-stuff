using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;

using OpCodes = dnlib.DotNet.Emit.OpCodes;


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
			new Patch("osu!.exe to ainu.exe", (patch, exp) =>
			{
				// CommonUpdater Patch (Not useful, i guess...)
				var CommonUpd = exp["osu_common.Updater.CommonUpdater"]["doUpdate"].Editor;
				var CommonUpdLoc = CommonUpd.Locate(new[]
					{
						OpCodes.Ldloc_0,
						OpCodes.Ldfld,
						null,
						null,
						OpCodes.Call,
						OpCodes.Brfalse_S,
						OpCodes.Ldloc_0,
						OpCodes.Ldfld,
						OpCodes.Ldc_I4_1,
						OpCodes.Call,
						OpCodes.Ldloc_0,
						OpCodes.Ldfld,
						OpCodes.Ldc_I4_0,
						OpCodes.Call,
						OpCodes.Call,
						OpCodes.Brtrue_S,
					});
					CommonUpd.NopAt(CommonUpdLoc + 2, 1);
					CommonUpd.ReplaceAt(CommonUpdLoc + 3, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				/*
				// Install Dialog (Probably not working lmao)
				var InstallDialog = exp["osu.Helpers.Forms.Maintenance"]["OsuInstall"]["installDialog"].Editor;
				var InstallDialogLoc = InstallDialog.Locate(new[]
				{
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Ldfld,
					null,
					null,
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Brtrue,
				});
					InstallDialog.NopAt(InstallDialogLoc + 3, 1);
					InstallDialog.ReplaceAt(InstallDialogLoc + 4, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				*/
				// Exit or Restart
				var exitOrRestart = exp["osu.OsuMain"]["exitOrRestart"].Editor;
				var exitOrRestartLoc = exitOrRestart.Locate(new[]{
					null,
					null,
					OpCodes.Call,
					OpCodes.Pop,
					OpCodes.Leave_S,
					OpCodes.Pop,
					OpCodes.Leave_S,
					OpCodes.Ldsfld,
					OpCodes.Ldarg_0,
					OpCodes.Or,
					OpCodes.Brfalse_S,
					null,
					null,
					OpCodes.Call,
					OpCodes.Pop,
				});
					exitOrRestart.NopAt(exitOrRestartLoc, 1);
					exitOrRestart.NopAt(exitOrRestartLoc + 11, 1);
					exitOrRestart.ReplaceAt(exitOrRestartLoc + 1, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					exitOrRestart.ReplaceAt(exitOrRestartLoc + 12, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				// Force update
				var ForceUpdate = exp["osu.OsuMain"]["ForceUpdate"].Editor;
				var ForceUpdateLoc = ForceUpdate.Locate(new[]{
					null,
					null,
					OpCodes.Call,
					OpCodes.Pop,
				});
					ForceUpdate.NopAt(ForceUpdateLoc, 1);
					ForceUpdate.ReplaceAt(ForceUpdateLoc + 1, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				// GameBase Initialize
				var GameBaseInit = exp["osu.GameBase"]["Initialize"].Editor;
				var GameBaseInitLoc = GameBaseInit.Locate(new[]
				{
					OpCodes.Call,
					null, // Eaz deobfucator
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Brtrue_S,
					OpCodes.Call,
					null, // Eaz deobfucator
					null, // start osu!.lnk
					OpCodes.Call,
					OpCodes.Stloc_1,
					OpCodes.Ldloc_1,
					OpCodes.Call,
					OpCodes.Brtrue_S,
					OpCodes.Ldloc_1,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					null, // Eaz deobfucator
					null, // osu!
					OpCodes.Ldsfld,
					OpCodes.Call,
					OpCodes.Call,
					null, // Eaz deobfucator
					null, // repair osu!.lnk
					OpCodes.Call,
					OpCodes.Stloc_2,
					OpCodes.Ldloc_2,
					OpCodes.Call,
					OpCodes.Brtrue_S,
					OpCodes.Ldloc_2,
					OpCodes.Call,
					null, // Eaz deobfucator
					null, // osu!.exe
				});
					GameBaseInit.NopAt(GameBaseInitLoc + 1, 1);
					GameBaseInit.NopAt(GameBaseInitLoc + 16, 1);
					GameBaseInit.NopAt(GameBaseInitLoc + 33, 1);
					GameBaseInit.ReplaceAt(GameBaseInitLoc + 2, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					GameBaseInit.ReplaceAt(GameBaseInitLoc + 17, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					GameBaseInit.ReplaceAt(GameBaseInitLoc + 34, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				// Main
				var Main = exp["osu.OsuMain"]["Main"].Editor;
				var MainLoc1 = Main.Locate(new[]
				{
					OpCodes.Ldc_I4_2,
					OpCodes.Newobj,
					OpCodes.Call,
					OpCodes.Ldc_I4_2,
					OpCodes.Bne_Un_S,
					OpCodes.Ret,
					null,
					null,
					OpCodes.Call,
					OpCodes.Pop,
				});
				var MainLoc2 = Main.Locate(new[]
				{
					OpCodes.Ldc_I4_0,
					OpCodes.Newarr,
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Ret,
					OpCodes.Call,
					null,
					null,
					OpCodes.Call,
					OpCodes.Brfalse_S,
					null,
					null,
				});
					Main.NopAt(MainLoc1 + 6, 1);
					Main.ReplaceAt(MainLoc1 + 7, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Main.NopAt(MainLoc2 + 6, 1);
					Main.ReplaceAt(MainLoc2 + 7, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Main.NopAt(MainLoc2 + 10, 1);
					Main.ReplaceAt(MainLoc2 + 11, Instruction.Create(OpCodes.Ldstr, "Executable filename is not correct! Please rename to ainu.exe"));
				// Repair
				var Repair = exp["osu.OsuMain"]["Repair"].Editor;
				var RepairLoc = Repair.Locate(new[]{
					OpCodes.Ldc_I4_1,
					OpCodes.Newarr,
					OpCodes.Dup,
					OpCodes.Ldc_I4_0,
					OpCodes.Ldarga_S,
					OpCodes.Call,
					OpCodes.Stelem_Ref,
					OpCodes.Call,
					OpCodes.Ldarg_1,
					OpCodes.Brfalse_S,
					null,
					null,
					OpCodes.Ldc_I4_1,
					OpCodes.Newarr,
					OpCodes.Dup,
					OpCodes.Ldc_I4_0,
					OpCodes.Ldarg_1,
					OpCodes.Callvirt,
					OpCodes.Stelem_Ref,
					OpCodes.Call,
					null,
					null,
					OpCodes.Ldarg_0,
					OpCodes.Brtrue_S,
					null,
					null,
					OpCodes.Br_S,
					null,
					null,
					OpCodes.Call,
					OpCodes.Pop,
					OpCodes.Call,
					OpCodes.Ret,
				});
					Repair.NopAt(RepairLoc + 20, 1);
					Repair.ReplaceAt(RepairLoc + 21, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				// Restart Immediately
				var RestartImmediately = exp["osu.OsuMain"]["RestartImmediately"].Editor;
				var RestartImmediatelyLoc = RestartImmediately.Locate(new[]{null,null,null,null,OpCodes.Call,OpCodes.Pop,OpCodes.Call,OpCodes.Ret});
					RestartImmediately.NopAt(RestartImmediatelyLoc, 1);
					RestartImmediately.ReplaceAt(RestartImmediatelyLoc + 1, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				// Start-up
				var StartUpCheck = exp["osu.OsuMain"]["startup"].Editor;
				var StartUpLoc = StartUpCheck.Locate(new[]{
					OpCodes.Dup,
					OpCodes.Brtrue_S,
					OpCodes.Pop,
					OpCodes.Ldc_I4_0,
					OpCodes.Br_S,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Brtrue,

					OpCodes.Ldsfld,
					null, // Eaz deobfucator 
					null, // some registry idk
					OpCodes.Callvirt,

					OpCodes.Dup,
					OpCodes.Brtrue_S,
					OpCodes.Pop,
					OpCodes.Ldc_I4_0,
					OpCodes.Br_S,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Brtrue_S,

					OpCodes.Ldsfld,
					null, // Eaz deobfucator 
					null, // some registry idk
					OpCodes.Callvirt,

					OpCodes.Dup,
					OpCodes.Brtrue_S,
					OpCodes.Pop,
					OpCodes.Ldc_I4_0,
					OpCodes.Br_S,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Brtrue_S,

					OpCodes.Ldsfld,
					null, // Eaz deobfucator 
					null, // some registry idk
					OpCodes.Callvirt,

					OpCodes.Dup,
					OpCodes.Brtrue_S,
					OpCodes.Pop,
					OpCodes.Ldc_I4_0,
					OpCodes.Br_S,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Br_S,
				});
					StartUpCheck.NopAt(StartUpLoc + 6, 1);
					StartUpCheck.NopAt(StartUpLoc + 20, 1);
					StartUpCheck.NopAt(StartUpLoc + 34, 1);
					StartUpCheck.NopAt(StartUpLoc + 48, 1);
					StartUpCheck.ReplaceAt(StartUpLoc + 7, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					StartUpCheck.ReplaceAt(StartUpLoc + 21, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					StartUpCheck.ReplaceAt(StartUpLoc + 35, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					StartUpCheck.ReplaceAt(StartUpLoc + 49, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				// Updater
				var Upd = exp["osu.Helpers.Forms.Maintenance"]["update"].Editor;
				var UpdLoc = Upd.Locate(new[]{
					OpCodes.Dup,
					OpCodes.Stloc_S,
					OpCodes.Brfalse_S,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Brfalse_S,
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Stloc_S,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Stloc_2,
					OpCodes.Ldloc_2,
					OpCodes.Ldloc_S,
					OpCodes.Ldc_I4_0,
					OpCodes.Ldloc_S,
					OpCodes.Ldlen,
					OpCodes.Conv_I4,
					OpCodes.Callvirt,
					OpCodes.Leave_S,
					OpCodes.Ldloc_2,
					OpCodes.Brfalse_S,
					OpCodes.Ldloc_2,
					OpCodes.Callvirt,
					OpCodes.Endfinally,
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Pop,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Pop,
					OpCodes.Call,
					OpCodes.Ldloc_0,
					OpCodes.Brtrue,
					OpCodes.Br,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					OpCodes.Stloc_3,
				});
				var Upd2Loc = Upd.Locate(new[]{
					OpCodes.Call,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!
					OpCodes.Ldsfld,
					OpCodes.Call,
					OpCodes.Call,
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!.exe
					OpCodes.Call,
					null, // Eaz deobfucator 
					null, // osu!
					OpCodes.Ldsfld,
					OpCodes.Call,
					OpCodes.Ldarg_0,
					OpCodes.Ldfld,
					OpCodes.Callvirt,
				});
					Upd.NopAt(UpdLoc + 4, 1);
					Upd.NopAt(UpdLoc + 11, 1);
					Upd.NopAt(UpdLoc + 31, 1);
					Upd.NopAt(UpdLoc + 41, 1);
					Upd.NopAt(Upd2Loc + 2, 1);
					Upd.NopAt(Upd2Loc + 11, 1);
					Upd.ReplaceAt(UpdLoc + 5, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Upd.ReplaceAt(UpdLoc + 12, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Upd.ReplaceAt(UpdLoc + 32, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Upd.ReplaceAt(UpdLoc + 42, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Upd.ReplaceAt(Upd2Loc + 3, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Upd.ReplaceAt(Upd2Loc + 12, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
				// Auth Check
				var ACheck = exp["osu_common.Updater.CommonUpdater"]["AuthCheck"].Editor;
				var ACheckLoc = ACheck.Locate(new[]{
					OpCodes.Ldarg_0,
					null,
					null,
					OpCodes.Callvirt,
					OpCodes.Brtrue_S,
					OpCodes.Ldarg_0,
					OpCodes.Ret,
					null,
					null,
					OpCodes.Ret,
				});
					ACheck.NopAt(ACheckLoc + 7, 1);
					ACheck.ReplaceAt(ACheckLoc + 8, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
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
				// Patch pWebRequest to my server
				// osu.ppy.sh -> ainu.pw
				var pWebReq = exp["osu_common.Helpers.pWebRequest"].FindMethodRaw(".ctor").Editor;
				var pWebLoc = pWebReq.Locate(new[]
				{
					OpCodes.Ldarg_0,
					OpCodes.Call,
				});
				// The code below got help from xxCherry!
				// Thank you so much for helping me! :D
				var contains = exp.Module.CreateMethodRef(false, typeof(String), "Contains", typeof(bool), typeof(string));
				var replace = exp.Module.CreateMethodRef(false, typeof(String), "Replace", typeof(string),  typeof(string), typeof(string));
				var concat = exp.Module.CreateMethodRef(true, typeof(String), "Concat", typeof(string),  typeof(string), typeof(string));
				var parameter = pWebReq.Parent.Method.Parameters[1];
				var instructions = new[] {
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "ppy.sh"),
					Instruction.Create(OpCodes.Callvirt, contains),
					Instruction.Create(OpCodes.Nop),
					Instruction.Create(OpCodes.Ldstr, ""),
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ldstr, "osu.ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Ldstr, "c4.ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "c.ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Ldstr, "c5.ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "c.ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Ldstr, "c6.ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "c.ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Ldstr, "ce.ppy.sh"),
					Instruction.Create(OpCodes.Ldstr, "c.ainu.pw"),
					Instruction.Create(OpCodes.Callvirt, replace),
					Instruction.Create(OpCodes.Call, concat),
					Instruction.Create(OpCodes.Starg_S, parameter),
					Instruction.Create(OpCodes.Nop),
					Instruction.Create(OpCodes.Nop),
					Instruction.Create(OpCodes.Nop),
				};

				instructions[3] = Instruction.Create(OpCodes.Brfalse, instructions[23]);
				pWebReq.InsertAt(pWebLoc + 2, instructions);

				// Bancho Patching not working atm
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
			new Patch("Relax and Autopilot have miss and combobreak Sound", (patch, exp) =>
			{
				return new PatchResult(patch, PatchStatus.Disabled);
			}),
			new Patch("HardRock and Random mods ranked for Mania", (patch, exp) =>
			{
				return new PatchResult(patch, PatchStatus.Disabled);
			}),
			new Patch("Enable fail for Relax and Autopilot", (patch, exp) =>
			{
				return new PatchResult(patch, PatchStatus.Disabled);
			}),
		};
	}
}