namespace Journal.Models
{
    public class HumanModel(string Email, string Name, string Age, string Info, string Photo)
    {
        public string Email { get; set; } = Email;
        public string Name { get; set; } = Name;
        public string Age { get; set; } = Age;
        public string Info { get; set; } = Info;
        public string Photo { get; set; } = Photo;
    }
}
