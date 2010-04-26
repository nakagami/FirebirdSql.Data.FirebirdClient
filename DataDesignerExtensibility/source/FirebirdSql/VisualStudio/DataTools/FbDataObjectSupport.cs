/*
 *  Visual Studio DDEX Provider for Firebird
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
 *  Copyright (c) 2005 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System;
using Microsoft.VisualStudio.Data;

namespace FirebirdSql.VisualStudio.DataTools
{
    internal class FbDataObjectSupport : DataObjectSupport
    {
        #region � Constructors �

		public FbDataObjectSupport()
            : base("FirebirdSql.VisualStudio.DataTools.FbDataObjectSupport", typeof(FbDataObjectSupport).Assembly)
		{
            System.Diagnostics.Trace.WriteLine("FbDataObjectSupport()");
		}

        #endregion
    }
}
