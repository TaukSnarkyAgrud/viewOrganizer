namespace ChromeTools
{
    public class MessageObject
    {
        public string message;
        public DateTime arrival;
        public MessageObject(string message)
        {
            this.message = message;
            this.arrival = DateTime.Now;
        }
    }
}