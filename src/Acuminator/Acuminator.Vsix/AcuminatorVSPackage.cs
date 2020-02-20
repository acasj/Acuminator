﻿using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using EnvDTE80;
using System.ComponentModel.Design;
using Acuminator.Vsix.GoToDeclaration;
using Acuminator.Vsix.Settings;
using Acuminator.Vsix.Logger;
using Acuminator.Vsix.ToolWindows.CodeMap;
using Acuminator.Vsix.DiagnosticSuppression;
using Acuminator.Vsix.Formatter;
using Acuminator.Vsix.Utilities;
using Acuminator.Utilities;
using Acuminator.Utilities.DiagnosticSuppression;
using Microsoft.VisualStudio.Threading;

namespace Acuminator.Vsix
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.Debugging_string, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(AcuminatorVSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", 
                     Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideOptionPage(typeof(GeneralOptionsPage), SettingsCategoryName, GeneralOptionsPage.PageTitle,
					   categoryResourceID: 201, pageNameResourceID: 202, supportsAutomation: true, SupportsProfiles = true)]
	[ProvideToolWindow(typeof(CodeMapWindow), MultiInstances = false, Transient = true, Orientation = ToolWindowOrientation.Left,
					   Style = VsDockStyle.Linked)]
	public sealed class AcuminatorVSPackage : AsyncPackage
	{
		private const int TotalLoadSteps = 5;
		private const string SettingsCategoryName = SharedConstants.PackageName;

		public const string PackageName = SharedConstants.PackageName;

		/// <summary>
		/// AcuminatorVSPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "7e538ed0-0699-434f-acf0-3f6dbc9898ea";

		/// <summary>
		/// The acuminator default command set GUID string.
		/// </summary>
		public const string AcuminatorDefaultCommandSetGuidString = "3cd59430-1e8d-40af-b48d-9007624b3d77";

		[Import]
        internal IClassificationFormatMapService _classificationFormatMapService = null;  //Set via MEF

        public IClassificationFormatMapService ClassificationFormatMapService => _classificationFormatMapService;

        [Import]
        internal IClassificationTypeRegistryService _classificationRegistry = null; // Set via MEF

        public IClassificationTypeRegistryService ClassificationRegistry => _classificationRegistry;

        private const int INSTANCE_UNINITIALIZED = 0;
        private const int INSTANCE_INITIALIZED = 1;
        private static int _instanceInitialized;

        public static AcuminatorVSPackage Instance { get; private set; }

		private Lazy<GeneralOptionsPage> _generalOptionsPage = 
			new Lazy<GeneralOptionsPage>(() => Instance.GetDialogPage(typeof(GeneralOptionsPage)) as GeneralOptionsPage, isThreadSafe: true);

		public GeneralOptionsPage GeneralOptionsPage => _generalOptionsPage.Value;

		internal AcuminatorLogger AcuminatorLogger
		{
			get;
			private set;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="AcuminatorVSPackage"/> class.
        /// </summary>
        public AcuminatorVSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        
            SetupSingleton(this);
        }
          
        private static void SetupSingleton(AcuminatorVSPackage package)
        {
            if (package == null)
                return;

            if (Interlocked.CompareExchange(ref _instanceInitialized, INSTANCE_INITIALIZED, INSTANCE_UNINITIALIZED) == INSTANCE_UNINITIALIZED)
            {
                Instance = package;
            }
        }

		protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread
			await base.InitializeAsync(cancellationToken, progress);

			if (Zombied)
				return;

			await JoinableTaskFactory.SwitchToMainThreadAsync();

			InitializeLogger(progress);
			await InitializeCommandsAsync(progress);

			SolutionEvents.OnAfterBackgroundSolutionLoadComplete += SolutionEvents_OnAfterBackgroundSolutionLoadComplete;
			
			bool isSolutionOpen = await IsSolutionLoadedAsync();

			if (isSolutionOpen)
			{
				await SetupSuppressionManagerAsync();
			}

			cancellationToken.ThrowIfCancellationRequested();

			InitializeCodeAnalysisSettings(progress);
			cancellationToken.ThrowIfCancellationRequested();

			var progressData = new ServiceProgressData(VSIXResource.PackageLoad_WaitMessage, VSIXResource.PackageLoad_Done,
													   currentStep: 5, TotalLoadSteps);
			progress?.Report(progressData);
		}

		private void InitializeLogger(IProgress<ServiceProgressData> progress)
		{
			var progressData = new ServiceProgressData(VSIXResource.PackageLoad_WaitMessage, VSIXResource.PackageLoad_InitLogger,
													   currentStep: 1, TotalLoadSteps);
			progress?.Report(progressData);

			try
			{
				AcuminatorLogger = new AcuminatorLogger(this, swallowUnobservedTaskExceptions: false);
			}
			catch (Exception ex)
			{
				ActivityLog.TryLogError(PackageName,
					$"An error occurred during the logger initialization ({ex.GetType().Name}, message: \"{ex.Message}\")");
			}
		}

		private async System.Threading.Tasks.Task InitializeCommandsAsync(IProgress<ServiceProgressData> progress)
		{
			// if the package is zombied, we don't want to add commands
			if (Zombied)
				return;

			var progressData = new ServiceProgressData(VSIXResource.PackageLoad_WaitMessage, VSIXResource.PackageLoad_InitCommands,
													   currentStep: 2, TotalLoadSteps);
			progress?.Report(progressData);

			OleMenuCommandService oleCommandService = await this.GetServiceAsync<IMenuCommandService, OleMenuCommandService>();

			if (oleCommandService == null)
			{
				InvalidOperationException loadCommandServiceException = new InvalidOperationException("Failed to load OLE command service");
				AcuminatorLogger.LogException(loadCommandServiceException, logOnlyFromAcuminatorAssemblies: false, LogMode.Error);
				return;
			}
			
			FormatBqlCommand.Initialize(this, oleCommandService);
			GoToDeclarationOrHandlerCommand.Initialize(this, oleCommandService);
			BqlFixer.FixBqlCommand.Initialize(this, oleCommandService);

			OpenCodeMapWindowCommand.Initialize(this, oleCommandService);
		}

		private async System.Threading.Tasks.Task<bool> IsSolutionLoadedAsync()
		{
			await JoinableTaskFactory.SwitchToMainThreadAsync();
			var solutionService = await this.GetServiceAsync<SVsSolution, IVsSolution>();

			if (solutionService == null)
				return false;

			int errorCode = solutionService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object isOpenProperty);
			ErrorHandler.ThrowOnFailure(errorCode);

			return isOpenProperty is bool isSolutionOpen && isSolutionOpen;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			AcuminatorLogger?.Dispose();

			SolutionEvents.OnAfterBackgroundSolutionLoadComplete -= SolutionEvents_OnAfterBackgroundSolutionLoadComplete;
		}

		private void SolutionEvents_OnAfterBackgroundSolutionLoadComplete(object sender, EventArgs e)
		{
			#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
			JoinableTaskFactory.Run(SetupSuppressionManagerAsync);
			#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
		}

		private async System.Threading.Tasks.Task SetupSuppressionManagerAsync()
        {
            var workspace = await this.GetVSWorkspaceAsync();
            SuppressionManager.InitOrReset(workspace, generateSuppressionBase: false, 
										   errorProcessorFabric: () => new VsixIOErrorProcessor(),
										   buildActionSetterFabric: () => new VsixBuildActionSetter());
        }

        private void InitializeCodeAnalysisSettings(IProgress<ServiceProgressData> progress)
		{
			var progressData = new ServiceProgressData(VSIXResource.PackageLoad_WaitMessage, VSIXResource.PackageLoad_InitCodeAnalysisSettings,
													   currentStep: 4, TotalLoadSteps);
			progress?.Report(progressData);
			var codeAnalysisSettings = new CodeAnalysisSettingsFromOptionsPage(GeneralOptionsPage);
			GlobalCodeAnalysisSettings.InitializeGlobalSettingsOnce(codeAnalysisSettings);
		}

		#region Package Settings         
		public bool ColoringEnabled => GeneralOptionsPage?.ColoringEnabled ?? true;


        public bool UseRegexColoring => GeneralOptionsPage?.UseRegexColoring ?? false;

        public bool UseBqlOutlining => GeneralOptionsPage?.UseBqlOutlining ?? true;

		public bool UseBqlDetailedOutlining => GeneralOptionsPage?.UseBqlDetailedOutlining ?? true;

        public bool PXGraphColoringEnabled => GeneralOptionsPage?.PXGraphColoringEnabled ?? true;
        
        public bool PXActionColoringEnabled => GeneralOptionsPage?.PXActionColoringEnabled ?? true;

        public bool ColorOnlyInsideBQL => GeneralOptionsPage?.ColorOnlyInsideBQL ?? false;
        #endregion
    }
}
