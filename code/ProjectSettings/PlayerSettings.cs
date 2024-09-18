
[GameResource("Player Settings", "ps", "Player Settings")]
public class PlayerSettings : ProjectSetting<PlayerSettings>
{
	[Group("Configs"), Property] public CharacterMovementConfig characterMovementConfig { get; set; }
}
