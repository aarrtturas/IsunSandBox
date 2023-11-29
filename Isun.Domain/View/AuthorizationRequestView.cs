namespace Isun.Domain.View;
public record AuthorizationRequestView(string Username, string Password);

public record AuthorizationResponseView(string Token);
