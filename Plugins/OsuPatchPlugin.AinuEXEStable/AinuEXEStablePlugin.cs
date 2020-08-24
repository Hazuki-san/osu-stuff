using dnlib.DotNet;
using dnlib.DotNet.Emit;
using osu_patch;
using osu_patch.Plugins;
using System;
using System.Collections.Generic;

namespace OsuPatchPlugin.AinuEXEStable
{
	public class AinuEXEStablePlugin : IOsuPatchPlugin
	{
		public IEnumerable<Patch> GetPatches() => new Patch[]
		{
			new Patch("osu!.exe to ainu.exe (CommonUpdater)", (patch, exp) =>
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
					});
					CommonUpd.NopAt(CommonUpdLoc + 2, 1);
					CommonUpd.ReplaceAt(CommonUpdLoc + 3, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (exitOrRestart)", (patch, exp) =>
			{
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
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (ForceUpdate)", (patch, exp) =>
			{
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
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (Initialize)", (patch, exp) =>
			{
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
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (Main)", (patch, exp) =>
			{
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
					OpCodes.Call,
					null,
					null,
					OpCodes.Call,
					OpCodes.Brfalse_S,
					null,
					null,
					OpCodes.Newobj,
				});
					Main.NopAt(MainLoc1 + 6, 1);
					Main.ReplaceAt(MainLoc1 + 7, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Main.NopAt(MainLoc2 + 1, 1);
					Main.ReplaceAt(MainLoc2 + 2, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
					Main.NopAt(MainLoc2 + 5, 1);
					Main.ReplaceAt(MainLoc2 + 6, Instruction.Create(OpCodes.Ldstr, "Executable filename is not correct! Please rename to ainu.exe"));
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (Repair)", (patch, exp) =>
			{
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
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (RestartImmediately)", (patch, exp) =>
			{
				// Restart Immediately
				var RestartImmediately = exp["osu.OsuMain"]["RestartImmediately"].Editor;
				var RestartImmediatelyLoc = RestartImmediately.Locate(new[]{null,null,null,null,OpCodes.Call,OpCodes.Pop,OpCodes.Call,OpCodes.Ret});
					RestartImmediately.NopAt(RestartImmediatelyLoc, 1);
					RestartImmediately.ReplaceAt(RestartImmediatelyLoc + 1, Instruction.Create(OpCodes.Ldstr, "ainu.exe"));
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (startup)", (patch, exp) =>
			{
				// Start-up
				var StartUpCheck = exp["osu.OsuMain"]["startup"].Editor;
				var StartUpLoc = StartUpCheck.Locate(new[]{
					/* --- First osu!.exe */
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
					/* --- 2nd osu!.exe */
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
					/* --- 3rd osu!.exe */
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
					/* --- 4th osu!.exe */
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
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (update)", (patch, exp) =>
			{
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
			return patch.Result(PatchStatus.Success);
			}),
			new Patch("osu!.exe to ainu.exe (AuthCheck)", (patch, exp) =>
			{
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
			new Patch("osu!.exe to ainu.exe (-ainu)", (patch, exp) =>
			{
				// Change subversion and concat them
				var concatSSSS = exp.Module.CreateMethodRef(true, typeof(String), "Concat", typeof(string),  typeof(string), typeof(string), typeof(string), typeof(string));
				var buildname = exp["osu.General"]["get_BUILD_NAME"].Editor;
				var buildnameloc = buildname.Locate(new[]{OpCodes.Call,OpCodes.Ret});
				var instructions = new[] {
					Instruction.Create(OpCodes.Ldstr, "-ainu"),
					Instruction.Create(OpCodes.Call, concatSSSS),
					Instruction.Create(OpCodes.Ret),
				};
					buildname.Remove(buildnameloc);
					buildname.InsertAt(buildnameloc, instructions);
			return patch.Result(PatchStatus.Success);
			}),
		};

		public void Load(ModuleDef originalObfOsuModule) { }
	}
}
