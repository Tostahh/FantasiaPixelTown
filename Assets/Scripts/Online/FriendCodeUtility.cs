using System;

public static class FriendCodeUtility
{
    public static string GenerateFriendCode()
    {
        Guid guid = Guid.NewGuid();
        string code = Convert.ToBase64String(guid.ToByteArray())
            .Replace("=", "")
            .Replace("+", "")
            .Replace("/", "")
            .Substring(0, 10)
            .ToUpper();
        return $"FPT-{code}";
    }
}
