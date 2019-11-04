﻿using System;
using System.Collections.Generic;
using System.Reflection;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using osu_patch.Explorers;
using osu_patch.Naming;

using OsuPatchCommon;

namespace osu_patch.Conversion
{
	public class BodyConverter
	{
		private IList<LocalVariableInfo> _locals;
		private IList<ExceptionHandlingClause> _exceptions;
		private byte[] _body;
		private bool _initLocals;

		private int _position;
		private Module _patchModule;
		private ModuleExplorer _osuModule;

		#region Read methods
		private short ReadInt16()
		{
			var val = BitConverter.ToInt16(_body, _position);
			_position += 2;
			return val;
		}

		private ushort ReadUInt16()
		{
			var val = BitConverter.ToUInt16(_body, _position);
			_position += 2;
			return val;
		}

		private int ReadInt32()
		{
			var val = BitConverter.ToInt32(_body, _position);
			_position += 4;
			return val;
		}

		private uint ReadUInt32()
		{
			var val = BitConverter.ToUInt32(_body, _position);
			_position += 4;
			return val;
		}

		private long ReadInt64()
		{
			var val = BitConverter.ToInt64(_body, _position);
			_position += 8;
			return val;
		}

		private ulong ReadUInt64()
		{
			var val = BitConverter.ToUInt64(_body, _position);
			_position += 8;
			return val;
		}

		private float ReadSingle() // float
		{
			var val = BitConverter.ToSingle(_body, _position);
			_position += 4;
			return val;
		}

		private double ReadDouble()
		{
			var val = BitConverter.ToDouble(_body, _position);
			_position += 8;
			return val;
		}

		private sbyte ReadSByte() =>
			(sbyte)_body[_position++];

		private byte ReadByte() =>
			_body[_position++];
		#endregion

		public BodyConverter(Delegate del, ModuleExplorer osuModule)
		{
			var methBody = del.Method.GetMethodBody() ?? throw new Exception("Unable to get method body!");

			_locals = methBody.LocalVariables;
			_exceptions = methBody.ExceptionHandlingClauses;
			_body = methBody.GetILAsByteArray();
			_initLocals = methBody.InitLocals;

			_patchModule = del.Method.Module;
			_osuModule = osuModule;
		}

		public CilBody ToCilBody()
		{
			var newLocals = new List<Local>();

			foreach (var local in _locals)
			{
				var type = HookTypeToTypeDefOrRef(local.LocalType);

				if(type.Name != "Object")
					Console.WriteLine();
			}

			return new CilBody();
		}

		/// <summary>
		/// Convert hook types (generated by HookAssemblyGenerator) to osu!.exe TypeSig
		/// </summary>
		private ITypeDefOrRef HookTypeToTypeDefOrRef(Type hookType)
		{
			/*
			if (originalDef.Namespace == "System" || originalDef.Namespace.StartsWith("System."))
				return _hookModule.CorLibTypes.GetTypeRef(originalDef.Namespace, originalDef.Name); // System types

			if (originalDef.DefinitionAssembly.FullName != _originalModule.Assembly.FullName) // External dependency (OpenTK, etc)
				return _hookModule.Import(originalDef).ScopeType;

			if (_processedTypes.TryGetValue(originalDef.ScopeType.FullName, out var defInfo)) // Internal hook type (already added)
				return defInfo.HookDef;

			return _hookModule.CorLibTypes.Object.TypeDefOrRef;
			*/

			if (hookType.IsSystemType() || !hookType.Assembly.FullName.StartsWith("OsuHooks-")) // System types and external dependencies (OpenTK, etc) // TODO: Checking by assembly name is pretty dumb
				return _osuModule.GetCorLibTypeDefOrRef(hookType);

			// if(_osuModule.NameProvider.GetName(hookType.))

			return _osuModule.CorLibTypes.Object.TypeDefOrRef; // BUG! TODO
		}
	}

	public static class RuntimeOpCodeList
	{
		private static Dictionary<ushort, OpCode> _opCodes = new Dictionary<ushort, OpCode>();

		static RuntimeOpCodeList()
		{
			var fields = typeof(OpCodes).GetFields();

			for (int i = 0; i < fields.Length; i++)
			{
				var info = fields[i];

				if (info.FieldType == typeof(OpCode))
				{
					var opCode = (OpCode)info.GetValue(null);
					var opCodeValue = opCode.Value;

					_opCodes.Add((ushort)opCodeValue, opCode);
				}
			}
		}

		public static OpCode Get(ushort id) =>
			_opCodes[id];
	}
}