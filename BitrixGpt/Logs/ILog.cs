

namespace BitrixGpt.Logs
{
    internal interface ILog
    {
        LogDelegate LogDelegate { get; set; }
        LogMsgDelegate LogMsgDelegate { get; set; }
    }
}