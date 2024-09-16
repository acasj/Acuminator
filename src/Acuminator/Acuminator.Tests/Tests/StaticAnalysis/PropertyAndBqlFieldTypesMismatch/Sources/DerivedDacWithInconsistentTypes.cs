﻿using System;

using PX.Data;
using PX.Data.BQL;

namespace PX.Analyzers.Test.Sources
{
	[PXHidden]
	public class DerivedDac : Dac
	{
		#region LineNbr
		[PXUIField]
		public byte[] LineNbr { get; set; }

		public abstract class lineNbr : Data.BQL.BqlInt.Field<lineNbr> { }
		#endregion

		#region OrderType
		public abstract class orderType : BqlLong.Field<orderType> { }

		[PXUIField]
		public string OrderType { get; set; }
		#endregion

		// Acuminator disable once PX1067 MissingBqlFieldRedeclarationInDerivedDac [Justification]
		[PXUIField]
		public string NoteID { get; set; }

		public new abstract class Tstamp : BqlByte.Field<Tstamp> { }
	}

	[PXHidden]
	public class Dac : IBqlTable
	{
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		#endregion

		#region tstamp
		public abstract class Tstamp : IBqlField { }

		[PXUIField]
		public virtual byte[] tstamp { get; set; }
		#endregion
	}
}