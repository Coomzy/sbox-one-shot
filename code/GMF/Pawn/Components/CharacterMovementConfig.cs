
using System.Text.Json.Serialization;

[Group("GMF")]
[GameResource("Character Movement Config", "cmcfg", "Config for Character Movement")]
public class CharacterMovementConfig : GameResource
{
	[Group("Speed"), Property] public float crouchMoveSpeed { get; set; } = 64.0f;
	[Group("Speed"), Property] public float walkMoveSpeed { get; set; } = 100.0f;
	[Group("Speed"), Property] public float runMoveSpeed { get; set; } = 190.0f;
	[Group("Speed"), Property] public float sprintMoveSpeed { get; set; } = 320.0f;

	[Group("Ducking"), Property] public float characterHeight { get; set; } = 64.0f;
	[Group("Ducking"), Property] public float crouchHeight { get; set; } = 36.0f;

	[Group("Ducking"), Property] public float crouchDuckSpeed { get; set; } = 10.0f;
	[Group("Ducking"), Property] public float slideDuckSpeed { get; set; } = 10.0f;

	[Group("Friction"), Property] public float groundFriction { get; set; } = 6.0f;
	[Group("Friction"), Property] public float airFriction { get; set; } = 0.2f;

	[Group("Sliding"), Property] public float slideTime { get; set; } = 1.5f;
	[Group("Sliding"), Property] public float slideMinVelocity { get; set; } = 50.0f;
	[Group("Sliding"), Property] public float slideStartMinVelocity { get; set; } = 200.0f;
	[Group("Sliding"), Property] public Curve slideFalloffCurve { get; set; } = new Curve(new Curve.Frame(0.0f, 1.0f, 0.0f, -1.0f), new Curve.Frame(1.0f, 0.0f, 1.0f, 0.0f));

	[Group("Jumping"), Property] public float jumpHeight { get; set; } = 300.0f;
	[Group("Jumping"), Property] public float jumpCooldown { get; set; } = 0.3f;
	[Group("Jumping"), Property] public float jumpCoyoteTime { get; set; } = 0.2f;

	[Group("Eye Height"), Property] public float eyeHeight { get; set; } = 64;
	[Group("Eye Height"), Property] public float eyeHeightSliding { get; set; } = 28;
	[Group("Eye Height"), Property] public float eyeHeightCrouching { get; set; } = 28;
	[Group("Eye Height"), Property] public float duckHeightOffset { get; set; } = 36;
	[Group("Eye Height"), Property, JsonIgnore, ReadOnly] public float duckHeight => eyeHeight - duckHeightOffset;
}
