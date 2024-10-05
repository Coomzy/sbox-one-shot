
using System;
using System.Numerics;
using System.Reflection.PortableExecutable;

// TODO: Split this out so there can be a Flatscreen and VR version but the code can still reference Spectator.instance
[Group("GMF")]
public class CameraBoom : Component
{
	[Group("Setup"), Property] public GameObject roller { get; set; }
	[Group("Setup"), Property] public GameObject socket { get; set; }

	[Property] public Rotation initalRotation { get; set; } = Rotation.Identity;
	[Property] public Angles inputAngles = new Angles();

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Test();
	}

	void Test()
	{
		if (PlayerInfo.allAlive.Count < 1)
		{
			return;
		}
		if (!IsFullyValid(PlayerInfo.allAlive[0]?.character?.body))
		{
			return;
		}

		// TODO: this shouldn't choose IT'S spectate target, get told
		var spectateTarget = PlayerInfo.allAlive[0].character.body;

		var targetPos = spectateTarget.WorldPosition;

		var localOffsetBack = WorldRotation.Backward * 125.0f;
		var localOffsetUp = Vector3.Up * 55.0f;
		var localOffset = localOffsetBack + localOffsetUp;

		inputAngles.yaw = 90.0f;

		localOffset *= inputAngles;

		targetPos += localOffset;

		targetPos = MathY.MoveTowards(WorldPosition, targetPos, Time.Delta * 1000.0f);

		var targetRot = spectateTarget.WorldRotation;

		targetRot *= inputAngles.ToRotation();

		WorldPosition = targetPos;
		WorldRotation = targetRot;
		//initalRotation = targetRot;
	}
}
