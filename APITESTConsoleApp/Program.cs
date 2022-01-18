using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using gpm_vibration_module_api;

namespace APITESTConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            GPMModuleAPI_HSR api = new GPMModuleAPI_HSR();
            bool connected = api.Connect("192.168.0.59", 5000).Result == 0;
            if (!connected)
                throw new Exception("連線失敗");
            while (true)
            {
                var dataSet = api.GetData(true, true).Result;

                Console.WriteLine("Velocity P2P - " + dataSet.PhysicalQuantity.X.Velocity.P2P + dataSet.PhysicalQuantity.X.Velocity.Physical_unit);
                Console.WriteLine("Displacement P2P - " + dataSet.PhysicalQuantity.X.Displacement.P2P + dataSet.PhysicalQuantity.X.Displacement.Physical_unit);
                Thread.Sleep(1000);
            }
            Console.ReadKey();
        }
    }
}
