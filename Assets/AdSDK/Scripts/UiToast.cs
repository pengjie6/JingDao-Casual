using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 极轻量的屏幕提示条（Toast）。
///
/// 为什么不用预制体：
///   1. 这个提示要在「死亡界面（timeScale = 0）」和「主菜单」两处都能弹，
///      挂到任何现有 UIView 上都可能被 HideView 一起关掉；
///   2. 不新增预制体就不用动场景，也不会被 OPS 混淆改名后丢脚本引用。
///
/// 做法：首次调用时自己在运行时搭一个 ScreenSpaceOverlay 的 Canvas，
/// 之后复用同一份 UI，只改文字、重新计时。用 unscaledDeltaTime 驱动，
/// 游戏暂停时也能正常淡入淡出。
/// </summary>
public class UiToast : MonoBehaviour
{
	private const float FadeInTime = 0.18f;
	private const float FadeOutTime = 0.32f;
	private const float DefaultHoldTime = 1.4f;

	private static UiToast _instance;

	private GameObject _canvasGo;
	private CanvasGroup _group;
	private RectTransform _panel;
	private TextMeshProUGUI _text;

	private float _elapsed;
	private float _holdTime = DefaultHoldTime;
	private bool _running;

	/// <summary>
	/// 弹一条提示。
	/// </summary>
	/// <param name="message">提示文字</param>
	/// <param name="duration">完全不透明的停留时长（秒），不含淡入淡出</param>
	public static void Show(string message, float duration = DefaultHoldTime)
	{
		if (string.IsNullOrEmpty(message))
			return;

		EnsureInstance();
		if (_instance == null)
			return;

		_instance.ShowInternal(message, duration);
	}

	private static void EnsureInstance()
	{
		if (_instance != null)
			return;

		var go = new GameObject("UiToast");
		DontDestroyOnLoad(go);
		_instance = go.AddComponent<UiToast>();
		_instance.Build();
	}

	private void Build()
	{
		// ── Canvas：覆盖在最上层，缩放按项目本身的 1080x2160 设计分辨率 ──
		_canvasGo = new GameObject("ToastCanvas", typeof(Canvas), typeof(CanvasScaler),
			typeof(GraphicRaycaster));
		_canvasGo.transform.SetParent(transform, false);

		var canvas = _canvasGo.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.overrideSorting = true;
		canvas.sortingOrder = 31000;

		var scaler = _canvasGo.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080f, 2160f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 1f;

		// ── 底板：半透明黑条 ──
		var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer),
			typeof(Image), typeof(CanvasGroup));
		panelGo.transform.SetParent(_canvasGo.transform, false);

		_panel = panelGo.GetComponent<RectTransform>();
		_panel.anchorMin = new Vector2(0.5f, 0.5f);
		_panel.anchorMax = new Vector2(0.5f, 0.5f);
		_panel.pivot = new Vector2(0.5f, 0.5f);
		_panel.anchoredPosition = new Vector2(0f, -260f);
		_panel.sizeDelta = new Vector2(620f, 150f);

		var bg = panelGo.GetComponent<Image>();
		bg.color = new Color(0f, 0f, 0f, 0.7f);
		bg.raycastTarget = false;

		_group = panelGo.GetComponent<CanvasGroup>();
		_group.alpha = 0f;
		_group.interactable = false;
		_group.blocksRaycasts = false;

		// ── 文字：中文字形由动态图集自动生成（项目默认字体就是 DroidSansFallback SDF） ──
		var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
		textGo.transform.SetParent(panelGo.transform, false);

		var textRect = textGo.GetComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = new Vector2(24f, 12f);
		textRect.offsetMax = new Vector2(-24f, -12f);

		_text = textGo.AddComponent<TextMeshProUGUI>();
		_text.font = TMP_Settings.defaultFontAsset;
		_text.text = string.Empty;
		_text.color = Color.white;
		_text.fontStyle = FontStyles.Bold;
		_text.alignment = TextAlignmentOptions.Center;
		_text.enableWordWrapping = false;
		_text.raycastTarget = false;
		_text.enableAutoSizing = true;
		_text.fontSizeMin = 30f;
		_text.fontSizeMax = 56f;

		_canvasGo.SetActive(false);
	}

	private void ShowInternal(string message, float duration)
	{
		if (_text == null || _group == null || _panel == null)
			return;

		_text.text = message;
		_holdTime = duration > 0f ? duration : DefaultHoldTime;
		_elapsed = 0f;
		_running = true;

		_group.alpha = 0f;
		_panel.localScale = new Vector3(0.86f, 0.86f, 1f);
		_canvasGo.SetActive(true);
	}

	private void Update()
	{
		if (!_running)
			return;

		_elapsed += Time.unscaledDeltaTime;

		float alpha;
		if (_elapsed < FadeInTime)
		{
			alpha = _elapsed / FadeInTime;
		}
		else if (_elapsed < FadeInTime + _holdTime)
		{
			alpha = 1f;
		}
		else
		{
			alpha = 1f - (_elapsed - FadeInTime - _holdTime) / FadeOutTime;
		}

		_group.alpha = Mathf.Clamp01(alpha);

		// 轻微放大，做出「弹出来」的手感
		float t = Mathf.Clamp01(_elapsed / (FadeInTime * 2f));
		float scale = Mathf.Lerp(0.86f, 1f, t);
		_panel.localScale = new Vector3(scale, scale, 1f);

		if (_elapsed >= FadeInTime + _holdTime + FadeOutTime)
		{
			_running = false;
			_group.alpha = 0f;
			_canvasGo.SetActive(false);
		}
	}

	private void OnDestroy()
	{
		if (_instance == this)
			_instance = null;
	}
}
