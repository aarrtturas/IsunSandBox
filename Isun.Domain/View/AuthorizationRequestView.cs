namespace Isun.Domain.View;
public record AuthorizationRequestView(string username, string password);

public record AuthorizationResponseView(string token);
