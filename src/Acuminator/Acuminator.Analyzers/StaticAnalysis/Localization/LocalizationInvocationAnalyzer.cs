﻿using System.Collections.Immutable;

using Acuminator.Utilities;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Acuminator.Analyzers.StaticAnalysis.Localization
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LocalizationInvocationAnalyzer : PXDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create
            (
                Descriptors.PX1050_HardcodedStringInLocalizationMethod,
                Descriptors.PX1051_NonLocalizableString,
                Descriptors.PX1052_IncorrectStringToFormat,
                Descriptors.PX1053_ConcatenationPriorLocalization
            );

		public LocalizationInvocationAnalyzer() : base() { }

		/// <summary>
		/// Constructor accepting code analysis settings for tests.
		/// </summary>
		/// <param name="codeAnalysisSettings">The code analysis settings.</param>
		public LocalizationInvocationAnalyzer(CodeAnalysisSettings codeAnalysisSettings) : base(codeAnalysisSettings)
		{
		}

		protected override void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext, PXContext pxContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeLocalizationMethodInvocation(syntaxContext, pxContext), 
															 SyntaxKind.InvocationExpression);
        }

        private void AnalyzeLocalizationMethodInvocation(SyntaxNodeAnalysisContext syntaxContext, PXContext pxContext)
        {
			syntaxContext.CancellationToken.ThrowIfCancellationRequested();

			if (syntaxContext.Node is not InvocationExpressionSyntax localizationMethodInvocationNode)
				return;

			SymbolInfo localizationMethodInfo = syntaxContext.SemanticModel.GetSymbolInfo(localizationMethodInvocationNode, 
																						  syntaxContext.CancellationToken);
			var (isFormatMethod, isLocalizationMethod) = GetLocalizationMethodInfoFromSymbolInfo(pxContext, localizationMethodInfo);

			if (!isLocalizationMethod)
				return;

			ExpressionSyntax? stringArgExpression = GetStringArgumentExpressionFromLocalizationMethodInvocation(syntaxContext, 
																												localizationMethodInvocationNode);
			if (stringArgExpression == null)
				return;

			var messageValidator = new LocalizationMessageValidator(syntaxContext, pxContext, ValidationContext.LocalizationMethodCall);
			messageValidator.ValidateMessage(stringArgExpression, isFormatMethod);
        }

        private (bool IsFormatMethod, bool IsLocalizationMethod) GetLocalizationMethodInfoFromSymbolInfo(PXContext pxContext, 
																										 SymbolInfo localizationMethodInfo)
        {
			if (localizationMethodInfo.Symbol == null && localizationMethodInfo.CandidateSymbols.IsEmpty)
				return (IsFormatMethod: false, IsLocalizationMethod: false);

			if (localizationMethodInfo.Symbol is IMethodSymbol localizationMethod)
				return GetLocalizationMethodInfoFromSymbol(localizationMethod, pxContext);

			if (!localizationMethodInfo.CandidateSymbols.IsDefaultOrEmpty)
			{
				foreach (IMethodSymbol candidate in localizationMethodInfo.CandidateSymbols.OfType<IMethodSymbol>())
				{
					var (isFormatMethod, isLocalizationMethod) = GetLocalizationMethodInfoFromSymbol(candidate, pxContext);

					if (isLocalizationMethod)
						return (isFormatMethod, isLocalizationMethod);
				}
			}

            return (IsFormatMethod: false, IsLocalizationMethod: false);
        }

		private (bool IsFormatMethod, bool IsLocalizationMethod) GetLocalizationMethodInfoFromSymbol(IMethodSymbol localizationMethod, PXContext pxContext)
		{
			if (IsFormatMethodSymbol(localizationMethod, pxContext))
				return (IsFormatMethod: true, IsLocalizationMethod: true);

			return (IsFormatMethod: false, IsLocalizationMethod: IsLocalizationNonFormatMethodSymbol(localizationMethod, pxContext));
		}

		private bool IsFormatMethodSymbol(IMethodSymbol method, PXContext pxContext) =>
			pxContext.Localization.PXMessagesFormatMethods.Contains(method, SymbolEqualityComparer.Default) || 
			pxContext.Localization.PXLocalizerFormatMethods.Contains(method, SymbolEqualityComparer.Default);

		private bool IsLocalizationNonFormatMethodSymbol(IMethodSymbol method, PXContext pxContext) =>
			 pxContext.Localization.PXMessagesSimpleMethods.Contains(method, SymbolEqualityComparer.Default) ||
			 pxContext.Localization.PXLocalizerSimpleMethods.Contains(method, SymbolEqualityComparer.Default);

		private ExpressionSyntax? GetStringArgumentExpressionFromLocalizationMethodInvocation(SyntaxNodeAnalysisContext syntaxContext,
																							  InvocationExpressionSyntax localizationMethodInvocationNode)
		{
			ArgumentSyntax? messageArg = localizationMethodInvocationNode.ArgumentList?.Arguments.FirstOrDefault();

			if (messageArg?.Expression == null)
				return null;

			ITypeSymbol? messageArgType = syntaxContext.SemanticModel.GetTypeInfo(messageArg.Expression, syntaxContext.CancellationToken).Type;

			if (messageArgType?.SpecialType != SpecialType.System_String)
				return null;

			return messageArg.Expression;
		}
	}
}
