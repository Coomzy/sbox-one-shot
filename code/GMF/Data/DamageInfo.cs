
public class DamageInfo
{
	public PlayerInfo instigator { get; set; }
	public Component damageCauser { get; set; }
	public PhysicsBody hitBody { get; set; }
	public Vector3 hitVelocity { get; set; }
}
