
[Group("GMF")]
public class EquipmentProxy : Component
{
	[Group("Setup"), Property] public ModelRenderer model {  get; set; }

	public virtual void SetVisibility(bool isVisible)
	{
		model.Enabled = isVisible;
	}

	public virtual void ShadowOnly()
	{
		model.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
	}
}
