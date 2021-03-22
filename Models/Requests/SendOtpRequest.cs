namespace Models.Requests
{
    public class SendOtpRequest
    {
        public string Login { get; set; }
        public OtpType Type { get; set; }
    }
}