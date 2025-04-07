

namespace BitrixGpt.Bitrix24.Models
{
    class OpenLineDialogInfo
    {
        public int ID { get; set; }
        //вроде ответственный
        public int OWNER { get; set; }
        //отправлено ли
        public bool SEND { get; set; }
        public int LAST_MESSAGE_ID { get; set; }
        public string ENTITY_TYPE { get; set; }
        public string ENTITY_ID { get; set; }
        public string ENTITY_DATA_1 { get; set; }
        public string ENTITY_DATA_2 { get; set; }
        public string DIALOG_ID { get; set; }
    }
}