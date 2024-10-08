
using Sandbox;
using Sandbox.ModelEditor;
using Sandbox.Utility;
using System;
using System.ComponentModel.DataAnnotations;
using static Sandbox.VertexLayout;

[Group("GMF")]
public class CharacterMovement : Component
{
	[Group("Setup"), Property] public Character owner { get; set; }
	[Group("Setup"), Property] public CharacterController characterController { get; set; }
	[Group("Setup"), Property] public CharacterMovementConfig config { get; set; }

	[Group("Runtime"), Property, Sync] public bool isSliding { get; set; }
	[Group("Runtime"), Property, Sync] public bool isMantling { get; set; }
	[Group("Runtime"), Property, Sync] public bool isCrouching { get; set; }
	[Group("Runtime"), Property, Sync] public Vector3 wishVelocity { get; set; }
	[Group("Runtime"), Property, Sync] public Vector3 slideVelocity { get; set; }
	[Group("Runtime"), Property] public float slideVelocityReduction { get; set; }

	[Group("Runtime"), Property] public bool wishCrouch { get; set; }
	[Group("Runtime"), Property] public float eyeHeight { get; set; }

	[Group("Runtime"), Property] public TimeSince lastMantleInput { get; set; }
	[Group("Runtime"), Property] public TimeUntil timeToFinishMantle { get; set; }
	[Group("Runtime"), Property] public Vector3 mantleStart { get; set; }
	[Group("Runtime"), Property] public Vector3 mantleEnd { get; set; }

	[Group("Runtime"), Property, Sync] public bool isGrounded { get; set; }
	[Group("Runtime"), Property] public TimeSince lastGrounded { get; set; }
	[Group("Runtime"), Property] public TimeSince lastUngrounded { get; set; }
	[Group("Runtime"), Property] public TimeSince lastMantleEnd { get; set; }

	[Group("Runtime"), Property] public TimeSince lastJump { get; set; }
	[Group("Runtime"), Property] public TimeSince slideStart { get; set; }

	[Group("Runtime"), Property] public Vector3 lastVelocity { get; set; }

	[Group("Runtime"), Property] public float heighestSlideVel { get; set; } = 0.0f;

	[ConVar] public static bool debug_character_movement { get; set; }
	public static bool cheat_remove_slide_vel_cap { get; set; }

	float CurrentMoveSpeed
	{
		get
		{
			if (isCrouching) return config.crouchMoveSpeed;
			if (Input.Down(Inputs.run)) return config.sprintMoveSpeed;
			if (Input.Down(Inputs.walk)) return config.walkMoveSpeed;

			return config.runMoveSpeed;
		}
	}

	protected override void OnStart()
	{
		config = config ?? new CharacterMovementConfig();

		eyeHeight = config.eyeHeight;

		isGrounded = true;
		isSliding = false;
		isMantling = false;
		isCrouching = false;
	}

	protected override void OnUpdate()
	//public void Update()
	{
		if (IsProxy)
			return;

		if (debug_character_movement)
		{
			var vel = characterController.Velocity.WithZ(0).Length;
			vel = characterController.Velocity.Length;
			if (isSliding)
			{
				if (heighestSlideVel < vel)
				{
					heighestSlideVel = vel;
				}
			}
			Debuggin.ToScreen($"characterController.Velocity: {vel}");
			Debuggin.ToScreen($"heighestSlideVel: {heighestSlideVel}");
		}
	}

	//protected override void OnFixedUpdate()
	public void Update()
	{
		if (IsProxy)
			return;

		if (GameMode.instance.modeState == ModeState.ReadyPhase)
		{
			return;
		}

		if (isMantling)
		{
			MantleUpdate();
			return;
		}

		SlideInput();

		if (isSliding)
		{
			SlideUpdate();
		}
		else
		{
			MantlingInput();
			CrouchingInput();
			MovementInput();
		}

		UpdateGrounding();
		SmoothMoveBodyFromCrouchJumpOffset();
		//GetMantleSpot(out var mantleEndPoint);
		lastVelocity = characterController.Velocity;
	}	

