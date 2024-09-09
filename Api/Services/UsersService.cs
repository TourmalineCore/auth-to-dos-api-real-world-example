using Api.Services.Models;
using DataAccess.Commands;
using DataAccess.Models;
using DataAccess.Queries;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

public class UsersService
{
    private readonly IExternalServicesHttpClient _externalServicesHttpClient;
    private readonly IFindUserQuery _findUserQuery;
    private readonly ILogger<UsersService> _logger;
    private readonly IPasswordValidator<User> _passwordValidator;
    private readonly UserBlockCommand _userBlockCommand;
    private readonly UserManager<User> _userManager;
    private readonly UserUnblockCommand _userUnblockCommand;

    public UsersService(
        UserManager<User> userManager,
        IFindUserQuery findUserQuery,
        IExternalServicesHttpClient externalServicesHttpClient,
        ILogger<UsersService> logger,
        IPasswordValidator<User> passwordValidator,
        UserBlockCommand userBlockCommand,
        UserUnblockCommand userUnblockCommand)
    {
        _userManager = userManager;
        _findUserQuery = findUserQuery;
        _externalServicesHttpClient = externalServicesHttpClient;
        _logger = logger;
        _passwordValidator = passwordValidator;
        _userBlockCommand = userBlockCommand;
        _userUnblockCommand = userUnblockCommand;
    }

    public async Task RegisterAsync(RegistrationModel registrationModel)
    {
        var user = await _findUserQuery.FindUserByLoginAsync(registrationModel.Login);

        if (user != null)
            throw new NullReferenceException($"User with the login [{registrationModel.Login}] already exists");

        var newUser = new User
        {
            UserName = registrationModel.Login,
            AccountId = registrationModel.AccountId
        };

        var userPassword = PasswordGenerator.GeneratePassword(5, 5, 5, 5);
        Console.WriteLine("**************************** " + userPassword);
        await _userManager.CreateAsync(newUser, userPassword);

        try
        {
            var passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(newUser);
            await _externalServicesHttpClient.SendPasswordCreationLink(registrationModel.Login, passwordResetToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"[{nameof(UsersService)}]: Couldn't send a link on password creation for user [{registrationModel.Login}]. Exception details: {ex.Message}");
        }
    }

    public async Task BlockAsync(long accountId)
    {
        await _userBlockCommand.ExecuteAsync(accountId);
    }

    public async Task UnblockAsync(long accountId)
    {
        await _userUnblockCommand.ExecuteAsync(accountId);
    }

    // toDo: we need it? do we have smtp client for it?
    public async Task ResetPasswordAsync(string corporateEmail)
    {
        var user = await _findUserQuery.FindUserByLoginAsync(corporateEmail);
        if (user == null)
            throw new NullReferenceException("User doesn't exists");
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        await _externalServicesHttpClient.SendPasswordResetLink(corporateEmail, resetToken);
    }

    public async Task ChangePasswordAsync(PasswordChangeModel passwordChangeModel)
    {
        var user = await _findUserQuery.FindUserByLoginAsync(passwordChangeModel.Login);

        if (user == null)
            throw new NullReferenceException($"User with the login [{passwordChangeModel.Login}] doesn't exists");

        var passwordResetTokenIsValid = await _userManager.VerifyUserTokenAsync(user,
            _userManager.Options.Tokens.PasswordResetTokenProvider,
            UserManager<User>.ResetPasswordTokenPurpose, passwordChangeModel.PasswordResetToken);

        if (!passwordResetTokenIsValid) throw new Exception("Password reset token is invalid");

        var newPasswordValidationResult =
            await _passwordValidator.ValidateAsync(_userManager, user, passwordChangeModel.NewPassword);

        if (!newPasswordValidationResult.Succeeded) throw new ArgumentException("New password is invalid");

        await _userManager.ResetPasswordAsync(user, passwordChangeModel.PasswordResetToken,
            passwordChangeModel.NewPassword);
    }
}