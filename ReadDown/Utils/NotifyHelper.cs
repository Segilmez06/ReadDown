using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadDown.Utils
{
    public class NotifyHelper
    {
        public static string ShowNotification(string Title, string Description, List<ToastButton> Buttons, string DataKey)
        {
            string result = "";
            ToastContentBuilder Notify = new();
            ToastNotificationManagerCompat.OnActivated += delegate(ToastNotificationActivatedEventArgsCompat e) 
            {
                ToastArguments args = ToastArguments.Parse(e.Argument);
                result = args[DataKey];
            };
            Notify.AddText(Title);
            Notify.AddText(Description);
            foreach (ToastButton btn in Buttons)
            {
                Notify.AddButton(btn);
            }
            Notify.Show();
            while (result.Length < 1){;}
            return result;
        }
        
        //async void ShowToast()
        //{
        //    ToastContentBuilder Notify = new();
        //    ToastNotificationManagerCompat.OnActivated += ToastActivated;
        //    Notify.AddText("ReadDown");
        //    Notify.AddText("Do you want to associate ReadDown with Markdown files?");

        //    ToastButton YesBtn = new();
        //    YesBtn.SetContent("Sure!");
        //    YesBtn.AddArgument("data", "yes");

        //    ToastButton NoBtn = new();
        //    NoBtn.SetContent("No thanks.");
        //    NoBtn.AddArgument("data", "no");

        //    Notify.AddButton(YesBtn);
        //    Notify.AddButton(NoBtn);
        //    Notify.Show();
        //}

        //private void ToastActivated(ToastNotificationActivatedEventArgsCompat e)
        //{
        //    ToastArguments args = ToastArguments.Parse(e.Argument);
        //    string Result = args["data"];
        //    if (Result == "yes")
        //    {
        //        ProcessStartInfo psi = new();
        //        psi.FileName = Application.ExecutablePath;
        //        psi.Arguments = "--set-default";
        //        psi.Verb = "runas";
        //        Process.Start(psi);


        //        //var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        //        //ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
        //        //startInfo.Verb = "runas";
        //        //System.Diagnostics.Process.Start(startInfo);


        //        //DirectoryInfo d = Directory.CreateTempSubdirectory();
        //        //d.Create();
        //        //string FilePath = Path.Combine(d.FullName, "success.md");
        //        //File.CreateText(FilePath);
        //        //File.AppendAllText(FilePath, Resources.Success);
        //        //Process proc = new();
        //        //proc.EnableRaisingEvents = false;
        //        //proc.StartInfo.FileName = "rundll32.exe";
        //        //proc.StartInfo.Arguments = "shell32,OpenAs_RunDLL " + FilePath;
        //        //proc.Start();
        //    }
        //}
    }
}