	float GetFriction()
	{
		if (characterController.IsOnGround)
			return config.groundFriction;

		return config.airFriction;
	}

	void SlideUpdate()
	{
		float slideSpeedScalar = MathX.LerpInverse(slideStart, 0.0f, config.slideTime);
		slideSpeedScalar = config.slideFalloffCurve.Evaluate(slideSpeedScalar);

		if (slideSpeedScalar > 1.0f)
		{
			var slideSpeedScalarBonus = slideSpeedScalar - 1.0f;
			slideSpeedScalarBonus = slideSpeedScalarBonus * slideVelocityReduction;
			slideSpeedScalar = slideSpeedScalar - slideSpeedScalarBonus;
		}

		Vector3 curSlideVelocity = slideVelocity * slideSpeedScalar;

		characterController.Velocity = curSlideVelocity;

		if (CanJump() && Input.Pressed(Inputs.jump))
		{
			lastJump = 0;
			characterController.Punch(Vector3.Up * (config.jumpHeight * slideSpeedScalar));
			owner.body.Jump();
			isSliding = false;
		}

		characterController.Move();

		if (curSlideVelocity.Length <= config.slideMinVelocity)
		{
			isSliding = false;
		}

		if (!characterController.IsOnGround && lastGrounded > config.jumpCoyoteTime)
		{
			isSliding = false;
		}
	}

	void MantleUpdate()
	{
		isMantling = true;
		var horizontalLerp = Easing.QuadraticIn(timeToFinishMantle.Fraction);
		var verticalLerp = Easing.QuadraticOut(timeToFinishMantle.Fraction);

		horizontalLerp = config.mantleHorizontalCurve.Evaluate(timeToFinishMantle.Fraction);
		verticalLerp = config.mantleVerticalCurve.Evaluate(timeToFinishMantle.Fraction);

		var newMantleVerticalPosition = Vector3.Lerp(mantleStart, mantleEnd, verticalLerp);
		var newMantlePosition = Vector3.Lerp(mantleStart, mantleEnd, horizontalLerp);
		newMantlePosition.z = newMantleVerticalPosition.z;

		//GameObject.WorldPosition = newMantlePosition;
		characterController.MoveTo(newMantlePosition, true);

		if (timeToFinishMantle)
		{
			isMantling = false;
			lastMantleEnd = 0;
		}
	}

	void MantlingInput()
	{
		if (isSliding || isCrouching)
			return;

		if (isGrounded)
			return;

		if (Input.Pressed(Inputs.jump))
		{
			lastMantleInput = 0;
		}

		if (lastMantleInput > config.mantleInputBuffer)
		{
			return;
		}

		if (!GetMantleSpot(out var mantleEndPoint))
		{
			return;
		}

		isMantling = true;
		characterController.Velocity = Vector3.Zero;
		mantleStart = WorldPosition;
		mantleEnd = mantleEndPoint;

		float mantleDistance = Vector3.DistanceBetween(mantleStart, mantleEnd);
		timeToFinishMantle = config.mantleMoveDistanceRemap.Evaluate(mantleDistance);
	}

