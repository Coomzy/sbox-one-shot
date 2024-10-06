
public partial interface IUIEvents : ISceneEvent<IUIEvents>
{
	void AddMedalEntry(string medalImage){}
	void OnRankUp(){ }
	void OnDamagedEnemy() { }
}