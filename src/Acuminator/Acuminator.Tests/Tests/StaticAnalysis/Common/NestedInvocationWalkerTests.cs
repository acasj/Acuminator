﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acuminator.Tests.Helpers;
using Acuminator.Tests.Verifiers;
using Acuminator.Utils.RoslynExtensions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestHelper;
using Xunit;

namespace Acuminator.Tests
{
	public class NestedInvocationWalkerTests : DiagnosticVerifier
	{
		private class ExceptionWalker : NestedInvocationWalker
		{
			private readonly List<Location> _locations = new List<Location>();
			public IReadOnlyList<Location> Locations => _locations;

			public ExceptionWalker(SemanticModel semanticModel, CancellationToken cancellationToken) 
				: base(semanticModel, cancellationToken)
			{
			}

			public override void VisitThrowStatement(ThrowStatementSyntax node)
			{
				base.VisitThrowStatement(node);
				_locations.Add((OriginalNode ?? node).GetLocation());
			}
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\SanityCheck.cs")]
		public async Task SanityCheck(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode) await document.GetSyntaxRootAsync();

			node.Accept(walker);
			
			walker.Locations.Should().BeEquivalentTo((line: 13, column: 4));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\StaticMethod.cs")]
		public async Task StaticMethod(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode) (await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 13, column: 4));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\PropertyGetter.cs")]
		public async Task PropertyGetter(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 14, column: 16));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\PropertyGetterConditionalAccess.cs")]
		public async Task PropertyGetterConditionalAccess(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 14, column: 16));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\PropertySetter.cs")]
		public async Task PropertySetter(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 14, column: 4));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\PropertySetterFromInitializer.cs")]
		public async Task PropertySetterFromInitializer(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 13, column: 26));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\PropertyValid.cs")]
		public async Task Property_ShouldNotFindAnything(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEmpty();
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\Constructor.cs")]
		public async Task Constructor(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 13, column: 14));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\LocalLambda.cs")]
		public async Task LocalLambda(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 13, column: 20));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\InstanceMethod.cs")]
		public async Task InstanceMethod(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 14, column: 4));
		}

		[Theory]
		[EmbeddedFileData(@"Common\NestedInvocationWalker\InstanceMethodConditionalAccess.cs")]
		public async Task InstanceMethodConditionalAccess(string text)
		{
			Document document = CreateDocument(text);
			SemanticModel semanticModel = await document.GetSemanticModelAsync();
			var walker = new ExceptionWalker(semanticModel, CancellationToken.None);
			var node = (CSharpSyntaxNode)(await document.GetSyntaxRootAsync()).DescendantNodes()
				.OfType<ClassDeclarationSyntax>().First();

			node.Accept(walker);

			walker.Locations.Should().BeEquivalentTo((line: 14, column: 4));
		}
	}
}