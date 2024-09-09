namespace Api.Services.Models;

public readonly struct RegistrationModel
{
    public string Login { get; init; }

    public long AccountId { get; init; }
}