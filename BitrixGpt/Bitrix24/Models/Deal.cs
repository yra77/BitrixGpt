

namespace BitrixGpt.Bitrix24.Models
{
    class Deal
    {
        public int ID { get; set; }
        public string TITLE { get; set; }
        //стадия сделки
        public string STAGE_ID { get; set; }
        public string CONTACT_ID { get; set; }
        public string ASSIGNED_BY_ID { get; set; }
        public string CLOSED { get; set; }
        //воронка
        public string CATEGORY_ID { get; set; }
        //источник сообщений
        public string SOURCE_ID { get; set; }
        //если null то это сделка иначе лид
        public string LEAD_ID { get; set; }
        public string DATE_CREATE { get; set; }
    }
}