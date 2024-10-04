
using System.Numerics;

public class DebugRotation : Component
{
	[Button]
	public void InverseRotation()
	{		
		WorldRotation = WorldRotation.Inverse;
	}

	[Button]
	public void Flip()
	{
		var backward = Transform.World.Backward;

		WorldRotation = backward.EulerAngles.ToRotation();
	}
}
