﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Acuminator.Analyzers.StaticAnalysis.DacNonAbstractFieldType
{
	[Shared]
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class DacNonAbstractFieldTypeFix : PXCodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds { get; } =
			ImmutableArray.Create(Descriptors.PX1024_DacNonAbstractFieldType.Id);

		protected override Task RegisterCodeFixesForDiagnosticAsync(CodeFixContext context, Diagnostic diagnostic)
		{
			return Task.Run(() =>
			{
				string codeActionName = nameof(Resources.PX1024Fix).GetLocalized().ToString();
				CodeAction codeAction =
					CodeAction.Create(codeActionName,
									  cToken => MarkDacFieldAsAbstractAsync(context.Document, context.Span, cToken),
									  equivalenceKey: codeActionName);

				context.RegisterCodeFix(codeAction, diagnostic);
			}, context.CancellationToken);
		}

		private async Task<Document> MarkDacFieldAsAbstractAsync(Document document, TextSpan span, CancellationToken cancellationToken)
		{
			SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken)
											 .ConfigureAwait(false);
			var dacFieldDeclaration = root?.FindNode(span) as ClassDeclarationSyntax;

			if (dacFieldDeclaration == null || cancellationToken.IsCancellationRequested)
				return document;

			SyntaxToken abstractToken = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);

			if (dacFieldDeclaration.Modifiers.Contains(abstractToken))
				return document;

			var modifiedRoot = root!.ReplaceNode(dacFieldDeclaration, dacFieldDeclaration.AddModifiers(abstractToken));
			return document.WithSyntaxRoot(modifiedRoot);
		}
	}
}