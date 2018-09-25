﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;

namespace PX.Objects
{
	public class SOInvoiceEntry : PXGraph<SOInvoiceEntry, SOInvoice>
	{
		protected virtual void _(Events.FieldDefaulting<SOInvoice, SOInvoice.refNbr> e)
		{
			((PXCache<SOInvoice>) e.Cache).Update(e.Row);
		}

		protected virtual void _(Events.FieldVerifying<SOInvoice.refNbr> e)
		{
			((PXCache<SOInvoice>) e.Cache).Update(e.Row);
		}

		protected virtual void _(Events.RowSelecting<SOInvoice> e)
		{
			((PXCache<SOInvoice>) e.Cache).Insert(e.Row);
		}

		protected virtual void _(Events.RowSelected<SOInvoice> e)
		{
			((PXCache<SOInvoice>) e.Cache).Update(e.Row);
		}

		protected virtual void _(Events.RowInserting<SOInvoice> e)
		{
			((PXCache<SOInvoice>) e.Cache).Delete(e.Row);
		}

		protected virtual void _(Events.RowUpdating<SOInvoice> e)
		{
			((PXCache<SOInvoice>) e.Cache).Delete(e.Row);
		}

		protected virtual void _(Events.RowDeleting<SOInvoice> e)
		{
			((PXCache<SOInvoice>) e.Cache).Insert(e.Row);
		}
	}

	public class SOInvoice : IBqlTable
	{
		#region RefNbr
		[PXDBString(8, IsKey = true, InputMask = "")]
		public string RefNbr { get; set; }
		public abstract class refNbr : IBqlField { }
		#endregion	
	}
}