﻿
namespace AntlrVSIX.Package
{
    using AntlrVSIX.About;
    using AntlrVSIX.Extensions;
    using AntlrVSIX.FindAllReferences;
    using AntlrVSIX.GoToDefinition;
    using AntlrVSIX.GoToVisitor;
    using AntlrVSIX.Grammar;
    using AntlrVSIX.NextSym;
    using AntlrVSIX.Options;
    using AntlrVSIX.Reformat;
    using AntlrVSIX.Rename;
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.CommandBars;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Task = System.Threading.Tasks.Task;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(AntlrVSIX.Constants.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly",
        Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(FindRefsWindow))]
    [ProvideService((typeof(IVsSolutionEvents)), IsAsyncQueryable = true)]
    public sealed class AntlrLanguagePackage : AsyncPackage, IVsSolutionEvents
    {
        public AntlrLanguagePackage()
        {
        }

        private IVsSolution solution;
        private uint solutionEventsCookie;

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await base.InitializeAsync(cancellationToken, progress);

            // Attach to solution events
            solution = GetService(typeof(SVsSolution)) as IVsSolution;
            //if (solution == null) Common.Log("Could not get solution");
            solution.AdviseSolutionEvents(this, out solutionEventsCookie);

            FindAllReferencesCommand.Initialize(this);
            FindRefsWindowCommand.Initialize(this);
            GoToDefinitionCommand.Initialize(this);
            GoToVisitorCommand.Initialize(this);
            NextSymCommand.Initialize(this);
            OptionsCommand.Initialize(this);
            ReformatCommand.Initialize(this);
            RenameCommand.Initialize(this);
            AboutCommand.Initialize(this);
        }

        private static AntlrLanguagePackage _instance;
        public static AntlrLanguagePackage Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                _instance = ((Microsoft.VisualStudio.Shell.Interop.IVsShell)GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsShell))).LoadPackage<AntlrLanguagePackage>();
                return _instance;
            }
        }

        public ITextView View { get; set; }
        public SnapshotSpan Span { get; set; }
        public string Classification { get; set; }

        public Dictionary<IWpfTextView, IClassifier> Aggregator { get; } = new Dictionary<IWpfTextView, IClassifier>();
        public Dictionary<IWpfTextView, ITextStructureNavigator> Navigator { get; } = new Dictionary<IWpfTextView, ITextStructureNavigator>();
        public Dictionary<IWpfTextView, SVsServiceProvider> ServiceProvider { get; } = new Dictionary<IWpfTextView, SVsServiceProvider>();

        public IWpfTextView GetActiveView()
        {
            IWpfTextView view = null;
            // Look for currently active view. I don't know if the SVsTextManager
            // provider will do this correctly, so I make sure it returns a consistent
            // view with that stored for the provider.
            foreach (var kvp in AntlrVSIX.Package.AntlrLanguagePackage.Instance.ServiceProvider)
            {
                var k = kvp.Key;
                var v = kvp.Value;
                var service = v.GetService(typeof(SVsTextManager));
                var textManager = service as IVsTextManager2;
                IVsTextView view2;
                int result = textManager.GetActiveView2(1, null, (uint)_VIEWFRAMETYPE.vftCodeWindow, out view2);
                var xxx = view2.GetIWpfTextView();
                if (xxx == k)
                {
                    view = xxx;
                    break;
                }
            }
            return view;
        }

        private void ParseAllFiles()
        {
            // First, open up every .g4 file in project and parse.
            DTE application = DteExtensions.GetApplication();
            if (application == null) return;

            IEnumerable<ProjectItem> iterator = DteExtensions.SolutionFiles(application);
            ProjectItem[] list = iterator.ToArray();
            foreach (var item in list)
            {
                string file_name = item.Name;
                if (file_name != null)
                {
                    if (!file_name.IsAntlrSuffix()) continue;
                    try
                    {
                        object prop = item.Properties.Item("FullPath").Value;
                        string ffn = (string) prop;
                        if (!ParserDetails._per_file_parser_details.ContainsKey(ffn))
                        {
                            StreamReader sr = new StreamReader(ffn);
                            ParserDetails.Parse(sr.ReadToEnd(), ffn);
                        }
                    }
                    catch (Exception eeks)
                    {
                    }
                }
            }
        }


        public int OnAfterOpenProject(IVsHierarchy aPHierarchy, int aFAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy aPHierarchy, int aFRemoving, ref int aPfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy aPHierarchy, int aFRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy aPStubHierarchy, IVsHierarchy aPRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy aPRealHierarchy, ref int aPfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy aPRealHierarchy, IVsHierarchy aPStubHierarchy)
        {
            return VSConstants.S_OK;
        }


        public int OnAfterOpenSolution(object aPUnkReserved, int aFNewSolution)
        {
            ParseAllFiles();
            return VSConstants.S_OK;
        }


        public int OnQueryCloseSolution(object aPUnkReserved, ref int aPfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object aPUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object aPUnkReserved)
        {
            return VSConstants.S_OK;
        }
    }
}
