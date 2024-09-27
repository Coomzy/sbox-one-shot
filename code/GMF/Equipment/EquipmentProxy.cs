
using System.Numerics;
using System.Reflection.PortableExecutable;
using static Sandbox.ModelRenderer;

[Group("GMF")]
public class EquipmentProxy : Component, IRoundEvents
{
	[Group("Setup"), Property] public ModelRenderer model {  get; set; }
	[Group("Setup"), Property] public GameObject twoHandedGrip { get; set; }

	[Group("Runtime"), Property] public Equipment equipment { get; set; }

	protected override void OnStart()
	{
		ModelRenderer.ShadowRenderType renderType = ModelRenderer.ShadowRenderType.Off;

		if (IsFullyValid(equipment))
		{
			renderType = equipment.IsProxy? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.ShadowsOnly;
			if (IsFullyValid(equipment?.instigator?.body?.thirdPersonEquipmentAttachPoint))
			{
				AttachTo(equipment?.instigator?.body?.thirdPersonEquipmentAttachPoint);
			}
		}
		else
		{
			Log.Error($"equipment on EquipmentProxy '{GameObject}' was null!");
		}

		SetRenderType(renderType);
	}

	public virtual void SetVisibility(bool isVisible)
	{
		model.Enabled = isVisible;
	}

	public virtual void SetRenderType(ModelRenderer.ShadowRenderType renderType)
	{
		model.RenderType = renderType;
	}

	public virtual void AttachTo(GameObject target)
	{
		if (!IsFullyValid(target))
		{
			Log.Error($"EquipmentProxy '{GameObject}' tried to attach, but target was null!");
			return;
		}
		GameObject.SetParent(target);
		Transform.LocalPosition = Vector3.Zero;
		Transform.LocalRotation = Quaternion.Identity;
	}

	public virtual void Dettach()
	{
		GameObject.SetParent(null, true);
	}

	public virtual void RoundCleanup()
	{
		GameObject.Destroy();
	}
}
