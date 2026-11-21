using System;
using System.Collections.Generic;
using System.Linq;
using _Content.Data;
using _Content.Events;
using _Content.InGame.Characters;
using _Content.InGame.Characters.Abilities;
using _Content.InGame.GameDrop;
using _Content.InGame.Levels;
using Base;
using MoreMountains.Feedbacks;
using NaughtyAttributes;
using UnityEngine;
using EventHandler = Opsive.Shared.Events.EventHandler;

namespace _Content.InGame.Managers
{
	public class GameplayManager : Singleton<GameplayManager>
	{
		[SerializeField] [Tag] private string _spawnPointTag;
		[SerializeField] private float _arenaSize = 50f;
		[SerializeField] private Transform _arenaCenter;
		[SerializeField] private List<int> _expToNextLevels;
		[SerializeField] private Character _characterPrefab;
		[SerializeField] private MMF_Player _onLevelCompleteFeedback;
		[Tooltip("每局最多复活几次。0 = 不限次数（只要有广告就能复活）")]
		[SerializeField] private int _maxRevivePerRun = 0;

		public bool CharacterRevived => _characterRevived;
		public float ArenaSize => _arenaSize;

		public Vector3 ArenaCenter
		{
			get
			{
				if (_arenaCenter != null)
					return _arenaCenter.position;
				return Vector3.zero;
			}
		}

		private bool _gameStarted = false;
		private float _gameTimer = 0f;
		private Character _character;
		private CharacterExperienceAbility _expAbility;
		private bool _characterRevived;
		private int _spawnPointIndex;

		// ── 复活次数（看激励广告攒，死亡时直接扣） ──
		private const string ReviveChancesKey = "ReviveChances";
		public const int MaxReviveChances = 5;
		private int _reviveChances;   // 库存：看广告攒下的复活次数
		private int _reviveCount;     // 本局已经复活过几次

		public bool GameStarted => _gameStarted;

		/// <summary>库存复活次数。</summary>
		public int ReviveChances => _reviveChances;

		/// <summary>本局已复活次数。</summary>
		public int ReviveCount => _reviveCount;

		/// <summary>本局是否还能复活。0 = 不限次数（次数用完就看广告复活）。</summary>
		public bool CanReviveThisRun => _maxRevivePerRun <= 0 || _reviveCount < _maxRevivePerRun;

		public float GameTimer => _gameTimer;
		public Character Character => _character;

		protected override void OnAwake()
		{
			base.OnAwake();
			_onLevelCompleteFeedback?.Initialization(gameObject);
			_reviveChances = Mathf.Clamp(PlayerPrefs.GetInt(ReviveChancesKey, 0), 0, MaxReviveChances);
		}

		public void InitializeLevel(LevelInfo prevLevelInfo, Action onLevelInitialized)
		{
			var spawnPoints = GameObject.FindGameObjectsWithTag(_spawnPointTag);
			var charStartPosition = Vector3.zero;
			if (spawnPoints != null && spawnPoints.Length > 0)
			{
				if (_spawnPointIndex >= spawnPoints.Length)
					_spawnPointIndex = 0;

				charStartPosition = spawnPoints[_spawnPointIndex].transform.position;
				_spawnPointIndex++;
				/*if (spawnPoints.Length == 1)
					charStartPosition = spawnPoints[0].transform.position;
				else
					charStartPosition = spawnPoints.GetRandomElement().transform.position;*/
			}

			_character = Instantiate(_characterPrefab, charStartPosition, Quaternion.identity);

			if (_character != null)
				_expAbility = _character.FindAbility<CharacterExperienceAbility>();

			var arenaCenterGo = GameObject.FindGameObjectWithTag("ArenaCenter");
			if (arenaCenterGo != null)
			{
				_arenaCenter = arenaCenterGo.transform;
			}

			_gameStarted = false;
			PlayerData.Instance.CurrentCharacterLevel = 1;
			PlayerData.Instance.CurrentCharacterExperience = 0;
			EventHandler.ExecuteEvent(InGameEvents.CharacterExperienceChanged,
				PlayerData.Instance.CurrentCharacterExperience);
			EventHandler.ExecuteEvent(InGameEvents.CharacterLevelChanged,
				PlayerData.Instance.CurrentCharacterLevel);

			EnemyManager.Instance.Initialize(prevLevelInfo);
			onLevelInitialized();
		}

