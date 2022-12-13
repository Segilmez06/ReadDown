using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;

namespace ReadDown.Utils
{
    public static class DocumentConvert
    {
        public static byte[] HtmlToDocx(string Source)
        {
            using (MemoryStream generatedDocument = new MemoryStream())
            {
                using (WordprocessingDocument package = WordprocessingDocument.Create(generatedDocument, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = package.MainDocumentPart;
                    if (mainPart == null)
                    {
                        mainPart = package.AddMainDocumentPart();
                        new Document(new Body()).Save(mainPart);
                    }

                    HtmlConverter converter = new HtmlConverter(mainPart);
                    converter.ParseHtml(Source);

                    mainPart.Document.Save();
                }

                return generatedDocument.ToArray();
            }
        }

        public static string HtmlToRtf(string Source)
        {
            WebBrowser browser = new WebBrowser();
            RichTextBox box = new RichTextBox();
            
            browser.Navigate("about:blank");
            browser.Document.Write(Source);
            
            browser.Document.ExecCommand("SelectAll", false, null);
            browser.Document.ExecCommand("Copy", false, null);
            
            box.SelectAll();
            box.Paste();
            
            browser.Dispose();
            return box.Text;
        }
    }
}
