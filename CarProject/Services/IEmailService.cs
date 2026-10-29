namespace CarProject.Services;

public interface IEmailService
{
    Task<bool> GuiEmailXacNhan(string email, string token, string callbackUrl);
}
