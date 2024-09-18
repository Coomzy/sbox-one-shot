
using Sandbox;
using Sandbox.Utility;
using System;
using System.Numerics;

[Group("GMF")]
public class HarpoonGun : Equipment
{
	[Group("Setup"), Order(-1), Property] public GameObject spearSpawnPoint {  get; set; }
	[Group("Setup"), Order(-1), Property] public GameObject spearPrefab {  get; set; }
	[Group("Setup"), Order(-1), Property] public GameObject spearHolder {  get; set; }

	[Group("Setup"), Order(-1), Property] public SkinnedModelRenderer gunCombinedModel {  get; set; }
	[Group("Setup"), Order(-1), Property] public SkinnedModelRenderer gunModel {  get; set; }
	[Group("Setup"), Order(-1), Property] public SkinnedModelRenderer spearModel {  get; set; }

	protected async override void OnStart()
	{
		base.OnStart();
		while (instigator == null)
			await Task.Frame();

		//Log.Info($"HarpoonGun::OnStart() OwnerConnection: {GameObject.Network.OwnerConnection}, IsProxy: {IsProxy}");

		gunCombinedModel.GameObject.Enabled = true;
		gunModel.GameObject.Enabled = true;
		spearModel.GameObject.Enabled = true;

		gunCombinedModel.RenderOptions.Overlay = !IsProxy;
		gunModel.RenderOptions.Overlay = !IsProxy;
		spearModel.RenderOptions.Overlay = !IsProxy;

		var renderType = IsProxy ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.Off;
		gunCombinedModel.RenderType = renderType;
		gunModel.RenderType = renderType;
		spearModel.RenderType = renderType;

		GameObject.Network.DisableInterpolation();

		gunCombinedModel.GameObject.Enabled = !IsProxy;
		gunModel.GameObject.Enabled = IsProxy;
		spearModel.GameObject.Enabled = IsProxy;

		procAnim.Enabled = !IsProxy;

		if (IsProxy)
		{
			while (instigator?.body == null)
				await Task.Frame();

			GameObject.SetParent(instigator.body.thirdPersonEquipmentAttachPoint);
			Transform.LocalPosition = Vector3.Zero;
			Transform.LocalRotation = Quaternion.Identity;
			return;
		}
	}
	
	protected override void OnUpdate()
	{
		base.OnUpdate();
		//string parameter = "castShadows";
		//Log.Info($"gunCombinedModel castShadows: {gunCombinedModel.Parameters.GetBool(parameter)} gunModel castShadows: {gunModel.Parameters.GetBool(parameter)}");

		/*var velocity = owner.characterController.Velocity.WithZ(0);
		var localVelocity = owner.characterController.Transform.World.NormalToLocal(velocity);

		var lerp = MathX.LerpInverse(velocity.Length, 0.0f, 320.0f);
		var lerpWalkToSprint = MathX.LerpInverse(velocity.Length, 190.0f, 320.0f);
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
		Transform.LocalPosition = localPos;*/

		//Gizmo.Draw.ScreenText($"bobTarget: {bobTarget}", new Vector2(10, 10));
		//Gizmo.Draw.ScreenText($"bobRate: {bobRate}", new Vector2(10, 25));
		//Gizmo.Draw.ScreenText($"isDownBob: {isDownBob}", new Vector2(10, 40));
		//Gizmo.Draw.ScreenText($"velocity: {velocity.Length}", new Vector2(10, 55));

		//float smooth = 10.0f;
		//Rotation newRotation = Quaternion.Identity;
		//Transform.LocalRotation = Quaternion.Lerp(Transform.LocalRotation, newRotation, (Time.Delta * smooth));
	}

	/*public async override void OnNetworkSpawn(Connection connection)
	{
		base.OnNetworkSpawn(connection);

		await Task.Frame();

		if (IsProxy)
		{
			return;
		}

		var shadowProxyInst = shadowProxyPrefab.Clone();
		shadowProxy = shadowProxyInst.Components.Get<HarpoonGun_ShadowProxy>();

		shadowProxy.GameObject.SetParent(owner.osCharacterVisual.thirdPersonEquipmentAttachPoint);
		shadowProxy.Transform.LocalPosition = Vector3.Zero;
		shadowProxy.Transform.LocalRotation = Quaternion.Identity;
	}*/

