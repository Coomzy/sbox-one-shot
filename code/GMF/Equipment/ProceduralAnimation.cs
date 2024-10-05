
using Sandbox.Utility;
using System;
using System.Runtime.CompilerServices;

[GameResource("ProceduralAnimationConfig", "pac", "ProceduralAnimationConfig")]
public class ProceduralAnimationConfig : GameResource
{
	[Group("Ground"), Property] public float groundBobAmount { get; set; } = 2.5f;
	[Group("Ground"), Property] public Vector2 groundBobRateRange { get; set; } = new Vector2(7.5f, 20.0f);
	[Group("Ground"), Property] public float groundBobMoveTowardsRate { get; set; } = 75.0f;
	[Group("Ground"), Property] public Vector2 groundVelocityRange { get; set; } = new Vector2(0.0f, 320.0f);
	[Group("Ground"), Property] public Vector2 groundVelocityWalkToSprintRange { get; set; } = new Vector2(100.0f, 320.0f);

	[Group("Mantle"), Property] public float mantleCenterZRate { get; set; } = 5.0f;
	[Group("Mantle"), Property] public float mantleMaxGunLower { get; set; } = 10.0f;
	[Group("Mantle"), Property] public Curve mantlePitchCurve { get; set; } = new Curve(new Curve.Frame(0.0f, 0.0f, 0.0f, 1.0f), new Curve.Frame(0.5f, 1.0f, -1.0f, -1.0f), new Curve.Frame(1.0f, 0.0f, 1.0f, 0.0f));

	[Group("Sliding"), Property] public float slidingCenterZRate { get; set; } = 5.0f;

	[Group("Air"), Property] public float airVerticalMoveCap { get; set; } = 500.0f;
	[Group("Air"), Property] public float airMaxZ { get; set; } = 3.5f;
	[Group("Air"), Property] public Vector2 airMoveRateRange { get; set; } = new Vector2(10f, 50f);

	[Group("Ground Impact"), Property] public float ungroundedBumpTime { get; set; } = 0.15f;
	[Group("Ground Impact"), Property] public float ungroundedBumpRate { get; set; } = 30.0f;
}

public class ProceduralAnimation : Component
{
	[Group("Setup"), Property] public Equipment equipment { get; set; }
	[Group("Setup"), Property] public ProceduralAnimationConfig config { get; set; }

	[Group("Runtime"), Order(100), Property] public bool isDownBob { get; set; } = true;
	[Group("Runtime"), Order(100), Property] public float bobRate { get; set; }

	[Group("Runtime"), Order(100), Property] public Angles lastAnglesInput = new Angles();
	[Group("Runtime"), Order(100), Property] public Angles lastAngles = new Angles();

	public Character owner => equipment?.instigator;

	protected override void OnStart()
	{
		config = config ?? new ProceduralAnimationConfig();
	}

	// TODO: I think ideally, everything would take a ref of desiredPos/desiredAngles and adjust it as needed
	protected override void OnUpdate()
	{
		if (owner == null)
			return;

		Vector3 desiredPos = Vector3.Zero;
		Angles desiredAngles = Angles.Zero;

		if (owner.movement.isMantling)
		{
			desiredPos = GetMantlingMovement();
			desiredAngles = GetMantlingRotation();
		}
		else if (owner.movement.isSliding)
		{
			desiredPos = GetSlidingMovement();
		}
		else if (!owner.movement.isGrounded)
		{
			desiredPos = GetAirMovement();
		}
		else
		{
			desiredPos = GetGroundMovement();
		}

		ApplyRecoil(ref desiredPos, ref desiredAngles);

		if (!owner.movement.isMantling)
		{
			desiredAngles = GetSway();

			DoGroundBump(ref desiredPos);
		}

		LocalPosition = desiredPos;
		LocalRotation = desiredAngles.ToRotation();
	}

	public virtual Vector3 GetMantlingMovement()
	{
		Vector3 localPos = LocalPosition;
		localPos.z = MathY.MoveTowards(localPos.z, 0.0f, Time.Delta * config.mantleCenterZRate);
		return localPos;
	}

	public virtual Angles GetMantlingRotation()
	{
		var mantleLerp = owner.movement.timeToFinishMantle.Fraction;
		var mantleLerpCurved = config.mantlePitchCurve.Evaluate(mantleLerp);

		Angles localAngles = LocalRotation.Angles();
		float gunLowerTarget = config.mantleMaxGunLower * mantleLerpCurved;

		localAngles.pitch = gunLowerTarget;
		//localAngles.pitch = MathY.MoveTowards(localAngles.pitch, gunLowerTarget, Time.Delta * 5.0f);
		return localAngles;
	}