		public void Deinitialize()
		{
			_gameStarted = false;
			var drop = FindObjectsOfType<ExpGem>();
			foreach (var expGem in drop)
			{
				Destroy(expGem.gameObject);
			}

			Destroy(_character.gameObject);
			EnemyManager.Instance.Deinitialize();
		}

		private void Update()
		{
			if (GameStarted)
			{
				_gameTimer += Time.deltaTime * GameManager.Instance.TimeScale;
			}
		}

		public void AddExperience(int exp)
		{
			exp = _expAbility.ProcessExperience(exp);
			var expToNewLevel = GetExpToNextLevel();
			var newExp = PlayerData.Instance.CurrentCharacterExperience + exp;
			if (newExp >= expToNewLevel)
			{
				var newLevels = newExp / expToNewLevel;
				newExp = newExp % expToNewLevel;
				PlayerData.Instance.SetExperience(newExp);
				PlayerData.Instance.SetCharacterLevel(PlayerData.Instance.CurrentCharacterLevel + newLevels);
				EventHandler.ExecuteEvent(InGameEvents.CharacterExperienceChanged,
					PlayerData.Instance.CurrentCharacterExperience);
				EventHandler.ExecuteEvent(InGameEvents.CharacterLevelChanged,
					PlayerData.Instance.CurrentCharacterLevel);
				if (CharacterSkillsManager.Instance.HasSkillToUpgrade())
					GameManager.Instance.ShowNewLevelAbilitiesUi(newLevels);
			}
			else
			{
				PlayerData.Instance.SetExperience(newExp);
				EventHandler.ExecuteEvent(InGameEvents.CharacterExperienceChanged,
					PlayerData.Instance.CurrentCharacterExperience);
			}
		}

		public int GetExpToNextLevel()
		{
			var index = PlayerData.Instance.CurrentCharacterLevel - 1;
			if (index >= _expToNextLevels.Count)
				index = _expToNextLevels.Count - 1;

			return _expToNextLevels.ElementAt(index);
		}

		public void OnSauceCollect()
		{
			PlayerData.Instance.AddSauce(1);
			EventHandler.ExecuteEvent(InGameEvents.SauceAmountChanged);
		}

		public void StartGame()
		{
			GameManager.Instance.StartMap();
			_gameTimer = 0f;
			_characterRevived = false;
			_reviveCount = 0;
			_gameStarted = true;
			EventHandler.ExecuteEvent(InGameEvents.GameStart);
		}

		public int GetCurrentLevelReward()
		{
			return GameManager.Instance.GetProgressPerWin();
		}

		public void CompleteLevel()
		{
			_gameStarted = false;
			_onLevelCompleteFeedback?.PlayFeedbacks();
			
			GameManager.Instance.ShowRewardUi();
		}

		public void ReviveCharacter()
		{
			_characterRevived = true;
			_reviveCount++;
			Character?.Revive();
		}

		/// <summary>
		/// 看激励广告成功后调用：库存复活次数 +1（上限 MaxReviveChances）。
		/// </summary>
		/// <returns>true = 真的加了 1 次；false = 已经到上限，没加</returns>
		public bool AddReviveChance()
		{
			if (_reviveChances >= MaxReviveChances)
			{
				Debug.Log($"[复活] 复活次数已满（{MaxReviveChances} 次），不再增加");
				return false;
			}

			_reviveChances++;
			PlayerPrefs.SetInt(ReviveChancesKey, _reviveChances);
			PlayerPrefs.Save();
			Debug.Log($"[复活] 看广告获得 1 次复活机会，当前库存 {_reviveChances} 次");
			return true;
		}

		/// <summary>
		/// 死亡复活时尝试扣一次库存。扣成功了就免广告直接复活。
		/// </summary>
		public bool TryConsumeReviveChance()
		{
			if (_reviveChances <= 0)
				return false;

			_reviveChances--;
			PlayerPrefs.SetInt(ReviveChancesKey, _reviveChances);
			PlayerPrefs.Save();
			Debug.Log($"[复活] 消耗 1 次库存复活，剩余 {_reviveChances} 次");
			return true;
		}

		public void WinLevelCheat()
		{
			if (!GameStarted)
				return;
			
			CompleteLevel();
		}
	}
}