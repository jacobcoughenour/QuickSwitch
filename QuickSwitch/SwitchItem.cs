using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace QuickSwitch
{
    public class SwitchItem
    {
        public BitmapSource Icon { get; set; }
        public bool IsCurrentFile { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
    }
}
