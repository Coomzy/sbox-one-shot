
using Sandbox.Diagnostics;
using System;

[Group("OS")]
public class HarpoonGun_Proxy : EquipmentProxy
{
	[Group("Setup"), Property] public ModelRenderer spearModel {  get; set; }

	public void SetState(bool hasHarpoonSpear)
	{
		// TODO: Add this to Checks but for IsValid?
		//Assert.NotNull(hasHarpoonSpear);
		//Assert.True(hasHarpoonSpear);
		//Assert()
		model.Enabled = true;
		spearModel.Enabled = hasHarpoonSpear;
	}

	public override void SetVisibility(bool isVisible)
	{
		base.SetVisibility(isVisible);
		spearModel.Enabled = isVisible;
	}

	public override void SetRenderType(ModelRenderer.ShadowRenderType renderType)
	{
		base.SetRenderType(renderType);

		spearModel.RenderType = renderType;
	}
}
