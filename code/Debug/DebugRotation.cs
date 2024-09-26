
using System.Numerics;

public class DebugRotation : Component
{
	[Button]
	public void InverseRotation()
	{		
		Transform.Rotation = Transform.Rotation.Inverse;
	}

	[Button]
	public void Flip()
	{
		var backward = Transform.World.Backward;

		Transform.Rotation = backward.EulerAngles.ToRotation();
	}
}
