﻿/*
 *	Firebird ADO.NET Data provider for .NET and Mono
 *
 *	   The contents of this file are subject to the Initial
 *	   Developer's Public License Version 1.0 (the "License");
 *	   you may not use this file except in compliance with the
 *	   License. You may obtain a copy of the License at
 *	   http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *	   Software distributed under the License is distributed on
 *	   an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *	   express or implied. See the License for the specific
 *	   language governing rights and limitations under the License.
 *
 *	Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *	All Rights Reserved.
 */

using System;
using System.IO;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version10
{
	internal sealed class GdsBlob : BlobBase
	{
		#region Fields

		private GdsDatabase _database;

		#endregion

		#region Properties

		public override IDatabase Database
		{
			get { return _database; }
		}

		#endregion

		#region Constructors

		public GdsBlob(IDatabase db, ITransaction transaction) : this(db, transaction, 0)
		{
		}

		public GdsBlob(IDatabase db, ITransaction transaction, long blobId) : base(db)
		{
			if (!(db is GdsDatabase))
			{
				throw new ArgumentException("Specified argument is not of GdsDatabase type.");
			}
			if (!(transaction is GdsTransaction))
			{
				throw new ArgumentException("Specified argument is not of GdsTransaction type.");
			}

			_database = (GdsDatabase)db;
			_transaction = transaction;
			_position = 0;
			_blobHandle = 0;
			_blobId = blobId;
		}

		#endregion

		#region Protected Methods

		protected override void Create()
		{
			try
			{
				CreateOrOpen(IscCodes.op_create_blob, null);
				RblAddValue(IscCodes.RBL_create);
			}
			catch (IscException)
			{
				throw;
			}
		}

		protected override void Open()
		{
			try
			{
				CreateOrOpen(IscCodes.op_open_blob, null);
			}
			catch (IscException)
			{
				throw;
			}
		}

		protected override byte[] GetSegment()
		{
			int requested = SegmentSize;

			lock (_database.SyncObject)
			{
				try
				{
					_database.Write(IscCodes.op_get_segment);
					_database.Write(_blobHandle);
					_database.Write((requested + 2 < short.MaxValue) ? requested + 2 : short.MaxValue);
					_database.Write(0); // Data	segment
					_database.Flush();

					GenericResponse response = _database.ReadGenericResponse();

					RblRemoveValue(IscCodes.RBL_segment);
					if (response.ObjectHandle == 1)
					{
						RblAddValue(IscCodes.RBL_segment);
					}
					else if (response.ObjectHandle == 2)
					{
						RblAddValue(IscCodes.RBL_eof_pending);
					}

					byte[] buffer = response.Data;

					if (buffer.Length == 0)
					{
						// previous	segment	was	last, this has no data
						return buffer;
					}

					int len     = 0;
					int srcpos  = 0;
					int destpos = 0;

					while (srcpos < buffer.Length)
					{
						len = IscHelper.VaxInteger(buffer, srcpos, 2);
						srcpos += 2;

						Buffer.BlockCopy(buffer, srcpos, buffer, destpos, len);
						srcpos	+= len;
						destpos += len;
					}

					byte[] result = new byte[destpos];
					Buffer.BlockCopy(buffer, 0, result, 0, destpos);

					return result;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		protected override void PutSegment(byte[] buffer)
		{
			lock (_database.SyncObject)
			{
				try
				{
					_database.Write(IscCodes.op_batch_segments);
					_database.Write(_blobHandle);
					_database.WriteBlobBuffer(buffer);
					_database.Flush();

					_database.ReadResponse();
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		protected override void Seek(int position)
		{
			lock (_database.SyncObject)
			{
				try
				{
					_database.Write(IscCodes.op_seek_blob);
					_database.Write(_blobHandle);
					_database.Write(0);                 // Seek	mode
					_database.Write(position);          // Seek	offset
					_database.Flush();

					GenericResponse response = (GenericResponse)_database.ReadResponse();

					_position = response.ObjectHandle;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_network_error);
				}
			}
		}

		protected override void GetBlobInfo()
		{
			throw new NotSupportedException();
		}

		protected override void Close()
		{
			_database.ReleaseObject(IscCodes.op_close_blob, _blobHandle);
		}

		protected override void Cancel()
		{
			_database.ReleaseObject(IscCodes.op_cancel_blob, _blobHandle);
		}

		#endregion

		#region Private API Methods

		private void CreateOrOpen(int op, BlobParameterBuffer bpb)
		{
			lock (_database.SyncObject)
			{
				try
				{
					_database.Write(op);
					if (bpb != null)
					{
						_database.WriteTyped(IscCodes.isc_bpb_version1, bpb.ToArray());
					}
					_database.Write(_transaction.Handle);
					_database.Write(_blobId);
					_database.Flush();

					GenericResponse response = _database.ReadGenericResponse();

					_blobId = response.BlobId;
					_blobHandle = response.ObjectHandle;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		#endregion
	}
}
