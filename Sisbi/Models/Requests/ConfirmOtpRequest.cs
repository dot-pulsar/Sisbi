namespace Models.Requests
{
    public class ConfirmOtpRequest
    {
        public string Login { get; set; }
        public int Otp { get; set; }
    }
}