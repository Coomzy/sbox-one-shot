
public partial interface IUIEvents : ISceneEvent<IUIEvents>
{
	void AddKillFeedEntry(string killer, string victim, string message){ }

	void EnableCrosshair(){}
	void DisableCrosshair(){}

	void AddSystemText(string message){}
	void BroadcastSystemText(string message){ }

	void AddAdminText(string message) { }
	void BroadcastAdminText(string message) { }
}