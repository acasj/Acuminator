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
		protected virtual void _(Events.RowSelected<SOInvoice> e)
		{
			if (!(e.Row is SOInvoice invoice))
				return;

			invoice.RefNbr = "<NEW>";
		}

		protected virtual void _(Events.FieldDefaulting<SOInvoice, SOInvoice.refNbr> e)
		{
			if (!(e.Row is SOInvoice invoice))
				return;

			invoice.RefNbr = "<NEW>";
		}

		protected virtual void _(Events.FieldVerifying<SOInvoice, SOInvoice.refNbr> e)
		{
			if (!(e.Row is SOInvoice invoice))
				return;

			invoice.RefNbr = "<NEW>";
		}

		protected virtual void _(Events.RowSelected<SOLine> e)
		{
			if (!(e.Row is { } row))
				return;

			row.RefNbr = "<NEW>";
		}

		protected virtual void _(Events.FieldDefaulting<SOLine, SOLine.refNbr> e)
		{
			if (!(e.Row is { } row))
				return;

			row.RefNbr = "<NEW>";
		}

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.refNbr> e)
		{
			if (!(e.Row is { } row))
				return;

			row.RefNbr = "<NEW>";
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

	public class SOLine : IBqlTable
	{
		#region RefNbr
		[PXDBString(8, IsKey = true, InputMask = "")]
		public string RefNbr { get; set; }
		public abstract class refNbr : IBqlField { }
		#endregion
	}
}