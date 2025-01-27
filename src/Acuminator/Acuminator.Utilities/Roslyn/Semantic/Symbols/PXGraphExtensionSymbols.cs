﻿#nullable enable

using System;
using System.Linq;

using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Constants;
using Acuminator.Utilities.Roslyn.Semantic.PXGraph;

using Microsoft.CodeAnalysis;

namespace Acuminator.Utilities.Roslyn.Semantic.Symbols
{
    public class PXGraphExtensionSymbols : SymbolsSetForTypeBase
	{
		public IMethodSymbol? Initialize { get; }

		public IMethodSymbol? Configure { get; }

		internal PXGraphExtensionSymbols(PXContext pxContext) : base(pxContext.Compilation, TypeFullNames.PXGraphExtension)
        {
			Initialize = Type.GetMethods(DelegateNames.Initialize).FirstOrDefault();
			Configure  = Type.GetConfigureMethodFromBaseGraphOrGraphExtension(pxContext);
		}
    }
}