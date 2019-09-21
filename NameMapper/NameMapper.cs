﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace NameMapper
{
	/// <summary>
	/// This program is used to identify obfuscated method/field/class names considering that you have similiar binary (may be updated/changed, comparsions are made based on similiarity).
	/// </summary>
	public class NameMapper
	{
		internal ModuleDefMD CleanModule { get; } // Module to inherit names from
		internal ModuleDefMD ObfuscatedModule { get; }

		private TextWriter _debugOutput;

		private NamableProcessor _namableProcessor;

		internal ConcurrentDictionary<string, string> NamePairs = new ConcurrentDictionary<string, string>();

		public bool DeobfuscateNames { get; }

		private int _overallErroredMethods;

		private int _inWork;

		public bool Processed { get; private set; }

		public NameMapper(ModuleDefMD cleanModule, ModuleDefMD obfuscatedModule, TextWriter debugOutput = null, bool deobfuscateNames = true)
		{
			CleanModule = cleanModule;
			ObfuscatedModule = obfuscatedModule;
			DeobfuscateNames = deobfuscateNames;
			_debugOutput = debugOutput; 
			_namableProcessor = new NamableProcessor(this);
		}

		public Dictionary<string, string> GetNamePairs() => new Dictionary<string, string>(NamePairs);

		public bool BeginProcessing()
		{
			if (Processed)
				return Message("E | Process() called but Processed equals true! This class is a one-time use.");

			Processed = true;

			// -- BEGIN IDENTIFYING PROCESS

			//     -- Identifying using entry point as start.

			var cleanEntry = FindEntryPoint(CleanModule);
			var obfuscatedEntry = FindEntryPoint(ObfuscatedModule);

			if (cleanEntry is null)
				return Message("E | Can't find entry point of clean module!");

			if (obfuscatedEntry is null)
				return Message("E | Can't find entry point of obfuscated module!");

			Message("I | Calling recurse! Level: 0");

			EnqueueRecurseThread(cleanEntry, obfuscatedEntry);

			WaitMakeSure();

			//     -- 

			//     -- Identifying using already known type pairs.

			int prevCount = -1;

			while(true)
			{
				long recurseNum = 0;

				foreach (var kvp in _namableProcessor.AlreadyProcessedTypes)
				{
					if (kvp.Value) // already fully processed
						continue;

					var cleanMethods = kvp.Key.Item1.ScopeType.ResolveTypeDef()?.Methods;
					var obfuscatedMethods = kvp.Key.Item2.ScopeType.ResolveTypeDef()?.Methods;

					if (cleanMethods is null || obfuscatedMethods is null)
						continue;

					List<MethodDef> cleanUniqueMethods = cleanMethods.ExcludeMethodsDuplicatesByOpcodes(); // exclude duplicates
					List<MethodDef> obfuscatedUniqueMethods = obfuscatedMethods.ExcludeMethodsDuplicatesByOpcodes();

					foreach (var cleanMethod in cleanUniqueMethods)
					{
						var obfuscatedMethod = obfuscatedUniqueMethods.FirstOrDefault(x => AreOpcodesEqual(cleanMethod?.Body?.Instructions, x.Body?.Instructions));

						if(obfuscatedMethod != null)
						{
							var result = _namableProcessor.ProcessMethod(cleanMethod, obfuscatedMethod);

							if (result == ProcessResult.Ok)
							{
								EnqueueRecurseThread(cleanMethod, obfuscatedMethod, recurseNum);
								recurseNum += 1000000000;
							}
						}
					}

					_namableProcessor.AlreadyProcessedTypes[kvp.Key] = true;
				}

				WaitMakeSure();

				int count = _namableProcessor.AlreadyProcessedTypes.Count;

				if (count == prevCount)
					break;

				Message($"I | {count - prevCount} new types! Processing...");

				prevCount = count;
			}

			//     --

			// also wait
			WaitMakeSure();

			if (_overallErroredMethods > 0)
				Message($"W | Not all methods are processed! {_overallErroredMethods} left behind.");

			Message($"I | Overall known classes: {_namableProcessor.AlreadyProcessedTypes.Count}; Fully processed classes: {_namableProcessor.AlreadyProcessedTypes.Count(x => x.Value)}");

			// -- END

			return true;
		}

		public string FindName(string cleanName)
		{
			string obfuscatedName = null;

			if(Processed)
				NamePairs.TryGetValue(cleanName, out obfuscatedName);

			return obfuscatedName;
		}

		private void WaitMakeSure()
		{
			int occ = 0;

			long prevState = 0;

			while (true)
			{
				if (occ < 3 && _inWork == 0)
					occ++;
				else if (occ >= 3)
					break;
				else
					occ = 0;

				Thread.Sleep(100);

				if (Math.Abs(prevState - _inWork) > 25)
					Message("I | Waiting far all threads to finish! In work: " + _inWork);

				prevState = _inWork;
			}
		}

		/// <summary>
		/// Try to find a valid entry point for assembly, returns null if not found.
		/// </summary>
		/// <param name="module">Module to find entry point in.</param>
		/// <returns>Real Entrypoint, null if not found.</returns>
		private MethodDef FindEntryPoint(ModuleDef module)
		{
			if (module?.EntryPoint?.Body?.Instructions?.Count == 2 && module.EntryPoint.Body.Instructions[0]?.OpCode == OpCodes.Call)
				return ((IMethodDefOrRef)module.EntryPoint.Body.Instructions[0]?.Operand).ResolveMethodDef();

			return null;
		}

		private void EnqueueRecurseThread(IMethod cleanMethod, IMethod obfuscatedMethod, long recurseLevel = 0) => EnqueueRecurseThread(cleanMethod.ResolveMethodDef(), obfuscatedMethod.ResolveMethodDef(), recurseLevel);

		private void EnqueueRecurseThread(MethodDef cleanMethod, MethodDef obfuscatedMethod, long recurseLevel = 0)
		{
			Interlocked.Increment(ref _inWork);

			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					RecurseResult recurseResult = new RecurseResult(RecurseResultEnum.None);

					try
					{
						recurseResult = RecurseFromMethod(cleanMethod, obfuscatedMethod, recurseLevel++);
					}
					catch (Exception e)
					{
						Message($"E | An error occurred while trying to recurse level-{recurseLevel} method. Details:\n{e}");
					}

					lock (_msgLock)
					{
						if (recurseResult.Result != RecurseResultEnum.NullArguments &&
						    recurseResult.Result != RecurseResultEnum.InProcess &&
						    recurseResult.Result != RecurseResultEnum.Ok)
						{
							Message($"I | [R-{recurseLevel}] Done! NS: {cleanMethod.DeclaringType.Namespace}; Tuple({cleanMethod.DeclaringType.Name}::{cleanMethod.Name}(), {obfuscatedMethod.DeclaringType.Name}::{obfuscatedMethod.Name}()); Result: ", false);

							var prevColor = Console.ForegroundColor;
							Console.ForegroundColor = ConsoleColor.Red;

							Message($"{recurseResult.Result}", recurseResult.Difference == 0);

							Console.ForegroundColor = prevColor;

							if (recurseResult.Difference != 0)
								Message("; Difference: " + recurseResult.Difference);

							if (recurseResult.Result != RecurseResultEnum.Ok)
								_overallErroredMethods++;
						}
					}
				}
				finally
				{
					Interlocked.Decrement(ref _inWork);
				}
			});
		}

		/// <summary>
		/// Start search recurse. Will use EnqueueRecurseThread.
		/// </summary>
		/// <param name="cleanMethod">Method in clean assembly to start recurse from.</param>
		/// <param name="obfuscatedMethod">Method in obfuscated assembly to start recurse from.</param>
		/// <param name="recurseLevel">Level of recurse (always start with 0).</param>
		/// <returns>Result of recurse operation.</returns>
		private RecurseResult RecurseFromMethod(MethodDef cleanMethod, MethodDef obfuscatedMethod, long recurseLevel)
		{
			if (cleanMethod is null || obfuscatedMethod is null)
				return new RecurseResult(RecurseResultEnum.NullArguments);

			if (Monitor.TryEnter(obfuscatedMethod)) // clean is used in OperandProcessors.ProcessMethod, hardcoded but that's important
			{
				try
				{
					IList<Instruction> cleanInstr = cleanMethod.Body?.Instructions;
					IList<Instruction> obfuscatedInstr = obfuscatedMethod.Body?.Instructions;

					_namableProcessor.ProcessMethod(cleanMethod, obfuscatedMethod);

					if (cleanMethod.HasBody != obfuscatedMethod.HasBody)
						return new RecurseResult(RecurseResultEnum.DifferentMethods);

					if (!cleanMethod.HasBody)
						return new RecurseResult(RecurseResultEnum.Ok); // all possible things are done at this moment

					// ReSharper disable PossibleNullReferenceException
					if (cleanInstr.Count != obfuscatedInstr.Count)
						return new RecurseResult(RecurseResultEnum.DifferentInstructionsCount, Math.Abs(cleanInstr.Count - obfuscatedInstr.Count));

					if (!AreOpcodesEqual(cleanInstr, obfuscatedInstr))
						return new RecurseResult(RecurseResultEnum.DifferentInstructions);

					for (int i = 0; i < cleanInstr.Count; i++)
					{
						object cleanOperand = cleanInstr[i].Operand;
						object obfuscatedOperand = obfuscatedInstr[i].Operand;

						if (cleanOperand is null || obfuscatedOperand is null)
							continue;

						if (cleanOperand.GetType() != obfuscatedOperand.GetType())
							continue;

						if (cleanOperand is IMethod)
						{
							var result = _namableProcessor.ProcessMethod(cleanOperand as IMethod, obfuscatedOperand as IMethod);

							if (result == ProcessResult.Ok)
								EnqueueRecurseThread(cleanOperand as IMethod, obfuscatedOperand as IMethod, recurseLevel + 1);
						}
						else if (cleanOperand is ITypeDefOrRef)
							_namableProcessor.ProcessType(cleanOperand as ITypeDefOrRef, obfuscatedOperand as ITypeDefOrRef);
						else if (cleanOperand is FieldDef)
							_namableProcessor.ProcessField(cleanOperand as FieldDef, obfuscatedOperand as FieldDef);

						/*(cleanOperand is Instruction || cleanOperand is Local || cleanOperand is Parameter ||
								 cleanOperand is Instruction[] || cleanOperand is string || cleanOperand is sbyte || cleanOperand is int ||
								 cleanOperand is float || cleanOperand is double || cleanOperand is long)*/
					}
				}
				finally
				{
					Monitor.Exit(obfuscatedMethod);
				}
			}
			else return new RecurseResult(RecurseResultEnum.InProcess);

			return new RecurseResult(RecurseResultEnum.Ok);
		}

		/// <summary>
		/// Check instruction equality using opcodes only, no operands used.
		/// </summary>
		/// <returns>Are opcodes equal or not</returns>
		private bool AreOpcodesEqual(IList<Instruction> cleanInstructions, IList<Instruction> obfuscatedInstructions)
		{
			if (cleanInstructions is null || obfuscatedInstructions is null)
				return false;

			if (cleanInstructions.Count != obfuscatedInstructions.Count)
				return false;

			for (int i = 0; i < cleanInstructions.Count; i++)
			{
				var cleanOpcode = cleanInstructions[i].OpCode;
				var obfuscatedOpcode = obfuscatedInstructions[i].OpCode;

				var cleanOperand = cleanInstructions[i].Operand;
				var obfuscatedOperand = obfuscatedInstructions[i].Operand;

				if (cleanOpcode != obfuscatedOpcode)
					return false;

				/*if (cleanOperand is null || obfuscatedOperand is null || cleanOperand.GetType() != obfuscatedOperand.GetType())
					continue; // ???????

				if(cleanOperand is sbyte || cleanOperand is int || cleanOperand is float || cleanOperand is double || cleanOperand is long)
					if (!cleanOperand.Equals(obfuscatedOperand))
						return false;*/ // useless anyways (?)
			}

			return true;
		}

		private object _msgLock = new object();

		internal bool Message(string msg = "", bool newline = true)
		{
			lock(_msgLock)
					_debugOutput?.Write(msg + (newline ? Environment.NewLine : string.Empty));

			return false;
		}

		private class RecurseResult
		{
			public RecurseResultEnum Result { get; }
			public int Difference { get; }

			public RecurseResult(RecurseResultEnum result, int diff = 0)
			{
				Result = result;
				Difference = diff;
			}
		}

		private enum RecurseResultEnum
		{
			None,
			Ok,
			NullArguments,
			InProcess,
			DifferentInstructionsCount,
			DifferentInstructions,
			DifferentMethods,
		}
	}
}
