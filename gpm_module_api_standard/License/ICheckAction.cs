using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_module_api.License
{
    interface ICheckAction
    {
         LicenseCheckState Check(string licFilePath);
    }
}
