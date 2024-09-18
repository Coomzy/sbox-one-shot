
using Sandbox;
using System;

[Group("GMF")]
public class CharacterMovement : Component
{
	[Group("Setup"), Property] public Character owner { get; set; }
	[Group("Setup"), Property] public CharacterController characterController { get; set; }
	[Group("Setup"), Property] public CharacterMovementConfig config { get; set; }

	[Group("Runtime"), Property, Sync] public bool isSliding { get; set; }
	[Group("Runtime"), Property, Sync] public bool isCrouching { get; set; }
	[Group("Runtime"), Property, Sync] public Vector3 wishVelocity { get; set; }
	[Group("Runtime"), Property, Sync] public Vector3 slideVelocity { get; set; }

	[Group("Runtime"), Property] public bool wishCrouch { get; set; }
	[Group("Runtime"), Property] public float eyeHeight { get; set; }

	[Group("Runtime"), Property, Sync] public bool isGrounded { get; set; }
	[Group("Runtime"), Property] public TimeSince lastGrounded { get; set; }
	[Group("Runtime"), Property] public TimeSince lastUngrounded { get; set; }

	[Group("Runtime"), Property] public TimeSince lastJump { get; set; }
	[Group("Runtime"), Property] public TimeSince slideStart { get; set; }
		
	float CurrentMoveSpeed
	{
		get
		{
			if (isCrouching) return config.crouchMoveSpeed;
			if (Input.Down("run")) return config.sprintMoveSpeed;
			if (Input.Down("walk")) return config.walkMoveSpeed;

			return config.runMoveSpeed;
		}
	}

	protected override void OnStart()
	{
		config = config ?? new CharacterMovementConfig();

		eyeHeight = config.eyeHeight;
	}

	protected override void OnFixedUpdate()
	{
		if (IsProxy)
			return;

		SlideInput();

		if (isSliding)
		{
			SlideUpdate();
		}
		else
		{
			CrouchingInput();
			MovementInput();
		}

		UpdateGrounding();
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
		Vector3 curSlideVelocity = slideVelocity * slideSpeedScalar;

		characterController.Velocity = curSlideVelocity;

		if (CanJump() && Input.Pressed("jump"))
		{
			lastJump = 0;
			float jumpBoost = MathX.Clamp(slideSpeedScalar, 1.0f, 2.0f);
			characterController.Punch(Vector3.Up * (config.jumpHeight * jumpBoost));
			owner.body.Jump();
			isSliding = false;
		}

		characterController.Move();

		if (curSlideVelocity.Length <= config.slideMinVelocity)
		{
			isSliding = false;
		}

		if (!characterController.IsOnGround && lastGrounded > 0.2f)
		{
			isSliding = false;
		}

	}

	void MovementInput()
	{
		if (characterController is null)
			return;

		var cc = characterController;

		Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		wishVelocity = Input.AnalogMove;

		if (CanJump() && Input.Pressed("jump"))
		{
			lastJump = 0;
			cc.Punch(Vector3.Up * config.jumpHeight);
			owner.body.Jump();
		}

		if (!wishVelocity.IsNearlyZero())
		{
			wishVelocity = new Angles(0, owner.Transform.Rotation.Yaw(), 0).ToRotation() * wishVelocity;
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

		//
		// Don't walk through other players, let them push you out of the way
		//
		var pushVelocity = PlayerPusher.GetPushVector(Transform.Position + Vector3.Up * 40.0f, Scene, GameObject);
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

	void UpdateGrounding()
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

		if (isGrounded)
		{
			owner.body.heightOffset = MathY.MoveTowards(owner.body.heightOffset, 0.0f, Time.Delta * 100.0f);
			owner.body.SetPosition(true);
		}
	}

	void OnGroundedChange()
	{
		var osPawn = (OSCharacter)owner;
		//osPawn.osCharacterVisual.bodyRenderer.Transform.LocalPosition = Vector3.Zero;
		//osPawn.osCharacterVisual.bodyRenderer.Transform.ClearInterpolation();
	}

	bool CanJump()
	{
		if (lastJump < config.jumpCooldown)
			return false;

		if (!isSliding && lastGrounded > config.jumpCoyoteTime)
			return false;

		return true;
	}

	bool CanUncrouch()
	{
		if (!isCrouching) 
			return true;

		var tr = characterController.TraceDirection(Vector3.Up * config.duckHeight);
		return !tr.Hit; // hit nothing - we can!
	}

	void SlideInput()
	{
		if (isSliding)
		{
			if (Input.Released("duck"))
			{
				isSliding = false;
			}
			return;
		}

		if (!Input.Pressed("duck"))
			return;

		if (!characterController.IsOnGround)
			return;

		if (characterController.Velocity.WithZ(0).Length < config.slideStartMinVelocity)
			return;

		isSliding = true;
		slideVelocity = characterController.Velocity;
		slideStart = 0;
	}

	void CrouchingInput()
	{
		wishCrouch = Input.Down("duck");

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
				var originalPos = Transform.Position;
				var moveDelta = Vector3.Up * config.duckHeight;
				characterController.MoveTo(originalPos + moveDelta, false);
				Transform.ClearInterpolation();
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
				var start = Transform.Position;
				var end = start - (Vector3.Up * config.duckHeight);
				var source = Scene.Trace.Ray(start, end);
				var trace = source.Size(characterController.BoundingBox).IgnoreGameObjectHierarchy(GameObject).WithoutTags(characterController.IgnoreLayers);

				var result = trace.Run();

				var endResult = result.Hit ? result.HitPosition : end;
				var moveDist = Vector3.DistanceBetween(start, endResult);
				var moveDelta = Vector3.Up * moveDist;
				var moveToPos = Transform.Position - moveDelta;

				//characterController.Transform.Position = moveToPos;
				characterController.MoveTo(moveToPos, true);
				characterController.Transform.ClearInterpolation();
				//eyeHeight += moveDist;
				eyeHeight += config.duckHeight;

				owner.body.heightOffset = 0.0f;
				owner.body.SetPosition(true);
				//BroadcastBodyUnshift();
				//Log.Info($"Air Uncrouch duckHeight: {config.duckHeight}, moveDist: {moveDist}");
			}

			return;
		}
	}

	public void Launch(Vector3 force)
	{
		if (isSliding)
		{
			isSliding = false;
		}

		characterController.Velocity = characterController.Velocity.WithZ(0);
		characterController.Punch(force);
	}
}