	// TODO: Lots of magic numbers in here
	bool debugMantle = false;
	bool GetMantleSpot(out Vector3 mantleEndPoint)
	{
		mantleEndPoint = WorldPosition;

		float radius = characterController.Radius * 0.8f;
		var capsule = Capsule.FromHeightAndRadius(5.0f, radius);
		var trace = Scene.Trace.Capsule(capsule).IgnoreGameObjectHierarchy(GameObject).WithoutTags("ragdoll", "local");

		// Ground Buffer Check
		float maxHeightFromGroundBuffer = 20.0f;

		var groundBufferStart = GameObject.WorldPosition;
		var groundBufferEnd = groundBufferStart - (GameObject.Transform.World.Up * maxHeightFromGroundBuffer);
		var groundBufferResult = trace.FromTo(groundBufferStart, groundBufferEnd).Run();

		float groundBuffer = maxHeightFromGroundBuffer;

		if (groundBufferResult.Hit)
		{
			var groundBufferPoint = groundBufferResult.HitPosition - (Vector3.Up * radius);
			groundBuffer = Vector3.DistanceBetween(groundBufferStart, groundBufferPoint);

			if (debugMantle)
			{
				Debuggin.draw.Sphere(groundBufferPoint, radius, 8, Game.ActiveScene.FixedDelta, Color.Yellow);
			}
		}
		float groundBufferDelta = maxHeightFromGroundBuffer - groundBuffer;
		if (debugMantle)
		{
			Debuggin.draw.Capsule(groundBufferStart, groundBufferEnd, radius, Game.ActiveScene.FixedDelta, groundBufferResult.Hit ? Color.Red : Color.Green);
		}

		// Above Character Check
		float aboveHeightCheckDistance = 50.0f;

		var upCheckStart = GameObject.WorldPosition + (GameObject.Transform.World.Up * characterController.Height);
		var upCheckEnd = upCheckStart + (GameObject.Transform.World.Up * aboveHeightCheckDistance);
		var upCheckResult = trace.FromTo(upCheckStart, upCheckEnd).Run();
		if (debugMantle)
		{
			Debuggin.draw.Capsule(upCheckStart, upCheckEnd, radius, Game.ActiveScene.FixedDelta, upCheckResult.Hit ? Color.Red : Color.Green);
		}

		// Forward Check
		float forwardCheckDistance = 40.0f;

		var forwardCheckStart = upCheckEnd;
		var forwardCheckEnd = forwardCheckStart + (GameObject.Transform.World.Forward * forwardCheckDistance);
		var forwardCheckResult = trace.FromTo(forwardCheckStart, forwardCheckEnd).Run();
		if (debugMantle)
		{
			Debuggin.draw.Capsule(forwardCheckStart, forwardCheckEnd, radius, Game.ActiveScene.FixedDelta, forwardCheckResult.Hit ? Color.Red : Color.Green);
		}

		if (forwardCheckResult.Hit)
		{
			return false;
		}

		// Down Check
		float downCheckDistance = 95.0f - groundBufferDelta;

		var downCheckStart = forwardCheckEnd;
		var downCheckEnd = downCheckStart - (GameObject.Transform.World.Up * downCheckDistance);
		var downCheckResult = trace.FromTo(downCheckStart, downCheckEnd).Run();
		if (debugMantle)
		{
			Debuggin.draw.Capsule(downCheckStart, downCheckEnd, radius, Game.ActiveScene.FixedDelta, downCheckResult.Hit ? Color.Red : Color.Green);
		}


		if (downCheckResult.Hit)
		{
			var mantlePoint = downCheckResult.HitPosition - (Vector3.Up * radius);
			mantleEndPoint = mantlePoint;
			if (debugMantle)
			{
				Debuggin.draw.Sphere(mantlePoint, radius, 8, Game.ActiveScene.FixedDelta, Color.Yellow);
			}

			if (IsMetal(downCheckResult.Surface))
			{
				//BroadcastSound(downCheckResult.Surface.Sounds.ImpactSoft, downCheckResult.HitPosition, config.soundOverrideForMetalImpact);
			}
			else
			{
				//BroadcastSound(downCheckResult.Surface.Sounds.ImpactSoft, downCheckResult.HitPosition);
			}
		}

		return downCheckResult.Hit;
	}

	public bool IsMetal(Surface surface)
	{
		if (surface.ResourceName == "metal" || surface.ResourceName == "metal.sheet" || surface.ResourceName == "metal.weapon")
			return true;

		return false;
	}

	[Broadcast]
	public void BroadcastGroundSlamSound(string sound, Vector3 position, float? volume = null)
	{
		var handle = Sound.Play(sound, position);
		if (volume.HasValue)
		{
			handle.Volume = volume.Value;
			handle.SetParent(GameObject);
			handle.Position = position;
		}
	}

	[Broadcast]
	public void BroadcastSound(string sound, Vector3 position, float? volume = null)
	{
		var handle = Sound.Play(sound, position);
		if (volume.HasValue)
		{
			handle.Volume = volume.Value;
		}
	}

