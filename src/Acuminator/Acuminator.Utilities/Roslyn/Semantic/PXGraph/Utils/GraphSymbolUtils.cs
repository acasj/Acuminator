﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Acuminator.Utilities.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Acuminator.Utilities.Roslyn.Semantic.PXGraph
{
	public static class GraphSymbolUtils
	{
		/// <summary>
		/// Gets the graph type from graph extension type.
		/// </summary>
		/// <param name="graphExtension">The graph extension to act on.</param>
		/// <param name="pxContext">Context.</param>
		/// <returns>
		/// The graph from graph extension.
		/// </returns>
		public static ITypeSymbol GetGraphFromGraphExtension(this ITypeSymbol graphExtension, PXContext pxContext)
		{
			pxContext.ThrowOnNull(nameof(pxContext));

			if (graphExtension == null || !graphExtension.InheritsFrom(pxContext.PXGraphExtensionType))
				return null;

			var baseGraphExtensionType = graphExtension.GetBaseTypesAndThis()
													   .OfType<INamedTypeSymbol>()
													   .FirstOrDefault(type => type.IsGraphExtensionBaseType());
			if (baseGraphExtensionType == null)
				return null;

			var graphExtTypeArgs = baseGraphExtensionType.TypeArguments;

			if (graphExtTypeArgs.Length == 0)
				return null;

			ITypeSymbol firstTypeArg = graphExtTypeArgs.Last();
			return firstTypeArg.IsPXGraph()
				? firstTypeArg
				: null;
		}

		public static bool IsDelegateForViewInPXGraph(this IMethodSymbol method, PXContext pxContext)
		{
			if (method == null || method.ReturnType.SpecialType != SpecialType.System_Collections_IEnumerable)
				return false;

			INamedTypeSymbol containingType = method.ContainingType;

			if (containingType == null || !containingType.IsPXGraphOrExtension(pxContext))
				return false;

			return containingType.GetMembers()
								 .OfType<IFieldSymbol>()
								 .Where(field => field.Type.InheritsFrom(pxContext.PXSelectBase.Type))
								 .Any(field => string.Equals(field.Name, method.Name, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Get view's DAC for which the view was declared.
		/// </summary>
		/// <param name="pxView">The view to act on.</param>
		/// <param name="pxContext">Context.</param>
		/// <returns>
		/// The DAC from view.
		/// </returns>
		public static ITypeSymbol GetDacFromView(this ITypeSymbol pxView, PXContext pxContext)
		{
			pxContext.ThrowOnNull(nameof(pxContext));

			if (pxView?.InheritsFrom(pxContext.PXSelectBase.Type) != true)
				return null;

			INamedTypeSymbol baseViewType;

			if (pxView.IsFbqlView(pxContext))
			{
				
				baseViewType = pxView.BaseType.ContainingType.GetBaseTypesAndThis()
															 .OfType<INamedTypeSymbol>()
															 .FirstOrDefault(t => t.OriginalDefinition.Equals(pxContext.BQL.PXViewOf));
			}
			else
			{
				baseViewType = pxView.GetBaseTypesAndThis()
									 .OfType<INamedTypeSymbol>()
									 .FirstOrDefault(type => !type.IsCustomBqlCommand(pxContext));

				if (baseViewType?.IsBqlCommand() != true)
					return null;
			}

			if (baseViewType == null || baseViewType.TypeArguments.Length == 0)
			{
				return null;
			}
			 
			return baseViewType.TypeArguments[0];
		}

		/// <summary>
		/// Get action's DAC for which the action was declared.
		/// </summary>
		/// <param name="pxAction">The action to act on.</param>
		/// <returns>
		/// The DAC from action.
		/// </returns>
		public static ITypeSymbol GetDacFromAction(this INamedTypeSymbol pxAction)
		{
			if (pxAction?.IsPXAction() != true)
				return null;

			ImmutableArray<ITypeSymbol> actionTypeArgs = pxAction.TypeArguments;

			if (actionTypeArgs.Length == 0)
				return null;

			ITypeSymbol pxActionDacType = actionTypeArgs[0];
			return pxActionDacType.IsDAC()
				? pxActionDacType
				: null;
		}

		public static bool IsValidActionHandler(this IMethodSymbol method, PXContext pxContext)
		{
			method.ThrowOnNull(nameof(method));
			pxContext.ThrowOnNull(nameof(pxContext));

			if (method.Parameters.Length == 0)
				return method.ReturnsVoid;
			else
			{
				return method.Parameters[0].Type.InheritsFromOrEquals(pxContext.PXAdapterType) &&
					   method.ReturnType.InheritsFromOrEquals(pxContext.SystemTypes.IEnumerable, includeInterfaces: true);
			}
		}

		public static bool IsValidViewDelegate(this IMethodSymbol method, PXContext pxContext)
		{
			method.ThrowOnNull(nameof(method));
			pxContext.ThrowOnNull(nameof(pxContext));

			return method.ReturnType.Equals(pxContext.SystemTypes.IEnumerable) &&
				   method.Parameters.All(p => p.RefKind != RefKind.Ref);
		}

		/// <summary>
		/// Get declared primary DAC from graph or graph extension.
		/// </summary>
		/// <param name="graphOrExtension">The graph or graph extension to act on.</param>
		/// <param name="pxContext">Context.</param>
		/// <returns>
		/// The declared primary DAC from graph or graph extension.
		/// </returns>
		public static ITypeSymbol GetDeclaredPrimaryDacFromGraphOrGraphExtension(this ITypeSymbol graphOrExtension, PXContext pxContext)
		{
			pxContext.ThrowOnNull(nameof(pxContext));
			bool isGraph = graphOrExtension?.InheritsFrom(pxContext.PXGraph.Type) ?? false;

			if (!isGraph && !graphOrExtension?.InheritsFrom(pxContext.PXGraphExtensionType) != true)
				return null;

			ITypeSymbol graph = isGraph
				? graphOrExtension
				: graphOrExtension.GetGraphFromGraphExtension(pxContext);

			var baseGraphType = graph.GetBaseTypesAndThis()
									 .OfType<INamedTypeSymbol>()
									 .FirstOrDefault(type => IsGraphWithPrimaryDacBaseGenericType(type)) as INamedTypeSymbol;

			if (baseGraphType == null || baseGraphType.TypeArguments.Length < 2)
				return null;

			ITypeSymbol primaryDacType = baseGraphType.TypeArguments[1];
			return primaryDacType.IsDAC() ? primaryDacType : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsGraphWithPrimaryDacBaseGenericType(INamedTypeSymbol type) =>
			type.TypeArguments.Length >= 2 && type.Name == TypeNames.PXGraph;

		public static (MethodDeclarationSyntax Node, IMethodSymbol Symbol) GetGraphExtensionInitialization
			(this INamedTypeSymbol typeSymbol, PXContext pxContext, CancellationToken cancellation = default)
		{
			typeSymbol.ThrowOnNull(nameof(typeSymbol));

			IMethodSymbol initialize = typeSymbol.GetMembers()
									   .OfType<IMethodSymbol>()
									   .Where(m => pxContext.PXGraphExtensionInitializeMethod.Equals(m.OverriddenMethod))
									   .FirstOrDefault();
			if (initialize == null)
				return (null, null);

			SyntaxReference reference = initialize.DeclaringSyntaxReferences.FirstOrDefault();
			if (reference == null)
				return (null, null);

			if (!(reference.GetSyntax(cancellation) is MethodDeclarationSyntax node))
				return (null, null);

			return (node, initialize);
		}
	}
}