using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReadDown.Utils
{
    public static class Extensions
    {
        public static Bitmap Invert(this Bitmap bmp)
        {
            for (int y = 0; (y <= (bmp.Height - 1)); y++)
            {
                for (int x = 0; (x <= (bmp.Width - 1)); x++)
                {
                    Color inv = bmp.GetPixel(x, y);
                    inv = Color.FromArgb(inv.A, (255 - inv.R), (255 - inv.G), (255 - inv.B));
                    bmp.SetPixel(x, y, inv);
                }
            }
            return bmp;
        }
    }
}
