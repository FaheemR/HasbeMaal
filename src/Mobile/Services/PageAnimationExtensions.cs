namespace HasbeMaal.Mobile.Services;

public static class PageAnimationExtensions
{
	public static Task AnimateEntranceAsync(this ContentPage page)
	{
		ArgumentNullException.ThrowIfNull(page);

		return page.Content is VisualElement content
			? content.FadeInUpAsync()
			: Task.CompletedTask;
	}

	private static async Task FadeInUpAsync(this VisualElement element)
	{
		element.AbortAnimation("HasbeMaalPageEntranceFade");
		element.AbortAnimation("HasbeMaalPageEntranceTranslate");
		element.Opacity = 0;
		element.TranslationY = 18;

		await Task.WhenAll(
			element.FadeToAsync(1, 240, Easing.CubicOut),
			element.TranslateToAsync(0, 0, 280, Easing.CubicOut));
	}
}