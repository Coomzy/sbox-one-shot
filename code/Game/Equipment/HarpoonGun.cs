
using Sandbox;
using Sandbox.Utility;
using System;
using System.Threading.Tasks;
using static Sandbox.SerializedProperty;

[GameResource("HarpoonGunConfig", "hgc", "HarpoonGunConfig")]
public class HarpoonGunConfig : GameResource
{
	[Group("Reload"), Order(-1), Property] public float reloadTime { get; set; } = 0.35f;
	[Group("Reload"), Order(-1), Property] public Curve reloadXAxisCurve { get; set; } = new Curve(new Curve.Frame(0.0f, 0.0f), new Curve.Frame(1.0f, 1.0f));
	[Group("Reload"), Order(-1), Property] public Curve reloadYAxisCurve { get; set; } = new Curve(new Curve.Frame(0.0f, 0.0f), new Curve.Frame(1.0f, 1.0f));
	[Group("Reload"), Order(-1), Property] public Curve reloadZAxisCurve { get; set; } = new Curve(new Curve.Frame(0.0f, 0.0f), new Curve.Frame(1.0f, 1.0f));
}

[Group("GMF")]
public class HarpoonGun : Equipment
{
	[Group("Setup"), Order(-1), Property] public GameObject spearPrefab {  get; set; }
	[Group("Setup"), Order(-1), Property] public GameObject spearHolder {  get; set; }

	[Group("Setup"), Order(-1), Property] public SkinnedModelRenderer gunModel {  get; set; }
	[Group("Setup"), Order(-1), Property] public SkinnedModelRenderer spearModel {  get; set; }

	[Group("Config"), Order(0), Property] public HarpoonGunConfig config {  get; set; }

	[Group("Runtime"), Order(100), Property, Sync, Change("OnRep_hasAmmo")] public bool hasAmmo {  get; set; } = true;
	[Group("Runtime"), Order(100), Property] public bool isReloading {  get; set; } = false;

	protected override void OnStart()
	{
		base.OnStart();

		procAnim.config = GameSettings.instance.harpoonProceduralAnimationConfig;
		config = GameSettings.instance.harpoonGunConfig;

		// TODO: This should be done in equipment, but isn't needed because...
		// ... OSCharacter can ONLY have this gun so it's on the prefab instead of spawning at runtime
		/*if (IsProxy)
		{
			return;
		}

		while (instigator?.body == null)
			await Task.Frame();

		GameObject.SetParent(instigator.body.thirdPersonEquipmentAttachPoint);
		LocalPosition = Vector3.Zero;
		LocalRotation = Quaternion.Identity;*/
	}

	public override void SetFirstPersonMode(bool isFirstPerson)
	{
		base.SetFirstPersonMode(isFirstPerson);

		gunModel.GameObject.Enabled = false;
		spearModel.GameObject.Enabled = false;

		gunModel.RenderType = ModelRenderer.ShadowRenderType.Off;
		spearModel.RenderType = ModelRenderer.ShadowRenderType.Off;

		gunModel.RenderOptions.Overlay = isFirstPerson;
		spearModel.RenderOptions.Overlay = isFirstPerson;
	}

	public override bool CanFire()
	{
		if (!hasAmmo)
			return false;

		if (isReloading)
			return false;

		return base.CanFire();
	}

	public override bool CanDryFire()
	{
		if (hasAmmo)
			return false;

		if (isReloading)
			return false;

		return base.CanDryFire();
	}

	public override void Fire()
	{
		base.Fire();

		hasAmmo = false;		

		var camPoint = PlayerCamera.instance.WorldPosition;
		var spawnPoint = PlayerCamera.instance.GetPointInFront(70.0f);
		var spawnRot = PlayerCamera.instance.WorldRotation;

		// TODO: Move projectile spawn into base or component?
		var spearInst = spearPrefab.Clone(spawnPoint, spawnRot);
		if (spearInst != null)
		{
			var projectile = spearInst.Components.Get<Projectile>();
			if (projectile != null)
			{
				projectile.instigator = this;
				projectile.velocity = projectile.WorldTransform.Forward * 2500.0f;
			}

			spearInst.NetworkSpawn(GameObject.Network.Owner);

			if (projectile != null)
			{
				projectile.SpawnSource(camPoint);
			}
		}

		model.GameObject.Enabled = false;
		gunModel.GameObject.Enabled = true;
		spearModel.GameObject.Enabled = false;

		var handle = Sound.Play("harpoon.fire", muzzleSocket.WorldPosition);
		if (handle != null)
		{
			handle.Occlusion = false;
		}
		instigator.body.Shoot();

		Fire_Remote();
	}

	[Broadcast]
	public void Fire_Remote()
	{
		if (!IsProxy)
			return;

		var handle = Sound.Play("harpoon.fire", muzzleSocket.WorldPosition);
		handle.SpacialBlend = 1.0f;

		instigator.body.Shoot();
	}

	public override void DryFire()
	{
		base.DryFire();

		Sound.Play("harpoon.dryfire", muzzleSocket.WorldPosition);
		DryFire_Remote();
	}

	[Broadcast]
	public void DryFire_Remote()
	{
		if (!IsProxy)
			return;

		var handle = Sound.Play("harpoon.dryfire", muzzleSocket.WorldPosition);
		handle.SpacialBlend = 1.0f;
	}

	public async void Reload()
	{
		if (isReloading)
			return;

		if (IsProxy)
		{
			return;
		}
		isReloading = true;

		Sound.Play("harpoon.reload");

		model.GameObject.Enabled = false;
		gunModel.GameObject.Enabled = true;
		spearModel.GameObject.Enabled = true;
		HarpoonGun_Proxy proxy = equipmentProxy as HarpoonGun_Proxy;
		if (proxy != null)
		{
			proxy.SetState(true);
			var _ = ReloadAnimation(proxy.spearModel.GameObject);
		}

		RemoteReloadAnim();

		await ReloadAnimation(spearModel.GameObject);
		hasAmmo = true;
		isReloading = false;

		model.GameObject.Enabled = true;
		gunModel.GameObject.Enabled = false;
		spearModel.GameObject.Enabled = false;
	}

	public async Task ReloadAnimation(GameObject target)
	{
		TimeUntil reloadFinish = config.reloadTime;

		Vector3 scale = Vector3.Zero;
		while (!reloadFinish)
		{
			if (!IsFullyValid(target))
				break;

			scale.x = config.reloadXAxisCurve.Evaluate(reloadFinish.Fraction);
			scale.y = config.reloadYAxisCurve.Evaluate(reloadFinish.Fraction);
			scale.z = config.reloadZAxisCurve.Evaluate(reloadFinish.Fraction);

			target.LocalScale = scale;
			await Task.Frame();
		}
	}

	[Broadcast]
	void RemoteReloadAnim()
	{
		if (!IsProxy)
		{
			return;
		}

		var handle = Sound.Play("harpoon.reload", muzzleSocket.WorldPosition);
		handle.SpacialBlend = 1.0f;

		HarpoonGun_Proxy proxy = equipmentProxy as HarpoonGun_Proxy;
		if (proxy != null)
		{
			proxy.SetState(true);
			var _ = ReloadAnimation(proxy.spearModel.GameObject);
		}
	}

	public void OnRep_hasAmmo(bool oldValue, bool newValue)
	{
		if (equipmentProxy is HarpoonGun_Proxy proxy)
		{
			proxy.SetState(newValue);
		}
	}
}
