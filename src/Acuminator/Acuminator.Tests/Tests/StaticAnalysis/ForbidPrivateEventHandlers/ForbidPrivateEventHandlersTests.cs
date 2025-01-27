﻿using System.Threading.Tasks;
using Acuminator.Analyzers.StaticAnalysis;
using Acuminator.Analyzers.StaticAnalysis.ForbidPrivateEventHandlers;
using Acuminator.Analyzers.StaticAnalysis.PXGraph;
using Acuminator.Tests.Helpers;
using Acuminator.Tests.Verification;
using Acuminator.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Acuminator.Tests.Tests.StaticAnalysis.ForbidPrivateEventHandlers
{
	public class ForbidPrivateEventHandlersTests : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new PXGraphAnalyzer(
				CodeAnalysisSettings.Default
									.WithRecursiveAnalysisEnabled()
									.WithStaticAnalysisEnabled()
									.WithSuppressionMechanismDisabled(),
				new ForbidPrivateEventHandlersAnalyzer());

		[Theory]
		[EmbeddedFileData("PrivateModifier.cs")]
		public async Task PrivateModifierNotAllowed(string source)
		{
			await VerifyCSharpDiagnosticAsync(source,
				Descriptors.PX1077_EventHandlersShouldNotBePrivate.CreateFor(8, 16),
				Descriptors.PX1077_EventHandlersShouldBeProtectedVirtual.CreateFor(12, 23, "protected virtual"),
				Descriptors.PX1077_EventHandlersShouldNotBePrivate.CreateFor(19, 16),
				Descriptors.PX1077_EventHandlersShouldBeProtectedVirtual.CreateFor(23, 18, "protected virtual"),
				Descriptors.PX1077_EventHandlersShouldBeProtectedVirtual.CreateFor(27, 27, "protected virtual"),
				Descriptors.PX1077_EventHandlersShouldBeProtectedVirtual.CreateFor(31, 26, "protected virtual")
			);
		}

		[Theory]
		[EmbeddedFileData("ContainerWithInterface.cs")]
		public async Task ContainerWithInterface(string source)
		{
			// The test should return exactly two errors.

			await VerifyCSharpDiagnosticAsync(source,
				Descriptors.PX1077_EventHandlersShouldNotBeExplicitInterfaceImplementations.CreateFor(12, 26),
				Descriptors.PX1077_EventHandlersShouldNotBeExplicitInterfaceImplementations.CreateFor(17, 26)
			);
		}

		[Theory]
		[EmbeddedFileData("SealedContainer.cs")]
		public async Task SealedContainer(string source)
		{
			await VerifyCSharpDiagnosticAsync(source,
				Descriptors.PX1077_EventHandlersShouldNotBePrivate.CreateFor(8, 16)
			);
		}

		[Theory]
		[EmbeddedFileData("AbstractHandler.cs")]
		public async Task AbstractHandler(string source)
		{
			await VerifyCSharpDiagnosticAsync(source,
				Descriptors.PX1077_EventHandlersShouldBeProtectedVirtual.CreateFor(8, 24, "protected")
			);
		}

		[Theory]
		[EmbeddedFileData("ContainerWithInterface_Expected.cs")]
		public async Task ExpectedFileCheck_ExplicitInterfaceImplementations(string source)
		{
			// The test should return exactly two errors. There is no code fix for the explicit interface implementations.

			await VerifyCSharpDiagnosticAsync(source,
				Descriptors.PX1077_EventHandlersShouldNotBeExplicitInterfaceImplementations.CreateFor(12, 26),
				Descriptors.PX1077_EventHandlersShouldNotBeExplicitInterfaceImplementations.CreateFor(17, 26)
			);
		}

		[Theory]
		[EmbeddedFileData("InvalidHandlerModifier_Expected.cs")]
		public async Task ExpectedFileCheck_WrongModifiers(string source)
		{
			await VerifyCSharpDiagnosticAsync(source);
		}

		[Theory]
		[EmbeddedFileData("PrivateModifier_Expected.cs")]
		public async Task ExpectedFilesCheck_PrivateModifiers(string source)
		{
			await VerifyCSharpDiagnosticAsync(source);
		}

		[Theory]
		[EmbeddedFileData("ModifierComments_Expected.cs")]
		public async Task ExpectedFilesCheck_ModifierWithComments(string source)
		{
			await VerifyCSharpDiagnosticAsync(source);
		}

		[Theory]
		[EmbeddedFileData("SealedContainer_Expected.cs")]
		public async Task ExpectedFilesCheck_SealedContainer(string source)
		{
			await VerifyCSharpDiagnosticAsync(source);
		}

		[Theory]
		[EmbeddedFileData("AbstractHandler_Expected.cs")]
		public async Task ExpectedFilesCheck_AbstractHandler(string source)
		{
			await VerifyCSharpDiagnosticAsync(source);
		}
	}
}
