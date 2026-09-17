using System.Resources;

namespace BudgetFriend.App.Resources.Localization;

/// <summary>
/// Provides access to localized UI strings via .NET resource infrastructure
/// (<c>.resx</c> + <see cref="ResourceManager"/>). The designer file is
/// intentionally omitted; use the helper methods or
/// <see cref="System.Resources.ResourceManager"/> directly.
/// </summary>
internal static partial class AppStrings {
    internal static readonly ResourceManager ResourceManager = new(
        typeof(AppStrings).FullName!,
        typeof(AppStrings).Assembly);
}
