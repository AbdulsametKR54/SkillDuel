using System.Threading.Tasks;

namespace SkillDuel.Application.Interfaces;

public interface IGameHub
{
    Task MatchFound(object data);
    Task RoundStarted(object data);
    Task RoundResult(object data);
    Task GameEnded(object data);
    Task GameError(object data);
    Task MatchmakingTimeout(object data);
    
    Task GuestJoined(string guestUsername);
    Task RoomMessage(string username, string message, string timestamp);
    Task RoomGameStarting(Guid sessionId);
    Task EmoteReceived(Guid playerId, string emote);
    Task FriendInviteReceived(object data);
    Task InviteExpired(object data);
    Task InviteDeclined(object data);
    Task FriendRequestReceived(string senderUsername);
    Task OpponentDisconnected(object data);
    Task OpponentReconnecting(object data);
    Task OpponentReconnected(object data);
    Task RoomClosed();
    Task PlayerLeft(object data);
    Task KickedFromRoom();
    Task RoomSettingsUpdated(object data);
    Task HostChanged(Guid newAdminId);
}

