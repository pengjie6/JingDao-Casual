using _Content.InGame.Managers;
using Common.UI;
using UnityEngine;

namespace _Content.InGame.UI.Misc
{
	public class SettingsButton: UIViewWrapper
	{
		public void OnSettingsButtonDown()
		{
			PlayLobbyInterstitial();
			GameManager.Instance.ShowSettings();
		}

		/// <summary>
		/// 只在「大厅/主菜单」点设置时顺便插一次广告；对局中点设置不插，避免打断玩家。
		/// 插屏本身有「广告模块启动 15 秒内 / 两次间隔 30 秒 / 实例还没加载完」三重限制，
		/// 播不出去是常态，所以失败了就静默跳过，绝不能让设置面板弹不出来。
		/// </summary>
		private static void PlayLobbyInterstitial()
		{
#if UNITY_EDITOR
			// 编辑器里 TT 接口是 mock（不会真的出广告），也不需要验证这个流程
			return;
#else
			// GameStarted == true 说明已经进对局了，跳过
			if (GameplayManager.Instance != null && GameplayManager.Instance.GameStarted)
				return;

			if (AdManager.Instance == null)
			{
				Debug.LogWarning("[广告] 场景里没有 AdManager，跳过设置页插屏");
				return;
			}

			AdManager.Instance.ShowInterstitialAd(null,
				reason => Debug.LogWarning($"[广告] 设置页插屏未展示：{reason}"));
#endif
		}
	}
}
