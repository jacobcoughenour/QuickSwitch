using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Task = System.Threading.Tasks.Task;

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

        public static readonly string CommandFullName = "Tools.InvokeSwitchCommand";

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

            GestureMap.Update(dte);

            // open the switch window
            // todo would be cool if we could keep a single instance of the window and just show and hide it
            // that might also get rid of the flashing caused by enabling the mica window style
            var switchWindow = new SwitchWindow();
            var items = SwitchLogic.GetMatchingFiles(activeDocument.FullName).Select(file =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                // get icon for file extension
                using (Icon icon = Icon.ExtractAssociatedIcon(file))
                {
                    return new SwitchItem
                    {
                        Icon = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                        IsCurrentFile = activeDocument.FullName == file,
                        Name = Path.GetFileName(file),
                        FullPath = file
                    };
                }
            });
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
