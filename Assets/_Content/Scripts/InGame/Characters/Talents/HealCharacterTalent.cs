using UnityEngine;

namespace _Content.InGame.Characters.Talents
{
	[CreateAssetMenu(menuName = "Abilities/Heal")]
	public class HealCharacterTalent: TalentBase
	{
		public override int LevelsCount => 1;
		
		public override string GetLevelDescription(int level)
		{
			return "+ 生命";
		}
	}
}