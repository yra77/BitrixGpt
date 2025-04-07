

namespace BitrixGpt.Bitrix24.Models
{
    class DialogHistory
    {
        public int CHATID { get; set; }
        public int SESSIONID { get; set; }
        public string USERID { get; set; }
        public Dictionary<string, Dialog> MESSAGE { get; set; }
    }

    class Message
    {
       public Dictionary<string, Dialog> MsgArr { get; set; }
    }

    class Dialog
    {
        public string ID { get; set; }
        public string SENDERID { get; set; }
        public string TEXT { get; set; }
        public string TEXTLEGACY { get; set; }
        public string DATE { get; set; }
    }
}