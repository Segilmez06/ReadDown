using Markdig;

using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

using Svg;

using ReadDown.Properties;
using ReadDown.Utils;

using System.IO;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ReadDown
{
    public partial class Viewer : Form
    {
        WebView2 Renderer = new();

        string FileName;
        string Content;
        string MD;
        string RawHMTL;

        string FilePath;

        IntPtr HWnd;

        public Viewer(string OpenFilePath = "")
        {
            ClientSize = new Size(1000, 500);
            ForeColor = Color.White;
            Icon = Resources.RD_Icon;
            MinimumSize = new Size(750, 240);
            ShowIcon = false;

            HWnd = Handle;
            var UISettings = new Windows.UI.ViewManagement.UISettings();
            ApplyTheme(UISettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString());
            UISettings.ColorValuesChanged += (sender, args) =>
            {
                string ColorHex = UISettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background).ToString();
                ApplyTheme(ColorHex);
            };

            #region File handling
            var args = Environment.GetCommandLineArgs();
            if (OpenFilePath.Length > 0)
            {
                FilePath = OpenFilePath;
            }
            else if (args.Length > 1)
            {
                FilePath = args[1];
            }

            MD = Resources.Welcome;

            if (File.Exists(FilePath))
            {
                FileName = Path.GetFileNameWithoutExtension(FilePath);

                MD = File.ReadAllText(FilePath);
            }
            else
            {
                FileName = "Welcome";
            }

            Text = $"{FileName} - ReadDown";

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            RawHMTL = Markdown.ToHtml(MD, pipeline);

            string style = Resources.style;
            string frame = Resources.frame;

            Content = frame.Replace("$0", style).Replace("$1", RawHMTL);
            #endregion

            InitializeAsync();
        }
        
        async void InitializeAsync()
        {
            Renderer.Dock = DockStyle.Fill;
            Renderer.BackColor = Color.FromArgb(37, 37, 37);
            await Renderer.EnsureCoreWebView2Async(null);

            var settings = Renderer.CoreWebView2.Settings;
            settings.IsScriptEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = true;
#if DEBUG
            settings.AreDevToolsEnabled = true;
#else
            settings.AreDevToolsEnabled = false;
#endif

            Renderer.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
            Renderer.NavigationStarting += Renderer_NavigationStarting;
            Renderer.NavigateToString(Content);
            Controls.Add(Renderer);
            
        }

        private void Renderer_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.Contains("#blocked"))
            {
                e.Cancel = true;
            }
            Regex linkRegex = new(@"[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
            string blockedPage = "about:blank#blocked";
            string l = e.Uri;
            if (l == blockedPage)
            {
                Renderer.NavigateToString(Content);
            }
            else if (linkRegex.IsMatch(l))
            {
                e.Cancel = true;
                Process.Start(new ProcessStartInfo()
                {
                    FileName = l,
                    UseShellExecute = true
                });
            }
            
        }

        private void ApplyTheme(string HexValue)
        {
            HexValue = HexValue.ToLower();
            if (HexValue == "#ff000000")
            {
                FormUtils.SetHandleTheme(HWnd, FormUtils.Theme.Dark);
                this.BackColor = Color.FromArgb(37, 37, 37);
            }
            else if (HexValue == "#ffffffff")
            {
                FormUtils.SetHandleTheme(HWnd, FormUtils.Theme.Light);
                this.BackColor = Color.FromArgb(255, 255, 255);
            }
        }

        #region Context Menu
        private void CoreWebView2_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
