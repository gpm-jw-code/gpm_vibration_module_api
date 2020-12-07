using gpm_module_api.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm_module_api
{
    interface IModuleClientMode
    {
        bool ServerStartUp(string ServerIP, int ServerPort, out string ErrorCode);
    }
}
