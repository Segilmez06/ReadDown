using Markdig;

using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

using Svg;

using ReadDown.Properties;
using ReadDown.Utils;

using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ReadDown
{
    public partial class Viewer : Form
    {
        WebView2 Renderer = new();

        string FileName;
        string Content;
        string MD;

        string FilePath;

        IntPtr HWnd;

        public Viewer(string OpenFilePath = "")
        {
            InitializeComponent();

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
            string md_html = Markdown.ToHtml(MD, pipeline);

            string style = Resources.style;
            string frame = Resources.frame;

            Content = frame.Replace("$0", style).Replace("$1", md_html);
            #endregion

            InitializeAsync();
        }

        async void InitializeAsync()
        {
            Renderer.Dock = DockStyle.Fill;
            Renderer.BackColor = Color.FromArgb(37, 37, 37);
            await Renderer.EnsureCoreWebView2Async(null);

            var setttings = Renderer.CoreWebView2.Settings;
            setttings.IsScriptEnabled = true;
            setttings.IsStatusBarEnabled = false;
            setttings.AreBrowserAcceleratorKeysEnabled = true;
#if DEBUG
            setttings.AreDevToolsEnabled = true;
#else
            setttings.AreDevToolsEnabled = false;
#endif

            
            Renderer.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
            Renderer.SourceChanged += Renderer_SourceChanged;
            await InitPageAsync();
            Controls.Add(Renderer);
        }

        private async void Renderer_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            Regex linkRegex = new(@"[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
            string blockedPage = "about:blank#blocked";
            string l = Renderer.Source.ToString();
            if (l == blockedPage || linkRegex.IsMatch(l))
            {
                await InitPageAsync();
            }
        }

        private async Task InitPageAsync()
        {
            Renderer.NavigateToString(Content);
            await Renderer.ExecuteScriptAsync(Resources.redirect);
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
                OpenFileDialog openDialog = new();
                openDialog.Filter = "Markdown document (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*";
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
                SaveFileDialog saveDialog = new();
                saveDialog.Filter = "PDF document (*.pdf)|*.pdf|HTML document (*.html)|*.html|All files (*.*)|*.*";
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
                    else
                    {
                        File.WriteAllText(saveDialog.FileName, MD);
                    }
                }
            };
            InsertNewItem(ContextMenuList, ExportItem);

#if DEBUG
            var Seperator = CreateNewItem(Renderer, "", null, CoreWebView2ContextMenuItemKind.Separator);
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

        private CoreWebView2ContextMenuItem CreateNewItem(WebView2 Renderer, string Label, Stream Icon = null, CoreWebView2ContextMenuItemKind ItemKind = CoreWebView2ContextMenuItemKind.Command)
        {
            return Renderer.CoreWebView2.Environment.CreateContextMenuItem(Label, Icon, ItemKind);
        }
        #endregion

    }
}