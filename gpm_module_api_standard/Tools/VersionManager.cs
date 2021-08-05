using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_vibration_module_api.Tools
{
    internal class VersionManager
    {
        public enum VERSION
        {
            OLD,
            NEW
        }
        private static readonly DateTime DivideDate = new DateTime(2020, 09, 10, 00, 00, 00);
        public static VERSION JudgeSensorConfigVersion(string FilePath)
        {
            var time = File.GetLastWriteTime(FilePath);
            return time < DivideDate ? VERSION.OLD : VERSION.NEW;  
        }
    }
}
