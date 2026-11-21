using _Content.Data;

namespace _Content.Appmetricas
{
	/// <summary>
	/// 打点事件空实现。
	/// 项目仅保留抖音（StarkSDK）SDK，Yandex AppMetrica 已移除，
	/// 此处保留同名同签名的公开方法，调用方（GameManager / CharacterSkillsManager 等）无需改动。
	/// 若日后要接抖音自有埋点，只需在这里的实现里补上报逻辑。
	/// </summary>
	public class AppMetricaEvents
	{
		public static void SendPurchaseEvent(string inappId, string currency, string price /*, InAppCategory category*/)
		{
		}

		public static void SendRateUs(int rateResult)
		{
		}

		public static void SendLevelStartEvent(int levelNumber, int levelCount, string levelName, int levelLoop)
		{
		}

		public static void SendUpgradeCharacterEvent(int levelNumber, int levelCount, int charLevel, string abilityName, int abilityLevel)
		{
		}

		public static void SendLevelEndEvent(int levelNumber, int levelCount, string levelName, int levelLoop,
			LevelResult levelResult,
			int spentSeconds, int progress)
		{
		}

		public static void SendTutorialStage(int stage)
		{
		}

		public static void SendUnlockLevel(int levelId, string levelName, int levelCount)
		{
		}

		public static void SendTutorialEvent(string name)
		{
		}
	}
}
