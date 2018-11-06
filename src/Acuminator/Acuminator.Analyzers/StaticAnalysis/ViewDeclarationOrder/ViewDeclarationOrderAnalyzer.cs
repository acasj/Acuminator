﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Analyzers.StaticAnalysis.PXGraph;
using Acuminator.Utilities.Roslyn.Semantic.PXGraph;

namespace Acuminator.Analyzers.StaticAnalysis.ViewDeclarationOrder
{
	/// <summary>
	/// An analyzer for the order of view declaration in graph/graph extension.
	/// </summary>
	public partial class ViewDeclarationOrderAnalyzer : IPXGraphAnalyzer
	{
		private const string InitCacheMappingmethodName = "InitCacheMapping";

		public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.PX1004_ViewDeclarationOrder, Descriptors.PX1006_ViewDeclarationOrder);

		
		public void Analyze(SymbolAnalysisContext symbolContext, PXContext pxContext, PXGraphSemanticModel graphSemanticModel)
		{

			if (graphSemanticModel.ViewsByNames.Count == 0 || IsNewMethodUsedToInitCaches(pxContext))
				return;

			AnalysisContext analysisContext = new AnalysisContext(symbolContext, graphSemanticModel);

			symbolContext.CancellationToken.ThrowIfCancellationRequested();
			RunAnalysisOnGraphViewsToFindTwoCacheCases(analysisContext);
			symbolContext.CancellationToken.ThrowIfCancellationRequested();	
		}

		/// <summary>
		/// Starting from the Acumatica 2018R2 version a new method is used to initialize caches with explicit ordering of caches.
		/// </summary>
		/// <returns/>
		private static bool IsNewMethodUsedToInitCaches(PXContext pxContext)
		{
			var baseGraphType = pxContext.PXGraph.Type;
			IMethodSymbol initCachesNewMethod = baseGraphType.GetMembers(InitCacheMappingmethodName)
															 .OfType<IMethodSymbol>()
															 .FirstOrDefault(method => method.ReturnsVoid && method.Parameters.Length == 1);
			return initCachesNewMethod != null;
		}

		private static void RunAnalysisOnGraphViewsToFindTwoCacheCases(AnalysisContext analysisContext)
		{
			foreach (DataViewInfo viewInfo in analysisContext.GetViewsToAnalyze())
			{
				ITypeSymbol viewDacType = viewInfo.ViewDAC;

				if (!viewDacType.IsDAC())
					continue;

				DiagnosticDescriptor descriptor = AnalyzeGraphViewOnForwardPass(analysisContext, viewInfo, viewDacType);

				if (analysisContext.AnalysisPassInfoByDacType.TryGetValue(viewDacType, out AnalysisDacInfo analysisPassInfo))
				{
					analysisPassInfo.VisitedViews.Add(viewInfo);
				}
				else
				{
					analysisPassInfo = new AnalysisDacInfo(viewDacType, viewInfo);
					analysisContext.AnalysisPassInfoByDacType[viewDacType] = analysisPassInfo;
				}

				if (descriptor != null)
				{
					analysisPassInfo.DiagnosticDescriptor = descriptor;
				}
			}
		}

		/// <summary>
		/// Analyze graph view on forward pass to find if PX1004 should be shown and returns the diagnostic descriptor if the diagnostic should be shown for the view. Otherwise returns null.
		/// </summary>
		/// <param name="analysisContext">Context for the analysis.</param>
		/// <param name="viewInfo">Information describing the view.</param>
		/// <param name="viewDacType">Type of the view DAC.</param>
		/// <returns/>
		private static DiagnosticDescriptor AnalyzeGraphViewOnForwardPass(AnalysisContext analysisContext, DataViewInfo viewInfo, 
																		  ITypeSymbol viewDacType)
		{
			if (!GraphContainsViewDeclaration(analysisContext.GraphSemanticModel, viewInfo))
			{
				analysisContext.ViewsInBaseGraphs.Add(viewInfo);
				return null;
			}
				
			Location viewLocation = viewInfo.Symbol.Locations[0];
			var visitedBaseDACs = analysisContext.GetVisitedBaseDacs(viewDacType);

			if (analysisContext.AnalysisPassInfoByDacType.TryGetValue(viewDacType, out var analysisPassInfo))  //If View DAC was already met in other view and the number of caches is already decided
			{
				if (analysisPassInfo.HasDiagnostic)  //If the number of caches was already decided for this view
				{
					analysisContext.ReportDiagnosticForBaseDACs(visitedBaseDACs, viewDacType, analysisPassInfo.DiagnosticDescriptor, viewLocation);
					return analysisPassInfo.DiagnosticDescriptor;
				}
				else
				{
					analysisContext.ViewsInGraphNotMarkedOnForwardPass.Add(viewInfo);
					return null;
				}				
			}
			else if (visitedBaseDACs.Any())         //If the DAC is met for a first time and the number of caches is decided now
			{
				analysisContext.ReportDiagnosticForBaseDACs(visitedBaseDACs, viewDacType, Descriptors.PX1004_ViewDeclarationOrder, viewLocation);
				return Descriptors.PX1004_ViewDeclarationOrder;
			}
			else
			{
				analysisContext.ViewsInGraphNotMarkedOnForwardPass.Add(viewInfo);
				return null;
			}
		}

		private static void RunAnalysisOnGraphViewsToFindOneCacheCases(AnalysisContext analysisContext)
		{
			var dacsDeclaredInBaseGraphs = analysisContext.ViewsInBaseGraphs
														  .Select(view => view.ViewDAC)
														  .Distinct()
														  .ToList();

			for (int i = analysisContext.ViewsInGraphNotMarkedOnForwardPass.Count - 1; i >= 0; i++)
			{
				DataViewInfo view = analysisContext.ViewsInGraphNotMarkedOnForwardPass[i];
				ITypeSymbol viewDacType = view.ViewDAC;
				var defivedDacsInBaseGraph = dacsDeclaredInBaseGraphs.Any(dacInBaseGraph => 
																			dacInBaseGraph.InheritsFrom(viewDacType));
				if ()
				{
					analysisContext.ReportDiagnosticForBaseDACs(visitedBaseDACs, viewDacType, 
																Descriptors.PX1004_ViewDeclarationOrder, viewLocation);
				}
			}
		}


		private static bool GraphContainsViewDeclaration(PXGraphSemanticModel graphSemanticModel, DataViewInfo viewInfo) =>
			graphSemanticModel.Symbol.OriginalDefinition?.Equals(viewInfo.Symbol.ContainingType?.OriginalDefinition) ?? false;
	}
}