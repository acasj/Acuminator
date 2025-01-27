﻿using System;

using PX.Data;

namespace Acuminator.Tests.Tests.Utilities.SemanticModels.Dac.Sources
{
	[PXCacheName("Sales Order")]
	public class SOOrder : BaseDac, IBqlTable
	{
		public abstract class orderType : IBqlField { }

		[PXDBString(IsKey = true, InputMask = "")]
		[PXDefault]
		[PXUIField(DisplayName = "Order Type")]
		public string OrderType { get; set; }


		public abstract class orderNbr : IBqlField { }

		[PXDBString(IsKey = true, InputMask = "")]
		[PXDefault]
		[PXUIField(DisplayName = "Order Nbr.")]
		public string OrderNbr { get; set; }
	}

	[PXHidden]
	public class BaseDac : PX.Objects.AP.APInvoice
	{
		#region CreatedByID
		public new abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID()]
		public new virtual Guid? CreatedByID { get; set; }
		#endregion

		#region CreatedByScreenID
		public new abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID()]
		public new virtual string CreatedByScreenID { get; set; }
		#endregion

		#region CreatedDateTime
		public new abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime()]
		public new virtual DateTime? CreatedDateTime { get; set; }
		#endregion
	}
}
