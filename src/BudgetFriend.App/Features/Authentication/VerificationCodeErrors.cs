namespace BudgetFriend.App.Features.Authentication;

/// <summary>
/// The possible reasons a verify-email / reset-password request was rejected
/// with a 400. Both endpoints share the same failure vocabulary.
/// </summary>
public enum VerificationCodeFailure {
    None,
    Invalid,
    Expired,
    LockedOut
}

/// <summary>
/// Classifies the API's code-failure message so the UI can react appropriately
/// (e.g. clear the code and offer a resend once a code expired or was locked).
/// </summary>
public static class VerificationCodeErrors {
    public static VerificationCodeFailure Classify(string? message) {
        if (string.IsNullOrWhiteSpace(message))
            return VerificationCodeFailure.Invalid;

        if (message.Contains("expired", StringComparison.OrdinalIgnoreCase))
            return VerificationCodeFailure.Expired;

        if (message.Contains("failed attempt", StringComparison.OrdinalIgnoreCase))
            return VerificationCodeFailure.LockedOut;

        return VerificationCodeFailure.Invalid;
    }
}