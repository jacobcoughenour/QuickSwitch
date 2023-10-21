using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.Diagnostics;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;

namespace QuickSwitch
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SwitchCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("6c06d782-22f3-4ea5-b7ca-a01312a0b347");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SwitchCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SwitchCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in SwitchCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SwitchCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // get current focused document
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var activeDocument = dte.ActiveDocument;

            if (activeDocument == null)
                // no document is open
                return;

            var activeDocumentPath = activeDocument.FullName;
            // get parent directory
            var parentDirectory = System.IO.Path.GetDirectoryName(activeDocumentPath);
            // get file name
            var currentFileName = System.IO.Path.GetFileName(activeDocumentPath);
            var currentFileFirstPart = currentFileName.Substring(0, currentFileName.IndexOf('.'));

            // for each file in the same directory
            var items = new List<SwitchItem>();
            foreach (var file in System.IO.Directory.GetFiles(parentDirectory))
            {
                // get icon for file extension
                using (Icon icon = Icon.ExtractAssociatedIcon(file))
                {
                    var isCurrent = file == activeDocumentPath;
                    var fileName = isCurrent ? currentFileName : System.IO.Path.GetFileName(file);
                    var start = isCurrent ? currentFileFirstPart : fileName.Substring(0, fileName.IndexOf('.'));

                    if (start != currentFileFirstPart)
                        continue;

                    // add to list
                    items.Add(new SwitchItem {
                        Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                        IsCurrentFile = isCurrent,
                        Name = fileName,
                        FullPath = file
                    });
                }
            }

            // open the switch window
            var switchWindow = new SwitchWindow();
            switchWindow.PopulateItems(items);
            switchWindow.ShowModal();

            // open the selected document
            try
            {
                dte.ItemOperations.OpenFile(switchWindow.SelectedItem.FullPath);
            }
            catch (Exception)
            {
            }
        }
    }
}
