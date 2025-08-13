using tfharchive.archive;

namespace PodExplorer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var form = new ArchiveExplorerForm(Archive.Load("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terminal Velocity\\Terminal Velocity\\CDROM.POD").Entries);
            form.EntryActivated += (s, e) =>
            {
                // your action here — open preview, export, etc.
                MessageBox.Show(
                    $"Open: {e.Entry.Directory}\\{e.Entry.Name}\n" +
                    $"Size: {e.Entry.Size} bytes\nOffset: 0x{e.Entry.Offset:X8}",
                    "Entry Activated");
            };
            Application.Run(form);
        }
    }
}