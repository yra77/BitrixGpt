

namespace BitrixGpt.Bitrix24.Models
{

    public class BitrixResponse<T>
    {
        public T Result { get; set; }
    }

    class Contact
    {
        public int ID { get; set; }
        public string NAME { get; set; }
        public string LAST_NAME { get; set; }
        public string SOURCE_ID { get; set; }
        public List<Phone> PHONE { get; set; } = [];
        public List<Im> IM { get; set; } = [];
    }

    class Im
    {
        public string VALUE { get; set; }
    }

    class Phone
    {
        public string VALUE { get; set; }
    }
}