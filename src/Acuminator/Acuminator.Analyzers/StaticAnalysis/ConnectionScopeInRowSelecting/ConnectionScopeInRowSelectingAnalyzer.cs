﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Acuminator.Analyzers.StaticAnalysis.EventHandlers;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Acuminator.Analyzers.StaticAnalysis.ConnectionScopeInRowSelecting
{
	public class ConnectionScopeInRowSelectingAnalyzer : IEventHandlerAnalyzer
	{
		private class Walker : NestedInvocationWalker
		{
			private class PXConnectionScopeVisitor : CSharpSyntaxVisitor<bool>
			{
				private readonly Walker _parent;
				private readonly PXContext _pxContext;

				public PXConnectionScopeVisitor(Walker parent, PXContext pxContext)
				{
					parent.ThrowOnNull(nameof(parent));
					pxContext.ThrowOnNull(nameof (pxContext));

					_parent = parent;
					_pxContext = pxContext;
				}

				public override bool VisitUsingStatement(UsingStatementSyntax node)
				{
					return (node.Declaration?.Accept(this) ?? false) || (node.Expression?.Accept(this) ?? false);
				}

				public override bool VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
				{
					var semanticModel = _parent.GetSemanticModel(node.SyntaxTree);
					if (semanticModel == null)
						return false;

					var symbolInfo = semanticModel.GetSymbolInfo(node.Type);
					return symbolInfo.Symbol?.OriginalDefinition != null 
						&& symbolInfo.Symbol.OriginalDefinition.Equals(_pxContext.PXConnectionScope);
				}
			}

			private static readonly IEnumerable<string> MethodPrefixes = new[] { "Select", "Search", "Update", "Delete" };

			private SymbolAnalysisContext _context;
			private readonly PXContext _pxContext;
			private readonly PXConnectionScopeVisitor _connectionScopeVisitor;
			private bool _insideConnectionScope;

			public Walker(SymbolAnalysisContext context, PXContext pxContext)
				: base(context.Compilation, context.CancellationToken)
			{
				pxContext.ThrowOnNull(nameof (pxContext));

				_context = context;
				_pxContext = pxContext;
				_connectionScopeVisitor = new PXConnectionScopeVisitor(this, pxContext);
			}

			public override void VisitUsingStatement(UsingStatementSyntax node)
			{
				ThrowIfCancellationRequested();

				if (_insideConnectionScope)
				{
					base.VisitUsingStatement(node);
				}
				else
				{
					_insideConnectionScope = node.Accept(_connectionScopeVisitor);
					base.VisitUsingStatement(node);
					_insideConnectionScope = false;
				}
			}

			public override void VisitInvocationExpression(InvocationExpressionSyntax node)
			{
				ThrowIfCancellationRequested();

				if (_insideConnectionScope)
					return;
				
				var methodSymbol = GetSymbol<IMethodSymbol>(node);

				if (methodSymbol != null && IsDatabaseCall(methodSymbol))
				{
					ReportDiagnostic(OriginalNode ?? node);
				}
				else
				{
					base.VisitInvocationExpression(node);
				}
			}

			private bool IsDatabaseCall(IMethodSymbol candidate)
			{
				var containingType = candidate.ContainingType?.OriginalDefinition;
				return MethodPrefixes.Any(p => candidate.Name.StartsWith(p, StringComparison.Ordinal))
				       && containingType != null
				       && (containingType.IsBqlCommand(_pxContext)
				           || containingType.InheritsFromOrEquals(_pxContext.PXViewType)
				           || containingType.InheritsFromOrEquals(_pxContext.PXSelectorAttribute)
				           || containingType.Equals(_pxContext.PXDatabase));
			}

			private void ReportDiagnostic(SyntaxNode node)
			{
				_context.ReportDiagnostic(Diagnostic.Create(Descriptors.PX1042_ConnectionScopeInRowSelecting,
					node.GetLocation()));
			}
		}

		public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Descriptors.PX1042_ConnectionScopeInRowSelecting);
		
		public void Analyze(SymbolAnalysisContext context, PXContext pxContext, EventType eventType)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			
			if (eventType == EventType.RowSelecting)
			{
				var methodSymbol = (IMethodSymbol) context.Symbol;
				var methodSyntax = methodSymbol.GetSyntax(context.CancellationToken) as CSharpSyntaxNode;
				methodSyntax?.Accept(new Walker(context, pxContext));
			}
		}
	}
}