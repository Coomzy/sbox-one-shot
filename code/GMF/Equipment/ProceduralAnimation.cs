
using Sandbox.Utility;
using System;

public class ProceduralAnimation : Component
{
	[Group("Setup"), Property] public Character owner { get; set; }

	[Group("Runtime"), Order(100), Property] public bool isDownBob { get; set; } = true;
	[Group("Runtime"), Order(100), Property] public float bobRate { get; set; }

	protected override void OnUpdate()
	{
		if (owner == null)
			return;

		Vector3 desiredPos = Vector3.Zero;
		if (owner.movement.isSliding)
		{
			desiredPos = GetSlidingMovement();
		}
		else if (!owner.controller.IsOnGround)
		{
			desiredPos = GetAirMovement();
		}
		else
		{
			desiredPos = GetGroundMovement();
		}

		var ungroundedLerp = MathX.LerpInverse(owner.movement.lastUngrounded, 0.0f, 0.15f);
		if (owner.movement.isGrounded && ungroundedLerp < 1.0f)
		{
			desiredPos.z -= Time.Delta * 30.0f;
			//desiredPos.z = 0.0f;
		}

		//desiredPos = GetAirMovement();
		Transform.LocalPosition = desiredPos;
	}

	public virtual Vector3 GetSlidingMovement()
	{
		Vector3 localPos = Transform.LocalPosition;
		localPos.z = MathY.MoveTowards(localPos.z, 0.0f, Time.Delta * 5.0f);
		return localPos;
	}

	public virtual Vector3 GetAirMovement()
	{
		Vector3 localPos = Transform.LocalPosition;
		var verticalVelocity = owner.controller.Velocity.z;
		//verticalVelocity = owner.characterMovement.velocity.z;
		//verticalVelocity = 100.0f;
		var lerp = MathX.LerpInverse(Math.Abs(verticalVelocity), 0.0f, 500.0f);
		var heightTarget = owner.controller.Velocity;
		var desiredHeight = 3.5f;
		var rate = MathX.Lerp(10, 50f, lerp);
		//rate = 35f;
		if (verticalVelocity > 0.0f)
		{
			desiredHeight = -desiredHeight;
		}

		Gizmo.Draw.ScreenText($"verticalVelocity: {verticalVelocity}", new Vector2(10, 10));
		Gizmo.Draw.ScreenText($"lerp: {lerp}", new Vector2(10, 25));
		Gizmo.Draw.ScreenText($"desiredHeight: {desiredHeight}", new Vector2(10, 40));
		Gizmo.Draw.ScreenText($"rate: {rate}", new Vector2(10, 55));
		//Gizmo.Draw.ScreenText($"velocity: {velocity.Length}", new Vector2(10, 55));

		localPos.z = MathY.MoveTowards(localPos.z, desiredHeight, Time.Delta * rate);
		return localPos;
	}

	public virtual Vector3 GetGroundMovement()
	{
		var velocity = owner.controller.Velocity.WithZ(0);
		var localVelocity = owner.controller.Transform.World.NormalToLocal(velocity);

		var lerp = MathX.LerpInverse(velocity.Length, 0.0f, 320.0f);
		var lerpWalkToSprint = MathX.LerpInverse(velocity.Length, 100.0f, 320.0f);
		var easedLerp = Easing.QuadraticIn(lerpWalkToSprint);

		var bobAmount = 2.5f;
		var bobTarget = MathX.Lerp(0.0f, bobAmount, lerp);
		var desiredBobRate = MathX.Lerp(7.5f, 20.0f, lerpWalkToSprint);
		bobRate = MathY.MoveTowards(bobRate, desiredBobRate, Time.Delta * 75.0f);

		if (isDownBob)
		{
			bobTarget = -bobTarget;
		}

		bool hasExceededTarget = Transform.LocalPosition.z >= bobTarget;

		if (isDownBob)
		{
			hasExceededTarget = Transform.LocalPosition.z <= bobTarget;
		}

		if (hasExceededTarget)
		{
			isDownBob = !isDownBob;
		}

		if (velocity.IsNearZeroLength)
		{
			isDownBob = false;
		}

		Vector3 localPos = Transform.LocalPosition;
		localPos.z = MathY.MoveTowards(localPos.z, bobTarget, Time.Delta * bobRate);
		return localPos;
	}
}
