namespace Backend.API.Services;

public interface IPasswordResetService
{
    Task RequestResetAsync(string email);
    Task<PasswordResetStatus> ResetAsync(string email, string code, string newPassword);
}

public enum PasswordResetStatus
{
    Success,
    InvalidCode,
    Expired,
    TooManyAttempts
}
