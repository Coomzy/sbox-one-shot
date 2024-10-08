
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Numerics;
using System.Text.Json.Serialization;
using static Sandbox.PhysicsContact;

public class DebugRagdoll : Component
{
	[Group("Setup"), Order(-100), Property] public ModelPhysics bodyPhysics { get; set; }
	[Group("Setup"), Order(-100), Property] public ModelCollider bodyCollider { get; set; }
	[Group("Setup"), Order(-100), Property] public SkinnedModelRenderer bodyRenderer { get; set; }
	[Group("Setup"), Order(-100), Property] public CitizenAnimationHelper thirdPersonAnimationHelper { get; set; }
	[Group("Setup"), Order(-100), Property] public Rigidbody rigidbody { get; set; }
	[Group("Setup"), Order(-100), Property] public FixedJoint fixedJoint { get; set; }

	[Group("Config"), Order(0), Property, Range(0, 15)] public int impaledPhysicsBodyIndex { get; set; }
	[Group("Config"), Order(0), Property] public bool isLocked { get; set; }

	[Group("Runtime"), Order(0), Property, ReadOnly] public int impaledPhysicsBodyIndexRemapped { get; set; }
	[Group("Runtime"), Order(100), Property] public PhysicsBody impaledPhysicsBody { get; set; }
	[Group("Runtime"), Order(100), Property, JsonIgnore] public string impaledPhysicsBodyName => impaledPhysicsBody?.GroupName;
	[Group("Runtime"), Order(100), Property, JsonIgnore] public int impaledPhysicsBodies => bodyPhysics.PhysicsGroup.Bodies.Count();

	// Sensible Indexes
	// 3 - HEad

	[Button]
	public void Toggle()
	{
		bool enable = !bodyPhysics.Enabled;
		bodyPhysics.Enabled = enable;
		bodyPhysics.MotionEnabled = enable;
		bodyRenderer.UseAnimGraph = !enable;
	}

	void LogBodies()
	{
		Debuggin.ToScreen($"Debug Ragdoll");
		Debuggin.ToScreen($"");
		if (!bodyPhysics.Enabled)
		{
			Debuggin.ToScreen($"Disabled");
			return;
		}

		Debuggin.ToScreen($"bodyPhysics.PhysicsGroup.Joints.Count: {bodyPhysics.PhysicsGroup.Joints.Count()}");
		Debuggin.ToScreen($"impaledPhysicsBodyIndex: {impaledPhysicsBodyIndex} name: {bodyPhysics.PhysicsGroup.Bodies.ElementAt(impaledPhysicsBodyIndex).GroupName}");
		Debuggin.ToScreen($"impaledPhysicsBodyIndexRemapped: {impaledPhysicsBodyIndexRemapped} name: {bodyPhysics.PhysicsGroup.Bodies.ElementAt(impaledPhysicsBodyIndexRemapped).GroupName}");
		Debuggin.ToScreen($"");
		Debuggin.ToScreen($"Bodies");
		foreach (var body in bodyPhysics.PhysicsGroup.Bodies)
		{
			Debuggin.ToScreen($"[{body.GroupIndex}] {body.GroupName}");
		}
		Debuggin.draw.Sphere(bodyPhysics.PhysicsGroup.Bodies.ElementAt(Bones.Terry.spine_0).Position, 10.0f, color: Color.Blue);
		Debuggin.draw.Sphere(bodyPhysics.PhysicsGroup.Bodies.ElementAt(Bones.Terry.spine_2).Position, 10.0f, color: Color.Yellow);
		Debuggin.draw.Sphere(bodyPhysics.PhysicsGroup.Bodies.ElementAt(Bones.Terry.head).Position, 10.0f, color: Color.Red);
		var hitBody = bodyPhysics.PhysicsGroup.Bodies.ElementAt(0).Position;
		Debuggin.ToScreen($"hitBody: {hitBody}");
	}

	void UpdateBody()
	{
		//impaledPhysicsBodyIndexRemapped = GetClosestSafeIndex(impaledPhysicsBodyIndex);

		var bodyLock = new PhysicsLock();
		bodyLock.Pitch = isLocked;
		bodyLock.Yaw = isLocked;
		bodyLock.Roll = isLocked;
		bodyLock.X = isLocked;
		bodyLock.Y = isLocked;
		bodyLock.Z = isLocked;

		bodyPhysics.Locking = bodyLock;
		bodyPhysics.MotionEnabled = !isLocked;

		if (isLocked)
		{
			return;
		}

		if (!bodyPhysics.Enabled)
		{
			return;
		}

		impaledPhysicsBody = bodyPhysics.PhysicsGroup.Bodies.ElementAt(impaledPhysicsBodyIndex);

		var followToPos = GameObject.Transform.World.Position;// - (GameObject.Transform.World.Forward * 30.0f);

		// If this was 
		if (impaledPhysicsBodyIndex == Bones.Terry.spine_0)
		{
			followToPos += Vector3.Up * 10.0f;
		}

		//hitPhysicsBody.Position = followToPos;
		var delta = Vector3.Direction(impaledPhysicsBody.Position, followToPos);

		impaledPhysicsBody.Velocity = delta * 500.0f;
		//impaledPhysicsBody.Velocity = Vector3.Zero;
		//impaledPhysicsBody.Position = MathY.MoveTowards(impaledPhysicsBody.Position, followToPos, Time.Delta * 100.0f);
		//Log.Info($"delta.Length: {delta.Length}, impaledByHarpoonSpear.isInFlight: {impaledByHarpoonSpear.isInFlight}");

		bool useOriginalMethod = false;
		if (useOriginalMethod) //delta.Length < 15.0f)// && !impaledByHarpoonSpear.isInFlight)
		{
			impaledPhysicsBody.Velocity = delta * 1000.0f;
			impaledPhysicsBody.Velocity = Vector3.Zero;
			impaledPhysicsBody.AngularVelocity = MathY.MoveTowards(impaledPhysicsBody.AngularVelocity, Vector3.Zero, Time.Delta * 15.0f);
			impaledPhysicsBody.AngularVelocity = Vector3.Zero;
			impaledPhysicsBody.Position = followToPos;



			//impaledPhysicsBody.Velocity = delta * 100.0f;
			//impaledPhysicsBody.Position = MathY.MoveTowards(impaledPhysicsBody.Position, followToPos, Time.Delta * 1000.0f);
		}
		else
		{
			float smoothRate = 0.075f;
			smoothRate = 0.0001f;

			float smoothRateRot = 0.075f;
			//smoothRateRot = 0.0001f;


			var velocity = impaledPhysicsBody.Velocity;
			Vector3.SmoothDamp(impaledPhysicsBody.Position, WorldPosition, ref velocity, smoothRate, Time.Delta);
			impaledPhysicsBody.Velocity = velocity;

			var angularVelocity = impaledPhysicsBody.AngularVelocity;
			Rotation.SmoothDamp(impaledPhysicsBody.Rotation, WorldRotation, ref angularVelocity, smoothRateRot, Time.Delta);
			impaledPhysicsBody.AngularVelocity = angularVelocity;

			//impaledPhysicsBody.AngularVelocity = Vector3.Zero;
		}
	}

	protected override void OnUpdate()
	{
		LogBodies();
		UpdateBody();
	}

	protected override void OnFixedUpdate()
	{

	}
}