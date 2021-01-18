namespace gpm_vibration_module_api
{
    /// <summary>
    /// 數據封包擷取模式
    /// </summary>
    public enum DAQMode
    {
        /// <summary>
        /// 取樣率8K，With Gap
        /// </summary>
        High_Sampling = 8000,
        /// <summary>
        /// 取樣率4K, Without Gap
        /// </summary>
        Low_Sampling = 4000,
        BULK = 8001
    }
}
