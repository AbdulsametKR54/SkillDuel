import * as signalR from '@microsoft/signalr';
import Cookies from 'js-cookie';

const SIGNALR_URL = process.env.NEXT_PUBLIC_SIGNALR_URL || 'http://localhost:5000/hubs/game';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private static instance: SignalRService;

  private constructor() {}

  public static getInstance(): SignalRService {
    if (!SignalRService.instance) {
      SignalRService.instance = new SignalRService();
    }
    return SignalRService.instance;
  }

  private startPromise: Promise<void> | null = null;

  public async startConnection(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return;
    if (this.startPromise) return this.startPromise;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(SIGNALR_URL, {
        accessTokenFactory: () => Cookies.get('token') || '',
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.startPromise = this.connection.start()
      .then(() => {
        console.log('SignalR connected');
        this.startPromise = null;
      })
      .catch((err) => {
        console.error('SignalR Connection Error: ', err);
        this.startPromise = null;
        throw err;
      });

    return this.startPromise;
  }

  public async ensureConnected() {
    if (this.connection?.state === signalR.HubConnectionState.Connected) return;
    await this.startConnection();
  }

  public getConnection() {
    return this.connection;
  }

  public stopConnection() {
    this.connection?.stop();
    this.connection = null;
  }

  // Typed event handlers helpers
  public onMatchFound(callback: (data: any) => void) {
    this.connection?.on('MatchFound', callback);
  }

  public onRoundStarted(callback: (data: any) => void) {
    this.connection?.on('RoundStarted', callback);
  }

  public onRoundResult(callback: (data: any) => void) {
    this.connection?.on('RoundResult', callback);
  }

  public onGameEnded(callback: (data: any) => void) {
    this.connection?.on('GameEnded', callback);
  }

  public onOpponentDisconnected(callback: (data: any) => void) {
    this.connection?.on('OpponentDisconnected', callback);
  }

  public onMatchmakingTimeout(callback: (data: any) => void) {
    this.connection?.on('MatchmakingTimeout', callback);
  }

  public onGameError(callback: (data: any) => void) {
    this.connection?.on('GameError', callback);
  }

  // Room handlers
  public onGuestJoined(callback: (guestUsername: string) => void) {
    this.connection?.on('GuestJoined', callback);
  }

  public onRoomMessage(callback: (username: string, message: string, timestamp: string) => void) {
    this.connection?.on('RoomMessage', callback);
  }

  public onRoomGameStarting(callback: (sessionId: string) => void) {
    this.connection?.on('RoomGameStarting', callback);
  }

  public onEmoteReceived(callback: (playerId: string, emote: string) => void) {
    this.connection?.on('EmoteReceived', callback);
  }

  public onFriendInviteReceived(callback: (data: { senderId: string; senderUsername: string; roomCode: string }) => void) {
    this.connection?.on('FriendInviteReceived', callback);
  }

  public onInviteExpired(callback: (data: { friendId: string; roomCode: string }) => void) {
    this.connection?.on('InviteExpired', callback);
  }

  public onInviteDeclined(callback: (data: { friendId: string; username: string; roomCode: string }) => void) {
    this.connection?.on('InviteDeclined', callback);
  }

  public onFriendRequestReceived(callback: (senderUsername: string) => void) {
    this.connection?.on('FriendRequestReceived', callback);
  }

  // Room invoke methods
  public async joinRoomGroup(roomCode: string) {
    await this.ensureConnected();
    await this.connection?.invoke('JoinRoomGroup', roomCode);
  }

  public async leaveRoomGroup(roomCode: string) {
    await this.ensureConnected();
    await this.connection?.invoke('LeaveRoomGroup', roomCode);
  }

  public async sendRoomMessage(roomCode: string, message: string) {
    await this.ensureConnected();
    await this.connection?.invoke('SendRoomMessage', roomCode, message);
  }

  public async startRoomGame(roomCode: string) {
    await this.ensureConnected();
    await this.connection?.invoke('StartRoomGame', roomCode);
  }

  public async sendEmote(sessionId: string, emote: string) {
    await this.ensureConnected();
    await this.connection?.invoke('SendEmote', sessionId, emote);
  }

  public async inviteFriend(friendId: string, roomCode: string) {
    await this.ensureConnected();
    await this.connection?.invoke('InviteFriend', friendId, roomCode);
  }

  public async declineInvite(senderId: string, roomCode: string) {
    await this.ensureConnected();
    await this.connection?.invoke('DeclineInvite', senderId, roomCode);
  }

  public removeHandlers() {
    this.connection?.off('MatchFound');
    this.connection?.off('RoundStarted');
    this.connection?.off('RoundResult');
    this.connection?.off('GameEnded');
    this.connection?.off('OpponentDisconnected');
    this.connection?.off('MatchmakingTimeout');
    this.connection?.off('GameError');
    this.connection?.off('GuestJoined');
    this.connection?.off('RoomMessage');
    this.connection?.off('RoomGameStarting');
    this.connection?.off('EmoteReceived');
    this.connection?.off('FriendInviteReceived');
    this.connection?.off('InviteExpired');
    this.connection?.off('InviteDeclined');
    this.connection?.off('FriendRequestReceived');
  }

}

export const signalRService = SignalRService.getInstance();
export default signalRService;
