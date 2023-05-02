namespace WebApi.Helpers
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string EmailServerHost { get; set; }
        public string EmailViaSSL { get; set; }
        public string EmailPort { get; set; }
        public string EmailAccountUserName { get; set; }
        public string EmailAccountPW { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailFromName { get; set; }
    }
}