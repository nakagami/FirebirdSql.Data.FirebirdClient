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
 *
 *  Contributors:
 *    Jiri Cincura (jiri@cincura.net)
 */

using System;
using System.Text;

namespace FirebirdSql.Data.Common
{
	internal sealed class DbField
	{
		#region Fields

		private short _dataType;
		private short _numericScale;
		private short _subType;
		private short _length;
		private short _nullFlag;
		private string _name;
		private string _relation;
		private string _owner;
		private string _alias;
		private int _charCount;
		private DbValue _dbValue;
		private Charset _charset;
		private ArrayBase _arrayHandle;

		#endregion

		#region Properties

		public DbDataType DbDataType
		{
			get { return GetDbDataType(); }
		}

		public int SqlType
		{
			get { return _dataType & ~1; }
		}

		public short DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}

		public short NumericScale
		{
			get { return _numericScale; }
			set { _numericScale = value; }
		}

		public short SubType
		{
			get { return _subType; }
			set
			{
				_subType = value;
				if (IsCharacter())
				{
					// Bits 0-7 of sqlsubtype is charset_id (127 is a special value -
					// current attachment charset).
					// Bits 8-17 hold collation_id for this value.
					byte[] cs = BitConverter.GetBytes(value);

					_charset = Charset.GetCharset(cs[0]);

					if (_charset == null)
					{
						_charset = Charset.DefaultCharset;
					}
				}
			}
		}

		public short Length
		{
			get { return _length; }
			set
			{
				_length = value;
				if (IsCharacter())
				{
					_charCount = _length / _charset.BytesPerCharacter;
				}
			}
		}

		public short NullFlag
		{
			get { return _nullFlag; }
			set { _nullFlag = value; }
		}

		public string Name
		{
			get { return _name; }
			set { _name = value.Trim(); }
		}

		public string Relation
		{
			get { return _relation; }
			set { _relation = value.Trim(); }
		}

		public string Owner
		{
			get { return _owner; }
			set { _owner = value.Trim(); }
		}

		public string Alias
		{
			get { return _alias; }
			set { _alias = value.Trim(); }
		}

		public Charset Charset
		{
			get { return _charset; }
		}

		public int CharCount
		{
			get { return _charCount; }
		}

		public ArrayBase ArrayHandle
		{
			get
			{
				if (IsArray())
				{
					return _arrayHandle;
				}
				else
				{
					throw new IscException("Field is not an array type");
				}
			}

			set
			{
				if (IsArray())
				{
					_arrayHandle = value;
				}
				else
				{
					throw new IscException("Field is not an array type");
				}
			}
		}

		public DbValue DbValue
		{
			get { return _dbValue; }
		}

		public object Value
		{
			get { return _dbValue.Value; }
			set { _dbValue.Value = value; }
		}

		#endregion

		#region Constructors

		public DbField()
		{
			_charCount = -1;
			_name = string.Empty;
			_relation = string.Empty;
			_owner = string.Empty;
			_alias = string.Empty;
			_dbValue = new DbValue(this, DBNull.Value);
		}

		#endregion

		#region Methods

		public bool IsNumeric()
		{
			if (_dataType == 0)
			{
				return false;
			}

			switch (DbDataType)
			{
				case DbDataType.SmallInt:
				case DbDataType.Integer:
				case DbDataType.BigInt:
				case DbDataType.Numeric:
				case DbDataType.Decimal:
				case DbDataType.Float:
				case DbDataType.Double:
					return true;

				default:
					return false;
			}
		}

		public bool IsDecimal()
		{
			if (_dataType == 0)
			{
				return false;
			}

			switch (DbDataType)
			{
				case DbDataType.Numeric:
				case DbDataType.Decimal:
					return true;

				default:
					return false;
			}
		}

		public bool IsLong()
		{
			if (_dataType == 0)
			{
				return false;
			}

			switch (DbDataType)
			{
				case DbDataType.Binary:
				case DbDataType.Text:
					return true;

				default:
					return false;
			}
		}

