

namespace BitrixGpt.Bitrix24.Models
{
    public class Chat
    {
        public string ID { get; set; }
        public string ENTITY_ID { get; set; } // Ідентифікатор клієнта або діалогу
        public string TITLE { get; set; } // Назва чату
        public string OWNER_ID { get; set; } // Відповідальний менеджер
    }
}