
using System;

[Group("OS")]
public class HarpoonGun_Proxy : EquipmentProxy
{
	[Group("Setup"), Property] public ModelRenderer harpoonSpear {  get; set; }

	public void SetState(bool hasHarpoonSpear)
	{
		model.Enabled = true;
		harpoonSpear.Enabled = hasHarpoonSpear;
	}

	public override void SetVisibility(bool isVisible)
	{
		base.SetVisibility(isVisible);
		harpoonSpear.Enabled = isVisible;
	}
}
