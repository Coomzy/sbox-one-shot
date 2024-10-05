
public interface IGameModeEvents : ISceneEvent<IGameModeEvents>
{
	void MatchStart(){}
	void RoundCleanup(){ }
	void ModeStateChange(ModeState oldValue, ModeState newValue){}
}
