using _Content.Data;
using _Content.InGame.Managers;
using Common.UI;
using TMPro;
using UnityEngine;

namespace _Content.InGame.UI.Main
{
	public class DefeatUi: UIViewWrapper
	{
		[SerializeField] private TextMeshProUGUI _enemiesCount;
		[SerializeField] private TextMeshProUGUI _characterLevel;
		[SerializeField] private GameObject _reviveButton;
		[SerializeField] private GameObject _restartButton;

		/// <summary>本次复活是否已经处理过，防止「关闭回调 + 兜底计时器」重复复活。</summary>
		private bool _reviveHandled;

		public override void ShowView(bool force = false)
		{
			base.ShowView(force);
			UpdateInformation();
		}

		private void UpdateInformation()
		{
			_characterLevel.text = PlayerData.Instance.CurrentCharacterLevel.ToString();
			_enemiesCount.text = EnemyManager.Instance.GetDefeatedEnemyCount().ToString();
			_reviveButton.SetActive(GameplayManager.Instance.CanReviveThisRun);
		}

		/// <summary>
		/// 死亡界面的「复活」按钮。
		/// 有库存次数：直接扣一次，免广告复活；
		/// 没库存：播插屏广告，广告关闭后复活。
		///
		/// 插屏和激励不一样：不保证看完，而且可能因为「模块启动 15 秒内 / 两次插屏间隔不足 30 秒 /
		/// 实例还没加载完」压根播不出去。所以这里的策略是「尝试过就复活」，
		/// 广告没播出来也不能把玩家卡死在死亡界面。
		/// </summary>
		public void OnRevive()
		{
			var gameplay = GameplayManager.Instance;
			if (!gameplay.CanReviveThisRun)
				return;

			// 有库存：直接从次数里扣，不看广告
			if (gameplay.TryConsumeReviveChance())
			{
				GameManager.Instance.RevivePlayer();
				return;
			}

			// 没库存：播插屏复活。先隐藏按钮防连点
			_reviveButton.SetActive(false);

#if UNITY_EDITOR
			// 编辑器里不播广告，直接复活方便调试
			GameManager.Instance.RevivePlayer();
#else
			if (AdManager.Instance == null)
			{
				Debug.LogError("[广告] 场景里没有 AdManager，复活插屏跳过，直接复活");
				ReviveOnce();
				return;
			}

			_reviveHandled = false;
			bool shown = AdManager.Instance.ShowInterstitialAd(OnReviveAdClosed, OnReviveAdFailed);

			if (!shown)
			{
				// 15 秒保护期 / 30 秒间隔 / 未加载完成 —— 广告没播出去，照样让玩家复活
				Debug.LogWarning("[广告] 复活插屏未能展示，本次直接复活");
				ReviveOnce();
				return;
			}

			// 兜底：万一平台没回调 OnClose，10 秒后按已复活处理，避免按钮永久消失
			CancelInvoke(nameof(ForceReviveFallback));
			Invoke(nameof(ForceReviveFallback), 10f);
#endif
		}

		/// <summary>插屏广告关闭：复活。</summary>
		private void OnReviveAdClosed()
		{
			ReviveOnce();
		}

		/// <summary>插屏广告没能展示（理论上走不到，展示前已经被返回值拦掉了）。</summary>
		private void OnReviveAdFailed(string reason)
		{
			Debug.LogWarning($"[广告] 复活插屏未展示：{reason}");
			ReviveOnce();
		}

		/// <summary>兜底：广告一直不回调时强制复活。</summary>
		private void ForceReviveFallback()
		{
			Debug.LogWarning("[广告] 复活插屏 10 秒内没有关闭回调，强制复活");
			ReviveOnce();
		}

		/// <summary>复活只执行一次，后续回调全部忽略。</summary>
		private void ReviveOnce()
		{
			if (_reviveHandled)
				return;

			_reviveHandled = true;
			CancelInvoke(nameof(ForceReviveFallback));
			GameManager.Instance.RevivePlayer();
		}

		public void OnRestart()
		{
			// 取消可能还挂着的复活兜底计时器，避免重开一局后被误复活
			CancelInvoke(nameof(ForceReviveFallback));
			_reviveHandled = true;
			GameManager.Instance.RestartAfterDefeat();
		}
	}
}
