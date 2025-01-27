﻿
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn;
using Acuminator.Utilities.Roslyn.PXFieldAttributes;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Syntax;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Acuminator.Analyzers.StaticAnalysis.DacPropertyAttributes
{
	[Shared]
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class IncompatibleDacPropertyAndFieldAttributeFix : PXCodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds { get; } =
			ImmutableArray.Create(Descriptors.PX1021_PXDBFieldAttributeNotMatchingDacProperty.Id);

		protected override async Task RegisterCodeFixesForDiagnosticAsync(CodeFixContext context, Diagnostic diagnostic)
		{
			if (!diagnostic.IsRegisteredForCodeFix())
				return;

			SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			SyntaxNode? codeFixNode = root?.FindNode(context.Span);

			if (codeFixNode == null)
				return;
			
			if (codeFixNode is AttributeSyntax attribute)
			{
				RegisterCodeFix(root!, attribute, context, diagnostic);
			}
			else
			{
				RegisterCodeFixForPropertyType(root!, codeFixNode, context, diagnostic);
			}		
		}

		private void RegisterCodeFixForPropertyType(SyntaxNode root, SyntaxNode codeFixNode, CodeFixContext context, Diagnostic diagnostic)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			Location? attributeLocation = diagnostic.AdditionalLocations.FirstOrDefault();

			if (attributeLocation == null)
				return;

			AttributeSyntax? attributeNode = root.FindNode(attributeLocation.SourceSpan) as AttributeSyntax;
			
			if (attributeNode == null)
				return;
			
			RegisterCodeFix(root, attributeNode, context, diagnostic);
		}

		private void RegisterCodeFix(SyntaxNode root, AttributeSyntax attributeNode, CodeFixContext context, Diagnostic diagnostic)
		{
			PropertyDeclarationSyntax? propertyNode = attributeNode.Parent<PropertyDeclarationSyntax>();
			context.CancellationToken.ThrowIfCancellationRequested();

			if (propertyNode == null)
				return;

			string codeActionName = nameof(Resources.PX1021PropertyFix).GetLocalized().ToString();

			CodeAction codeAction =
				CodeAction.Create(codeActionName,
								  cToken => ChangePropertyTypeToAttributeType(context.Document, root, attributeNode, propertyNode, cToken),
								  equivalenceKey: codeActionName);

			context.RegisterCodeFix(codeAction, diagnostic);
		}

		private async Task<Document> ChangePropertyTypeToAttributeType(Document document, SyntaxNode root, AttributeSyntax attributeNode,
																	   PropertyDeclarationSyntax propertyNode, CancellationToken cancellationToken)
		{		
			SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			ITypeSymbol? attributeType = semanticModel?.GetTypeInfo(attributeNode, cancellationToken).Type;
			cancellationToken.ThrowIfCancellationRequested();

			if (attributeType == null)
				return document;
			
			PXContext pxContext = new PXContext(semanticModel!.Compilation, codeAnalysisSettings: null);
			var attributesMetadataProvider = new FieldTypeAttributesMetadataProvider(pxContext);
			var fieldAttributeDataTypes = (from attrInfo in attributesMetadataProvider.GetDacFieldTypeAttributeInfos(attributeType)
										   where attrInfo.IsFieldAttribute && attrInfo.DataType != null
										   select attrInfo.DataType)
										  .ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

			if (fieldAttributeDataTypes.Count != 1) 
				return document;

			ITypeSymbol attributeDataType = fieldAttributeDataTypes.First();
			SyntaxGenerator generator = SyntaxGenerator.GetGenerator(document);
			TypeSyntax? replacingTypeNode = generator.TypeExpression(attributeDataType) as TypeSyntax;

			if (attributeDataType.IsValueType)
			{
				replacingTypeNode = generator.NullableTypeExpression(replacingTypeNode) as TypeSyntax;
			}

			if (replacingTypeNode == null)
				return document;

			cancellationToken.ThrowIfCancellationRequested();

			replacingTypeNode = replacingTypeNode.WithTrailingTrivia(propertyNode.Type.GetTrailingTrivia());
			var propertyModified = propertyNode.WithType(replacingTypeNode);
			var modifiedRoot = root.ReplaceNode(propertyNode, propertyModified);
			return document.WithSyntaxRoot(modifiedRoot);
		}
	}
}