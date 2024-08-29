namespace Api.Services.Models
{
    public readonly struct PasswordChangeModel
    {
        public string Login { get; init; }

        public string PasswordResetToken { get; init; }

        public string NewPassword { get; init; }
    }
}