		public bool IsCharacter()
		{
			if (_dataType == 0)
			{
				return false;
			}

			switch (DbDataType)
			{
				case DbDataType.Char:
				case DbDataType.VarChar:
				case DbDataType.Text:
					return true;

				default:
					return false;
			}
		}

		public bool IsArray()
		{
			if (_dataType == 0)
			{
				return false;
			}

			switch (DbDataType)
			{
				case DbDataType.Array:
					return true;

				default:
					return false;
			}
		}

		public bool IsAliased()
		{
			return (Name != Alias) ? true : false;
		}

		//public bool IsExpression()
		//{
		//    return this.Name.Length == 0 ? true : false;
		//}

		public int GetSize()
		{
			if (IsLong())
			{
				return System.Int32.MaxValue;
			}
			else
			{
				if (IsCharacter())
				{
					return CharCount;
				}
				else
				{
					return Length;
				}
			}
		}

		public bool AllowDBNull()
		{
			return ((DataType & 1) == 1);
		}

		public void SetValue(byte[] buffer)
		{
			if (buffer == null || NullFlag == -1)
			{
				Value = System.DBNull.Value;
			}
			else
			{
				switch (SqlType)
				{
					case IscCodes.SQL_TEXT:
					case IscCodes.SQL_VARYING:
						if (DbDataType == DbDataType.Guid)
						{
							Value = new Guid(buffer);
						}
						else
						{
							if (Charset.IsOctetsCharset)
							{
								Value = buffer;
							}
							else
							{
								string s = Charset.GetString(buffer, 0, buffer.Length);

								if ((Length % Charset.BytesPerCharacter) == 0 &&
									s.Length > CharCount)
								{
									s = s.Substring(0, CharCount);
								}

								Value = s;
							}
						}
						break;

					case IscCodes.SQL_SHORT:
						if (_numericScale < 0)
						{
							Value = TypeDecoder.DecodeDecimal(
								BitConverter.ToInt16(buffer, 0),
								_numericScale,
								_dataType);
						}
						else
						{
							Value = BitConverter.ToInt16(buffer, 0);
						}
						break;

					case IscCodes.SQL_LONG:
						if (NumericScale < 0)
						{
							Value = TypeDecoder.DecodeDecimal(
								BitConverter.ToInt32(buffer, 0),
								_numericScale,
								_dataType);
						}
						else
						{
							Value = BitConverter.ToInt32(buffer, 0);
						}
						break;

					case IscCodes.SQL_FLOAT:
						Value = BitConverter.ToSingle(buffer, 0);
						break;

					case IscCodes.SQL_DOUBLE:
					case IscCodes.SQL_D_FLOAT:
						Value = BitConverter.ToDouble(buffer, 0);
						break;

					case IscCodes.SQL_QUAD:
					case IscCodes.SQL_INT64:
					case IscCodes.SQL_BLOB:
					case IscCodes.SQL_ARRAY:
						if (NumericScale < 0)
						{
							Value = TypeDecoder.DecodeDecimal(
								BitConverter.ToInt64(buffer, 0),
								_numericScale,
								_dataType);
						}
						else
						{
							Value = BitConverter.ToInt64(buffer, 0);
						}
						break;

					case IscCodes.SQL_TIMESTAMP:
						DateTime date = TypeDecoder.DecodeDate(BitConverter.ToInt32(buffer, 0));
						TimeSpan time = TypeDecoder.DecodeTime(BitConverter.ToInt32(buffer, 4));

						Value = new System.DateTime(
							date.Year, date.Month, date.Day,
							time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
						break;

					case IscCodes.SQL_TYPE_TIME:
						Value = TypeDecoder.DecodeTime(BitConverter.ToInt32(buffer, 0));
						break;

					case IscCodes.SQL_TYPE_DATE:
						Value = TypeDecoder.DecodeDate(BitConverter.ToInt32(buffer, 0));
						break;

					default:
						throw new NotSupportedException("Unknown data type");
				}
			}
		}

		public void FixNull()
		{
			if (NullFlag == -1 && _dbValue.IsDBNull())
			{
				switch (DbDataType)
				{
					case DbDataType.Char:
					case DbDataType.VarChar:
						Value = string.Empty;
						break;

					case DbDataType.Guid:
						Value = Guid.Empty;
						break;

					case DbDataType.SmallInt:
						Value = (short)0;
						break;

					case DbDataType.Integer:
						Value = (int)0;
						break;

					case DbDataType.BigInt:
					case DbDataType.Binary:
					case DbDataType.Array:
					case DbDataType.Text:
						Value = (long)0;
						break;

					case DbDataType.Numeric:
					case DbDataType.Decimal:
						Value = (decimal)0;
						break;

					case DbDataType.Float:
						Value = (float)0;
						break;

					case DbDataType.Double:
						Value = (double)0;
						break;

					case DbDataType.Date:
					case DbDataType.TimeStamp:
						Value = new DateTime(0 * 10000L + 621355968000000000);
						break;

					case DbDataType.Time:
						Value = TimeSpan.Zero;
						break;

					default:
						throw new IscException("Unknown sql data type: " + DataType);
				}
			}
		}

		public Type GetSystemType()
		{
			return Type.GetType(TypeHelper.GetSystemDataTypeName(DbDataType), true);
		}

		public bool HasDataType()
		{
			return _dataType != 0;
		}

		#endregion

		#region Private Methods

		private DbDataType GetDbDataType()
		{
			// Special case for Guid handling
			if (SqlType == IscCodes.SQL_TEXT && Length == 16 &&
				(Charset != null && Charset.Name.Equals("OCTETS", StringComparison.InvariantCultureIgnoreCase)))
			{
				return DbDataType.Guid;
			}

			switch (SqlType)
			{
				case IscCodes.SQL_TEXT:
					return DbDataType.Char;

				case IscCodes.SQL_VARYING:
					return DbDataType.VarChar;

				case IscCodes.SQL_SHORT:
					if (SubType == 2)
					{
						return DbDataType.Decimal;
					}
					else if (SubType == 1)
					{
						return DbDataType.Numeric;
					}
					else if (NumericScale < 0)
					{
						return DbDataType.Decimal;
					}
					else
					{
						return DbDataType.SmallInt;
					}

				case IscCodes.SQL_LONG:
					if (SubType == 2)
					{
						return DbDataType.Decimal;
					}
					else if (SubType == 1)
					{
						return DbDataType.Numeric;
					}
					else if (NumericScale < 0)
					{
						return DbDataType.Decimal;
					}
					else
					{
						return DbDataType.Integer;
					}

				case IscCodes.SQL_QUAD:
				case IscCodes.SQL_INT64:
					if (SubType == 2)
					{
						return DbDataType.Decimal;
					}
					else if (SubType == 1)
					{
						return DbDataType.Numeric;
					}
					else if (NumericScale < 0)
					{
						return DbDataType.Decimal;
					}
					else
					{
						return DbDataType.BigInt;
					}

				case IscCodes.SQL_FLOAT:
					return DbDataType.Float;

				case IscCodes.SQL_DOUBLE:
				case IscCodes.SQL_D_FLOAT:
					if (SubType == 2)
					{
						return DbDataType.Decimal;
					}
					else if (SubType == 1)
					{
						return DbDataType.Numeric;
					}
					else if (NumericScale < 0)
					{
						return DbDataType.Decimal;
					}
					else
					{
						return DbDataType.Double;
					}

				case IscCodes.SQL_BLOB:
					if (_subType == 1)
					{
						return DbDataType.Text;
					}
					else
					{
						return DbDataType.Binary;
					}

				case IscCodes.SQL_TIMESTAMP:
					return DbDataType.TimeStamp;

				case IscCodes.SQL_TYPE_TIME:
					return DbDataType.Time;

				case IscCodes.SQL_TYPE_DATE:
					return DbDataType.Date;

				case IscCodes.SQL_ARRAY:
					return DbDataType.Array;

				case IscCodes.SQL_NULL:
					return DbDataType.Null;

				default:
					throw new SystemException("Invalid data type");
			}
		}

		#endregion
	}
}
