﻿/*
 *  Firebird ADO.NET Data provider for .NET and Mono
 *
 *     The contents of this file are subject to the Initial
 *     Developer's Public License Version 1.0 (the "License");
 *     you may not use this file except in compliance with the
 *     License. You may obtain a copy of the License at
 *     http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *     Software distributed under the License is distributed on
 *     an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *     express or implied.  See the License for the specific
 *     language governing rights and limitations under the License.
 *
 *  Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System;
using System.Data;
using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.ExternalEngine
{
	internal sealed class ExtTriggerContext : ITriggerContext
	{
		#region Fields

		private IDatabase _database;

		#endregion

		#region Constructors

		public ExtTriggerContext(IDatabase database)
		{
			_database = database;
		}

		#endregion

		#region Methods

		public string GetTableName()
		{
			int[] statusVector = ExtConnection.GetNewStatusVector();
			byte[] tableName = new byte[255];

			int count = SafeNativeMethods.isc_get_trigger_table_name(statusVector, tableName, 255);

			ExtConnection.ParseStatusVector(statusVector);

			return _database.Charset.GetString(tableName, 0, count);
		}

		public int GetTriggerAction()
		{
			int[] statusVector = ExtConnection.GetNewStatusVector();

			int action = SafeNativeMethods.isc_get_trigger_action(statusVector);

			ExtConnection.ParseStatusVector(statusVector);

			return action;
		}

		public object GetOldValue(string name)
		{
			return GetValue(name, true);
		}

		public object GetNewValue(string name)
		{
			return GetValue(name, false);
		}

		public void SetNewValue(string name, object value)
		{
			SetValue(name, value, false);
		}

		#endregion

		#region Private Methods

		private object GetValue(string name, bool oldValue)
		{
			int[] statusVector = ExtConnection.GetNewStatusVector();
			byte[] fieldName = new byte[32];
			object value = null;

			_database.Charset.GetBytes(name, 0, name.Length, fieldName, 0);

			// Marshal structures to pointer
			ParamDscMarshaler marshaler = ParamDscMarshaler.Instance;

			IntPtr valuePtr = marshaler.MarshalManagedToNative();

			bool result = SafeNativeMethods.isc_get_trigger_field(
				statusVector,
				(oldValue) ? 0 : 1,
				fieldName,
				valuePtr);

			value = marshaler.MarshalNativeToManaged(_database.Charset, valuePtr);
			marshaler.CleanUpNativeData(ref valuePtr);

			ExtConnection.ParseStatusVector(statusVector);

			return value;
		}

		private void SetValue(string name, object value, bool oldValue)
		{
			int[] statusVector = ExtConnection.GetNewStatusVector();
			byte[] fieldName = new byte[32];

			_database.Charset.GetBytes(name, 0, name.Length, fieldName, 0);

			// Marshal structures to pointer
			ParamDscMarshaler marshaler = ParamDscMarshaler.Instance;

			IntPtr valuePtr = marshaler.MarshalManagedToNative(_database.Charset, value);

			bool result = SafeNativeMethods.isc_set_trigger_field(
				statusVector,
				(oldValue) ? 0 : 1,
				fieldName,
				valuePtr);

			marshaler.CleanUpNativeData(ref valuePtr);

			ExtConnection.ParseStatusVector(statusVector);
		}

		#endregion
	}
}
