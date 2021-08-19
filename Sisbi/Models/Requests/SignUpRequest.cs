namespace Models.Requests
{
    public class SignUpRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public Role Role { get; set; }
    }
}