	protected virtual void MovementInput()
	{
		if (characterController is null)
			return;

		var cc = characterController;

		Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		wishVelocity = Input.AnalogMove;

		if (CanJump() && Input.Pressed(Inputs.jump))
		{
			lastJump = 0;
			cc.Punch(Vector3.Up * config.jumpHeight);
			owner.body.Jump();
		}

		if (!wishVelocity.IsNearlyZero())
		{
			wishVelocity = new Angles(0, owner.WorldRotation.Yaw(), 0).ToRotation() * wishVelocity;
			wishVelocity = wishVelocity.WithZ(0);
			wishVelocity = wishVelocity.ClampLength(1);
			wishVelocity *= CurrentMoveSpeed;

			if (!cc.IsOnGround)
			{
				wishVelocity = wishVelocity.ClampLength(50);
				//wishVelocity = wishVelocity.ClampLength(75);
			}
		}

		cc.ApplyFriction(GetFriction());

		if (cc.IsOnGround)
		{
			cc.Accelerate(wishVelocity);
			cc.Velocity = characterController.Velocity.WithZ(0);
		}
		else
		{
			cc.Velocity += halfGravity;
			cc.Accelerate(wishVelocity);

		}

		// Don't walk through other players, let them push you out of the way
		var pushVelocity = PlayerPusher.GetPushVector(WorldPosition + Vector3.Up * 40.0f, Scene, GameObject);
		if (!pushVelocity.IsNearlyZero())
		{
			var travelDot = cc.Velocity.Dot(pushVelocity.Normal);
			if (travelDot < 0)
			{
				cc.Velocity -= pushVelocity.Normal * travelDot * 0.6f;
			}

			cc.Velocity += pushVelocity * 128.0f;
		}

		cc.Move();

		if (!cc.IsOnGround)
		{
			cc.Velocity += halfGravity;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ(0);
		}
	}

	protected virtual void UpdateGrounding()
	{
		bool wasGrounded = isGrounded;
		isGrounded = characterController.IsOnGround;
		if (isGrounded)
		{
			lastGrounded = 0;
		}
		else
		{
			lastUngrounded = 0;
		}

		if (wasGrounded != isGrounded)
		{
			OnGroundedChange();
		}
	}

	protected virtual void SmoothMoveBodyFromCrouchJumpOffset()
	{
		if (!isGrounded)
			return;

		if (!IsFullyValid(owner?.body))
			return;

		owner.body.heightOffset = MathY.MoveTowards(owner.body.heightOffset, 0.0f, Time.Delta * 100.0f);
		owner.body.SetPosition(true);
	}

	protected virtual void OnGroundedChange()
	{
		if (!isGrounded)
			return;

		/*if (IsFullyValid(owner?.body))
		{
			owner.body.heightOffset = MathY.MoveTowards(owner.body.heightOffset, 0.0f, Time.Delta * 100.0f);
			owner.body.SetPosition(true);
		}*/

		var verticalVel = MathF.Abs(lastVelocity.z);
		//Debuggin.ToScreen($"OnGroundedChange() verticalVel: {verticalVel}, config.minVelForGroundImpact: {config.minVelForGroundImpact}", 10.0f);
		if (verticalVel < config.minVelForGroundImpact)
			return;

		var start = WorldPosition + (Transform.World.Up * 0.5f);
		var end = WorldPosition - (Transform.World.Up * 5.0f);
		var trace = Game.ActiveScene.Trace
			.Capsule(Capsule.FromHeightAndRadius(3.5f, characterController.Radius * 0.75f))
			.FromTo(start, end)
			.IgnoreGameObjectHierarchy(GameObject)
			.WithoutTags(Tag.TRIGGER, Tag.CHARACTER_BODY, Tag.CHARACTER_BODY_REMOTE);//, Tag.PLAYER_CLIP, Tag.SKY);
		var result = trace.Run();

		if (result.Hit)
		{
			if (IsMetal(result.Surface))
			{
				//BroadcastGroundSlamSound(result.Surface.Sounds.ImpactSoft, result.HitPosition, config.soundOverrideForMetalImpact);
			}
			else
			{
				//BroadcastGroundSlamSound(result.Surface.Sounds.ImpactSoft, result.HitPosition);//, 0.05f);
			}
		}
	}

