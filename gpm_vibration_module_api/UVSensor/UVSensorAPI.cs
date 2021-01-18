using gpm_vibration_module_api;
using gpm_vibration_module_api.Tools;
using System;
using System.Threading.Tasks;

namespace gpm_module_api.UVSensor
{

    public class UVSensorAPI : GPMModuleAPI_HSR
    {
        internal UVDataSet PreDataSet = new UVDataSet(0);
        private UVDataSet uv_dataset = new UVDataSet(0);

        public UVSensorAPI(GPMModulesServer.ConnectInState obj)
        {
            Obj = obj;
            AsynchronousClient.client = obj.ClientSocket;
        }

        public GPMModulesServer.ConnectInState Obj { get; }

        public new event Action<DateTime> DisconnectEvent;


        public  async Task<UVDataSet> GetData()
        {
            var state=  await SendMessageMiddleware("READUVVAL\r\n",4,3000);
            if (state.ErrorCode != clsErrorCode.Error.None)
                return new UVDataSet(0) { ErrorCode = (int) state.ErrorCode };
            PreDataSet = ConverterTools.UVPacketToDatatSet(state.DataByteList.ToArray());
            PreDataSet.RecieveTime = DateTime.Now;
            AddNewDataToTempData(PreDataSet);
            return PreDataSet;
        }
        private void AddNewDataToTempData(UVDataSet NewDataSet)
        {
            if (NewDataSet.ErrorCode != 0)
                return;

            if (NewDataSet.ParticalValueDict.Count != 5)
                return;

        }
    }
}
