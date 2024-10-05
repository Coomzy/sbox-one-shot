
public static partial class UI_Colours
{
	public const string SELF = "#0099cc";
	public const string PARTY = "#6600cc";
	public const string FRIEND = "#006600";
	public const string PLAYER = "#FFA500";
	public const string DEAD = "red";

	public static string PlayerToColour(PlayerInfo player)
	{
		if (player == PlayerInfo.local)
		{
			return SELF;
		}

		if (player.Network.Owner.PartyId != 0 && player.Network.Owner.PartyId == Connection.Local.PartyId)
		{
			return PARTY;
		}

		var friend = new Friend((long)player.steamId);
		if (friend.IsFriend)
		{
			return FRIEND;
		}

		return PLAYER;
	}
}