	public virtual Vector3 GetSlidingMovement()
	{
		Vector3 localPos = LocalPosition;
		localPos.z = MathY.MoveTowards(localPos.z, 0.0f, Time.Delta * config.slidingCenterZRate);
		return localPos;
	}

	public virtual Vector3 GetAirMovement()
	{
		Vector3 localPos = LocalPosition;
		var verticalVelocity = owner.controller.Velocity.z;
		var lerp = MathX.LerpInverse(Math.Abs(verticalVelocity), 0.0f, config.airVerticalMoveCap);
		var heightTarget = owner.controller.Velocity;
		var desiredHeight = config.airMaxZ;
		var rate = config.airMoveRateRange.Lerp(lerp);
		if (verticalVelocity > 0.0f)
		{
			desiredHeight = -desiredHeight;
		}

		/*Debuggin.ToScreen($"verticalVelocity: {verticalVelocity}");
		Debuggin.ToScreen($"lerp: {lerp}");
		Debuggin.ToScreen($"desiredHeight: {desiredHeight}");
		Debuggin.ToScreen($"rate: {rate}");*/

		localPos.z = MathY.MoveTowards(localPos.z, desiredHeight, Time.Delta * rate);
		return localPos;
	}

	public virtual Vector3 GetGroundMovement()
	{
		Vector3 localPos = LocalPosition;
		var velocityHorizontal = owner.controller.Velocity.WithZ(0);

		var movementSpeedLerp = config.groundVelocityRange.InverseLerp(velocityHorizontal.Length);
		var walkToSprintLerp = config.groundVelocityWalkToSprintRange.InverseLerp(velocityHorizontal.Length);

		var bobAmount = config.groundBobAmount;
		var bobTarget = MathX.Lerp(0.25f, bobAmount, movementSpeedLerp);
		var desiredBobRate = config.groundBobRateRange.Lerp(walkToSprintLerp);
		bobRate = MathY.MoveTowards(bobRate, desiredBobRate, Time.Delta * config.groundBobMoveTowardsRate);

		if (isDownBob)
		{
			bobTarget = -bobTarget;
		}

		// TODO: FIX THIS
		//bool hasExceededTarget = isDownBob ? localPos.z <= bobTarget : localPos.z >= bobTarget;

		//hasExceededTarget |= MathX.AlmostEqual(localPos.z, bobTarget);

		bool hasExceededTarget = localPos.z >= bobTarget;
		if (isDownBob)
		{
			hasExceededTarget = localPos.z <= bobTarget;
		}

		if (!hasExceededTarget && MathX.AlmostEqual(localPos.z, bobTarget))
		{
			hasExceededTarget = true;
		}

		if (hasExceededTarget)
		{
			isDownBob = !isDownBob;
		}

		/*Debuggin.ToScreen($"bobTarget: {bobTarget}");
		Debuggin.ToScreen($"desiredBobRate: {desiredBobRate}");
		Debuggin.ToScreen($"bobRate: {bobRate}"); */

		localPos.z = MathY.MoveTowards(localPos.z, bobTarget, Time.Delta * bobRate);
		//Debuggin.ToScreen($"localPos.z: {localPos.z}");
		return localPos;
	}

	public virtual void ApplyRecoil(ref Vector3 desiredPos, ref Angles desiredAngles)
	{
		if (!IsFullyValid(owner.equippedItem))
			return;

		var lastFire = owner.equippedItem.lastFire;

		var recoilPct = MathY.InverseLerp(0.15f, 0.0f, lastFire);

		float amount = 10.0f;

		desiredPos.x = -amount * recoilPct;
	}

	public virtual Angles GetSway()
	{
		var inputScalar = 3.5f;
		var maxInputRange = 3.5f;
		var inputSmoothRate = 5.0f;
		var smoothRate = 70.0f;
		var deltaTime = MathY.Min(Time.Delta, 60.0f / 1.0f);

		var input = Input.AnalogLook * inputScalar;
		input.yaw = MathY.Min(input.yaw, maxInputRange);
		input.pitch = MathY.Min(input.pitch, maxInputRange);

		lastAnglesInput = Angles.Lerp(lastAnglesInput, input, Time.Delta * inputSmoothRate);
		lastAngles = Angles.Lerp(lastAngles, lastAnglesInput, Time.Delta * smoothRate);

		return lastAngles;
	}

	public virtual void DoGroundBump(ref Vector3 desiredPos)
	{
		var ungroundedLerp = MathX.LerpInverse(owner.movement.lastUngrounded, 0.0f, config.ungroundedBumpTime);
		if (owner.movement.isGrounded && ungroundedLerp < 1.0f)
		{
			desiredPos.z -= Time.Delta * config.ungroundedBumpRate;
		}
	}
}
