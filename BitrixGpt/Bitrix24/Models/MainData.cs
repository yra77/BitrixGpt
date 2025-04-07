

namespace BitrixGpt.Bitrix24.Models
{
    class MainData
    {
        public Deal Deal { get; set; }
        public int ContactID { get; set; }
        public int OL_OpentChatID { get; set; }
        public int OL_DialogID { get; set; }
        public int InpMgID_Last { get; set; }
        public string ClientName { get; set; }
        public int  DIALOG_SESSIONID { get; set; }
    }
}