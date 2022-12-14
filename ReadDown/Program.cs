using ReadDown.Utils;

namespace ReadDown
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
            if (Environment.GetCommandLineArgs().Contains("--set-default"))
            {
                ExplorerUtils.SetAssociation(".md", "Zyex.ReadDown.Markdown", Application.ExecutablePath, "Markdown document");
            }
            else
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new Viewer());
            }
        }
    }
}