	public async override void FireStart()
	{
		base.FireStart();

		await Task.FixedUpdate();

		var spawnPoint = PlayerCamera.instance.GetPointInFront(70.0f);
		var spawnRot = PlayerCamera.instance.Transform.Rotation;

		var spearInst = spearPrefab.Clone(spawnPoint, spawnRot);
		if (spearInst != null)
		{
			spearInst.NetworkSpawn(GameObject.Network.OwnerConnection);

			var spear = spearInst.Components.Get<HarpoonSpear>();			
			if (spear != null)
			{
				spear.owner = this;
				spear.Launch(instigator.controller.Velocity);
			}
			Test(spear);
		}

		var handle = Sound.Play("harpoon.fire", spearSpawnPoint.Transform.Position);		
		if (handle != null)
		{
			handle.Occlusion = false;
		}
		instigator.body.Shoot();

		Fire_Remote();
	}

	async void Test(HarpoonSpear spear)
	{
		gunCombinedModel.GameObject.Enabled = false;
		gunModel.GameObject.Enabled = true;
		spearModel.GameObject.Enabled = true;
		spearModel.Transform.LocalPosition = new Vector3(0, 0, 0);

		gunModel.SceneModel.Flags.CastShadows = false;
		spearModel.SceneModel.Flags.CastShadows = false;

		spear.model.Enabled = false;
		var proxy = (HarpoonGun_Proxy)equipmentProxy;
		proxy.SetState(false);

		var preCheckDistance = 350.0f;
		var height = 5.0f;
		var radius = 5.0f;

		var start = PlayerCamera.instance.GetPointInFront(0.0f);
		var end = PlayerCamera.instance.GetPointInFront(preCheckDistance);

		var capsule = Capsule.FromHeightAndRadius(height, radius);
		var trace = Scene.Trace.Capsule(capsule, start, end)
			.IgnoreGameObjectHierarchy(GameObject)
			.IgnoreGameObjectHierarchy(instigator?.GameObject)
			.IgnoreGameObjectHierarchy(spear.GameObject);
		var result = trace.Run();

		var endResult = result.Hit ? result.HitPosition : end;
		var moveDist = Vector3.DistanceBetween(start, endResult);		

		TimeUntil reachSpear = 0.25f;
		float forwardOnlyTime = 0.15f;
		Vector3 startPos = spearModel.Transform.Position;

		if (result.Hit)
		{
			var lerp = MathX.LerpInverse(moveDist, 0.0f, preCheckDistance);
			reachSpear = MathX.Lerp(0.01f, 0.25f, lerp);
			Log.Info($"reachSpear: {reachSpear.Absolute}, lerp: {lerp}, hit: {result.GameObject}");
		}

		//Gizmo.Draw.LineCapsule(capsule);
		//ExtraDebug.draw.Capsule(capsule, 3.5f);
		ExtraDebug.draw.Capsule(start, end, radius, 3.5f);

		while (!reachSpear)
		{
			var destination = spear.Transform.Position;
			var localPoint = spearModel.Transform.World.PointToLocal(destination).WithY(0).WithZ(0);		
			var destinationForwardOnly = spearModel.Transform.World.PointToWorld(localPoint);

			if (reachSpear.Passed < forwardOnlyTime)
			{
				//destination = destinationForwardOnly;
			}
			else
			{
				var lerp = MathX.LerpInverse(reachSpear.Passed, forwardOnlyTime, reachSpear.Absolute);
				//destination = Vector3.Lerp(destinationForwardOnly, destination, lerp);
			}

			/*Gizmo.Transform = Game.ActiveScene.Transform.World;
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.Line(spearSpawnPoint.Transform.Position, destination);
			Gizmo.Draw.Color = Color.Yellow;
			Gizmo.Draw.Line(spearSpawnPoint.Transform.Position, destinationForwardOnly);*/

			spearModel.Transform.Position = Vector3.Lerp(startPos, destination, reachSpear.Fraction);
			await Task.Frame();
		}

		/*await Task.Frame();
		spearModel.Transform.LocalPosition = new Vector3(25, 0, 0);

		await Task.Frame();
		spearModel.Transform.LocalPosition = new Vector3(50, 0, 0);*/
		await Task.Frame();
		spearModel.GameObject.Enabled = false;
		spear.model.Enabled = true;

		// TEMP: while there is no reloading
		await Task.DelaySeconds(1.0f);

		gunCombinedModel.GameObject.Enabled = true;
		gunModel.GameObject.Enabled = false;
		spearModel.GameObject.Enabled = false;

		gunCombinedModel.SceneModel.Flags.CastShadows = false;

		proxy.SetState(true);

	}

	[Broadcast]
	public void Fire_Remote()
	{
		if (!IsProxy)
			return;

		Sound.Play("harpoon.fire", spearSpawnPoint.Transform.Position);
		instigator.body.Shoot();
	}
}