	public virtual bool CanJump()
	{
		if (lastJump < config.jumpCooldown)
			return false;

		if (!isSliding && lastGrounded > config.jumpCoyoteTime)
			return false;

		return true;
	}

	public virtual bool CanUncrouch()
	{
		if (!isCrouching) 
			return true;

		var traceResult = characterController.TraceDirection(Vector3.Up * config.duckHeight);
		if (traceResult.Hit)
			return false;

		return true;
	}

	protected virtual void SlideInput()
	{
		if (isSliding)
		{
			if (Input.Released(Inputs.duck) || Input.Released(Inputs.duck_alt))
			{
				isSliding = false;
			}
			return;
		}

		if (!Input.Pressed(Inputs.duck) && !Input.Down(Inputs.duck_alt))
			return;

		if (!characterController.IsOnGround)
			return;

		if (characterController.Velocity.WithZ(0).Length < config.slideStartMinVelocity)
			return;

		isSliding = true;
		
		slideVelocityReduction = MathY.InverseLerp(config.sprintMoveSpeed * 2, config.sprintMoveSpeed * 3, characterController.Velocity.Length);
		if (cheat_remove_slide_vel_cap)
		{
			slideVelocityReduction = 0.0f;
		}
		slideVelocity = characterController.Velocity;
		slideStart = 0;
	}

	protected virtual void CrouchingInput()
	{
		wishCrouch = Input.Down(Inputs.duck) || Input.Down(Inputs.duck_alt);

		if (isSliding)
		{
			wishCrouch = true;
		}

		if (wishCrouch == isCrouching)
			return;

		// crouch
		if (wishCrouch)
		{
			characterController.Height = config.crouchHeight;
			isCrouching = wishCrouch;

			// if we're not on the ground, slide up our bbox so when we crouch
			// the bottom shrinks, instead of the top, which will mean we can reach
			// places by crouch jumping that we couldn't.
			if (!characterController.IsOnGround)
			{
				var originalPos = WorldPosition;
				var moveDelta = Vector3.Up * config.duckHeight;
				characterController.MoveTo(originalPos + moveDelta, false);
				characterController.Transform.ClearInterpolation();
				eyeHeight -= config.duckHeight;

				owner.body.heightOffset = config.duckHeight;
				owner.body.SetPosition(true);
				//BroadcastBodyShift();
			}

			return;
		}

		// uncrouch
		if (!wishCrouch)
		{
			if (!CanUncrouch()) return;

			characterController.Height = config.characterHeight;
			isCrouching = wishCrouch;

			// uncrouching in the air is a fucking pain because you can't just teleport them because they'll clip
			// and MoveTo will shove them in the ground
			if (!characterController.IsOnGround)
			{
				var start = WorldPosition;
				var end = start - (Vector3.Up * config.duckHeight);
				var source = Scene.Trace.Ray(start, end);
				var trace = source.Size(characterController.BoundingBox).IgnoreGameObjectHierarchy(GameObject).WithoutTags(characterController.IgnoreLayers);

				var result = trace.Run();

				var endResult = result.Hit ? result.HitPosition : end;
				var moveDist = Vector3.DistanceBetween(start, endResult);
				var moveDelta = Vector3.Up * moveDist;
				var moveToPos = WorldPosition - moveDelta;

				//characterController.WorldPosition = moveToPos;
				characterController.MoveTo(moveToPos, true);
				characterController.Transform.ClearInterpolation();
				eyeHeight += moveDist;
				//eyeHeight += config.duckHeight;

				owner.body.heightOffset = 0.0f;
				owner.body.SetPosition(true);
				//BroadcastBodyUnshift();
				//Log.Info($"Air Uncrouch duckHeight: {config.duckHeight}, moveDist: {moveDist}");
			}

			return;
		}
	}

	public virtual void Launch(Vector3 force)
	{
		if (isSliding)
		{
			isSliding = false;
		}

		characterController.Velocity = characterController.Velocity.WithZ(0);
		characterController.Punch(force);
	}
}
