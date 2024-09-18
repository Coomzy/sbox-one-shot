
public abstract class SingletonComponent<T> : Component, IHotloadManaged where T : SingletonComponent<T>
{
#pragma warning disable SB3000 // Hotloading not supported
	static T _instance;
#pragma warning restore SB3000 // Hotloading not supported
	public static T instance => _instance;

	protected override void OnAwake()
	{
		if ( Active )
		{
			_instance = (T)this;
		}
	}

	void IHotloadManaged.Destroyed( Dictionary<string, object> state )
	{
		state["IsActive"] = instance == this;
	}

	void IHotloadManaged.Created( IReadOnlyDictionary<string, object> state )
	{
		if ( state.GetValueOrDefault( "IsActive" ) is true )
		{
			_instance = (T) this;
		}
	}

	protected override void OnDestroy()
	{
		if ( instance == this )
		{
			_instance = null;
		}
	}
}
