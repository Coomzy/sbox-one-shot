
public struct DamageInfo
{
	public PlayerInfo instigator { get; set; }
	public Component damageCauser { get; set; }
	public int hitBodyIndex { get; set; }
	public Vector3 hitVelocity { get; set; }
}
