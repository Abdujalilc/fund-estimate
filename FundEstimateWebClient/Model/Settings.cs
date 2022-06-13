namespace Models
{
    public class Settings
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public FileSettings FileSettings { get; set; }
        public ThreadSettings ThreadSettings { get; set; }
        public UrlAddress UrlAddress { get; set; }
    }
}
