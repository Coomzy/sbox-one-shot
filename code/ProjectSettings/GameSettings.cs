
using Sandbox.Services;

[GameResource("Game Settings", "gs", "Game Settings")]
public class GameSettings : ProjectSetting<GameSettings>
{
	[Group("Configs"), Property] public CharacterMovementConfig characterMovementConfig { get; set; }
	[Group("Configs"), Property] public ProceduralAnimationConfig harpoonProceduralAnimationConfig { get; set; }
	[Group("Configs"), Property] public HarpoonGunConfig harpoonGunConfig { get; set; }

	[Group("Achievements"), Property] public GamePass payToWinGamePass { get; set; }
}