#pragma warning disable CS8622
            var ContextMenuList = e.MenuItems;
            ClearList(ContextMenuList);

            var openIcon = new MemoryStream();
            SvgDocument.FromSvg<SvgDocument>(Resources.open).Draw().Invert().Save(openIcon, ImageFormat.Png);
            var OpenItem = CreateNewItem(Renderer, "Open file", openIcon);
            OpenItem.CustomItemSelected += delegate (object sender, object EventArgs)
            {
                System.Windows.Forms.OpenFileDialog openDialog = new();
                openDialog.Filter = "Markdown document (*.md)|*.md;*.markdown;*.mkd;*.mkdn;*.mdwn;*.mdown;*.markdn;*.markdown;*.mdtxt;*.mdtext|All files (*.*)|*.*";
                openDialog.FileName = "";
                var result = openDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    new Viewer(openDialog.FileName).Show();
                }
            };
            InsertNewItem(ContextMenuList, OpenItem);

            var exportIcon = new MemoryStream();
            SvgDocument.FromSvg<SvgDocument>(Resources.document_save).Draw().Invert().Save(exportIcon, ImageFormat.Png);
            var ExportItem = CreateNewItem(Renderer, "Export", exportIcon);
            ExportItem.CustomItemSelected += delegate (object sender, object EventArgs) {
                System.Windows.Forms.SaveFileDialog saveDialog = new();
                saveDialog.Filter = "PDF document (*.pdf)|*.pdf|HTML document (*.html)|*.html|Word document (*.docx)|*.docx|Rich Text document (*.rtf)|*.rtf|All files (*.*)|*.*";
                saveDialog.FileName = FileName;
                var result = saveDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    string extension = Path.GetExtension(saveDialog.FileName);
                    if (extension == ".pdf")
                    {
                        Renderer.CoreWebView2.PrintToPdfAsync(saveDialog.FileName);
                    }
                    else if (extension == ".html")
                    {
                        File.WriteAllText(saveDialog.FileName, Content);
                    }
                    else if (extension == ".docx")
                    {
                        File.WriteAllBytes(saveDialog.FileName, DocumentConvert.HtmlToDocx(RawHMTL));
                    }
                    else if (extension == ".rtf")
                    {
                        File.WriteAllText(saveDialog.FileName, DocumentConvert.HtmlToRtf(RawHMTL));
                    }
                    else
                    {
                        File.WriteAllText(saveDialog.FileName, MD);
                    }
                }
            };
            InsertNewItem(ContextMenuList, ExportItem);

            var editIcon = new MemoryStream();
            SvgDocument.FromSvg<SvgDocument>(Resources.open_with).Draw().Invert().Save(editIcon, ImageFormat.Png);
            var EditItem = CreateNewItem(Renderer, "View with another app", editIcon);
            EditItem.CustomItemSelected += delegate (object sender, object EventArgs)
            {
                Process p = new();
                p.EnableRaisingEvents = false;
                p.StartInfo.FileName = "rundll32.exe";
                p.StartInfo.Arguments = "shell32,OpenAs_RunDLL " + FilePath;
                p.Start();
            };
            InsertNewItem(ContextMenuList, EditItem);

#if DEBUG
            var Seperator = CreateNewItem(Renderer, "", new MemoryStream(), CoreWebView2ContextMenuItemKind.Separator);
            InsertNewItem(ContextMenuList, Seperator);

            var devIcon = new MemoryStream();
            SvgDocument.FromSvg<SvgDocument>(Resources.window_dev_tools).Draw().Invert().Save(devIcon, ImageFormat.Png);
            var DevToolsItem = CreateNewItem(Renderer, "Open Developer Tools", devIcon);
            DevToolsItem.CustomItemSelected += delegate (object sender, object EventArgs) {
                Renderer.CoreWebView2.OpenDevToolsWindow();
            };
            InsertNewItem(ContextMenuList, DevToolsItem);
#endif
        }

        private void ClearList(IList<CoreWebView2ContextMenuItem> ContextMenuList)
        {
            ContextMenuList.Clear();
        }

        private void InsertNewItem(IList<CoreWebView2ContextMenuItem> ContextMenuList, CoreWebView2ContextMenuItem MenuItem)
        {
            ContextMenuList.Insert(ContextMenuList.Count, MenuItem);
        }

        private CoreWebView2ContextMenuItem CreateNewItem(WebView2 Renderer, string Label, Stream Icon, CoreWebView2ContextMenuItemKind ItemKind = CoreWebView2ContextMenuItemKind.Command)
        {
            return Renderer.CoreWebView2.Environment.CreateContextMenuItem(Label, Icon, ItemKind);
        }
        #endregion

    }
}