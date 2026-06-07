'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { roomsApi, usersApi, friendsApi } from '@/lib/api';
import signalRService from '@/lib/signalr';
import { useGameStore } from '@/lib/store';
import { Loader2, Send, User as UserIcon, Shield, Copy, Share2, LogOut, Swords, MessageSquare, Users, MailCheck } from 'lucide-react';
import { toast } from 'sonner';

interface RoomDetails {
  id: string;
  code: string;
  name: string;
  hostId: string;
  hostUsername: string;
  isPrivate: boolean;
  categoryId?: string;
  categoryName?: string;
  difficulty?: string;
  questionType?: string;
  roundCount: number;
  maxPlayers: number;
  status: string;
  createdAt: string;
  players: { userId: string; username: string; slotNumber: number }[];
}

interface Message {
  username: string;
  text: string;
  timestamp: string;
}

export default function RoomPage() {
  const router = useRouter();
  const { code } = useParams() as { code: string };
  const [room, setRoom] = useState<RoomDetails | null>(null);
  const [me, setMe] = useState<any>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [startingGame, setStartingGame] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);

  const { setGameStatus, setSessionId, setUserId } = useGameStore();

  const [friends, setFriends] = useState<any[]>([]);
  const [friendsLoading, setFriendsLoading] = useState(false);
  const [invitedFriends, setInvitedFriends] = useState<string[]>([]);

  const fetchFriends = async () => {
    setFriendsLoading(true);
    try {
      const res = await friendsApi.list();
      setFriends(res.data || []);
    } catch (e) {
      console.error("Failed to fetch friends", e);
    } finally {
      setFriendsLoading(false);
    }
  };

  const handleInviteFriend = async (friendId: string) => {
    try {
      await signalRService.inviteFriend(friendId, code);
      setInvitedFriends(prev => [...prev, friendId]);
      toast.success("Davet başarıyla gönderildi!");
    } catch (e) {
      toast.error("Davet gönderilemedi.");
    }
  };

  useEffect(() => {
    fetchFriends();
  }, []);

  useEffect(() => {
    let isMounted = true;
    const fetchData = async () => {
      console.log(`[RoomPage] Mounting room ${code}. Fetching data...`);
      try {
        const [rRes, uRes] = await Promise.all([
          roomsApi.get(code),
          usersApi.me()
        ]);
        console.log(`[RoomPage] Data fetched successfully. Room data:`, rRes.data);
        console.log(`[RoomPage] User data:`, uRes.data);
        if (!isMounted) return;
        setRoom(rRes.data);
        setMe(uRes.data);
        
        console.log(`[RoomPage] Joining SignalR room group ${code}...`);
        await signalRService.joinRoomGroup(code);
        console.log(`[RoomPage] Joined SignalR room group ${code}.`);

        if (!isMounted) return;

        const conn = signalRService.getConnection();
        
        // Clear existing handlers to prevent double registration
        conn?.off('GuestJoined');
        conn?.off('RoomMessage');
        conn?.off('RoomGameStarting');
        conn?.off('MatchFound');
        conn?.off('RoomClosed');
        conn?.off('PlayerLeft');

        signalRService.onGuestJoined(async (guestUsername) => {
          console.log(`[RoomPage] SignalR Event: onGuestJoined -> ${guestUsername}`);
          try {
            const rRes = await roomsApi.get(code);
            setRoom(rRes.data);
          } catch(e) {
            console.error("Failed to fetch room on guest joined", e);
          }
          toast.success(`${guestUsername} joined the room!`);
        });

        signalRService.onRoomMessage((username, text, timestamp) => {
          console.log(`[RoomPage] SignalR Event: onRoomMessage -> ${username}: ${text}`);
          setMessages(prev => [...prev, { username, text, timestamp }]);
        });

        signalRService.onRoomGameStarting((sessionId) => {
          console.log(`[RoomPage] SignalR Event: onRoomGameStarting -> sessionId: ${sessionId}`);
          toast.success("Game starting...");
        });

        signalRService.onMatchFound((data: any) => {
          console.log(`[RoomPage] SignalR Event: MatchFound!`, data);
          setSessionId(data.sessionId);
          setUserId(data.myId);
          if (data.players) {
            useGameStore.getState().setPlayers(data.players.map((p: any) => ({
              id: p.id,
              username: p.username,
              elo: p.elo,
              score: 0,
              correctCount: 0
            })));
          }
          setGameStatus('playing');
          router.push(`/duel/${data.sessionId}`);
        });

        conn?.on('RoomClosed', () => {
          toast.error("Oda sahibi ayrıldı, oda kapatıldı.");
          router.push('/lobby');
        });

        conn?.on('PlayerLeft', async (data: any) => {
          console.log(`[RoomPage] SignalR Event: PlayerLeft ->`, data);
          try {
            const rRes = await roomsApi.get(code);
            setRoom(rRes.data);
          } catch(e) {
            console.error("Failed to fetch room on player left", e);
          }
          toast.info("Bir oyuncu odadan ayrıldı.");
        });
      } catch (err) {
        console.error(`[RoomPage] Error fetching data or joining group:`, err);
        toast.error("Room not found or unauthorized");
        router.push('/lobby');
      } finally {
        if (isMounted) setLoading(false);
      }
    };

    fetchData();

    return () => {
      isMounted = false;
      console.log(`[RoomPage] Unmounting. Leaving SignalR room group ${code}...`);
      const conn = signalRService.getConnection();
      conn?.off('GuestJoined');
      conn?.off('RoomMessage');
      conn?.off('RoomGameStarting');
      conn?.off('MatchFound');
      conn?.off('RoomClosed');
      conn?.off('PlayerLeft');
      signalRService.leaveRoomGroup(code);
    };
  }, [code, router, setSessionId, setUserId, setGameStatus]);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newMessage.trim()) return;
    
    try {
      await signalRService.sendRoomMessage(code, newMessage);
      setNewMessage('');
    } catch (err) {
      toast.error("Failed to send message");
    }
  };

  const handleStartGame = async () => {
    if (startingGame) return;
    setStartingGame(true);
    try {
      await signalRService.startRoomGame(code);
    } catch (err) {
      toast.error("Failed to start game");
      setStartingGame(false);
    }
  };

  const handleJoinClick = async () => {
    try {
      if (room?.isPrivate) {
        const pwd = prompt("Enter room password:");
        if (pwd === null) return;
        await roomsApi.join(code, { password: pwd });
      } else {
        await roomsApi.join(code, {});
      }
      
      toast.success("Joined room successfully!");
      // Refetch data
      const [rRes, uRes] = await Promise.all([roomsApi.get(code), usersApi.me()]);
      setRoom(rRes.data);
      setMe(uRes.data);
    } catch (err: any) {
      const msg = err.response?.data?.error || err.response?.data?.message || (typeof err.response?.data === 'string' ? err.response.data : err.message) || "Failed to join room";
      toast.error(msg);
    }
  };

  const copyCode = () => {
    navigator.clipboard.writeText(code);
    toast.success("Room code copied!");
  };

  const shareRoom = () => {
    navigator.clipboard.writeText(window.location.href);
    toast.success("Room link copied!");
  };

  if (loading) return <div className="flex items-center justify-center min-h-screen bg-background"><Loader2 className="h-12 w-12 animate-spin text-primary" /></div>;
  if (!room) return null;

  const isJoined = room.players?.some(p => p.userId === me?.id) || false;
  const isHost = room.players?.find(p => p.slotNumber === 1)?.userId === me?.id;
  const notJoined = !isJoined;
  const canStart = isHost && room.players?.length === room.maxPlayers;

  const handleLeaveRoom = async () => {
    if (isHost) {
      try {
        await roomsApi.delete(code);
      } catch (err) {
        console.error("Failed to delete room on leave", err);
      }
    }
    router.push('/lobby');
  };

  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col">
      <header className="bg-card border-b border-border p-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="bg-gradient-accent p-2 rounded-lg"><Swords className="h-5 w-5 text-white" /></div>
          <div>
            <h1 className="text-xl font-black tracking-tight">{room.name}</h1>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <span className="bg-input px-2 py-0.5 rounded font-mono font-bold text-primary select-all">{room.code}</span>
              <span>•</span>
              <span>{room.categoryName || 'All Categories'}</span>
              <span>•</span>
              <span className="capitalize">{room.difficulty || 'Any'}</span>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={copyCode} className="p-2 rounded-lg hover:bg-input text-muted-foreground transition-all" title="Copy Code"><Copy className="h-5 w-5" /></button>
          <button onClick={shareRoom} className="p-2 rounded-lg hover:bg-input text-muted-foreground transition-all" title="Share link"><Share2 className="h-5 w-5" /></button>
          <button onClick={handleLeaveRoom} className="p-2 rounded-lg hover:bg-destructive/10 text-destructive transition-all" title="Leave Room"><LogOut className="h-5 w-5" /></button>
        </div>
      </header>

      <main className="flex-1 grid grid-cols-1 lg:grid-cols-3 gap-6 p-6 overflow-hidden">
        {/* Players Area */}
        <div className="lg:col-span-2 space-y-6 flex flex-col">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Player Slots */}
            {Array.from({ length: room.maxPlayers }).map((_, idx) => {
              const slotNumber = idx + 1;
              const player = room.players?.find(p => p.slotNumber === slotNumber);
              const isSlotHost = slotNumber === 1;
              
              return (
                <div key={idx} className={`bg-card border-2 ${isSlotHost ? 'border-primary/20' : player ? 'border-primary/20' : 'border-dashed border-border'} rounded-2xl p-6 flex flex-col items-center justify-center space-y-4 transition-all`}>
                  {player ? (
                    <>
                      <div className="relative">
                        <div className={`w-20 h-20 ${isSlotHost ? 'bg-primary/10 border-primary' : 'bg-primary/10 border-primary/50'} rounded-full flex items-center justify-center border-2`}>
                          <UserIcon className="h-10 w-10 text-primary" />
                        </div>
                        {isSlotHost && <div className="absolute -top-1 -right-1 bg-primary text-white p-1 rounded-full"><Shield className="h-3 w-3" /></div>}
                      </div>
                      <div className="text-center">
                        <p className="font-black text-xl">{player.username}</p>
                        <p className="text-xs text-muted-foreground uppercase font-bold tracking-widest">{isSlotHost ? 'HOST' : `GUEST ${slotNumber - 1}`}</p>
                      </div>
                    </>
                  ) : (
                    <>
                      <div className="w-20 h-20 bg-input rounded-full flex items-center justify-center border-2 border-border">
                        <Loader2 className="h-8 w-8 text-muted-foreground animate-spin" />
                      </div>
                      <div className="text-center">
                        <p className="font-bold text-muted-foreground italic">Waiting...</p>
                      </div>
                    </>
                  )}
                </div>
              );
            })}
          </div>

          {/* Settings & Invite Container */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 flex-1">
            {/* Game Settings Summary */}
            <div className="bg-card border border-border rounded-2xl p-6 flex flex-col justify-between">
              <div>
                <h3 className="text-sm font-black uppercase tracking-widest text-muted-foreground mb-4">Game Settings</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div className="bg-input p-3 rounded-xl">
                    <p className="text-[10px] uppercase font-black text-muted-foreground mb-1">Rounds</p>
                    <p className="font-bold text-sm">{room.roundCount}</p>
                  </div>
                  <div className="bg-input p-3 rounded-xl">
                    <p className="text-[10px] uppercase font-black text-muted-foreground mb-1">Category</p>
                    <p className="font-bold text-sm truncate">{room.categoryName || 'Any'}</p>
                  </div>
                  <div className="bg-input p-3 rounded-xl">
                    <p className="text-[10px] uppercase font-black text-muted-foreground mb-1">Difficulty</p>
                    <p className="font-bold text-sm capitalize">{room.difficulty || 'Any'}</p>
                  </div>
                  <div className="bg-input p-3 rounded-xl">
                    <p className="text-[10px] uppercase font-black text-muted-foreground mb-1">Type</p>
                    <p className="font-bold text-sm capitalize">{room.questionType || 'Any'}</p>
                  </div>
                </div>
              </div>
              
              <div className="pt-6">
                {notJoined ? (
                  <button 
                    onClick={handleJoinClick}
                    className="w-full h-14 text-lg font-black rounded-2xl transition-all uppercase tracking-wider flex items-center justify-center gap-3 bg-gradient-accent text-white shadow-lg hover:opacity-90 active:scale-95"
                  >
                    <Swords className="h-5 w-5" />
                    JOIN ROOM
                  </button>
                ) : isHost ? (
                  <button 
                    onClick={handleStartGame}
                    disabled={!canStart || startingGame}
                    className={`w-full h-14 text-lg font-black rounded-2xl transition-all uppercase tracking-wider flex items-center justify-center gap-3 ${canStart && !startingGame ? 'bg-gradient-accent text-white shadow-lg hover:opacity-90 active:scale-95' : 'bg-muted text-muted-foreground cursor-not-allowed'}`}
                  >
                    {startingGame ? <Loader2 className="h-5 w-5 animate-spin" /> : <Swords className="h-5 w-5" />}
                    {startingGame ? 'STARTING...' : canStart ? 'START GAME' : 'WAITING FOR PLAYERS'}
                  </button>
                ) : (
                  <div className="w-full h-14 flex items-center justify-center bg-primary/10 border-2 border-primary/30 rounded-2xl text-primary font-black uppercase tracking-widest text-sm animate-pulse">
                    Waiting for host...
                  </div>
                )}
              </div>
            </div>

            {/* Quick Invite Friends Panel */}
            <div className="bg-card border border-border rounded-2xl p-6 flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-sm font-black uppercase tracking-widest text-muted-foreground flex items-center gap-2">
                    <Users className="h-4 w-4 text-primary" /> Arkadaşlarım
                  </h3>
                  <button onClick={fetchFriends} className="text-xs font-bold text-primary hover:underline">Yenile</button>
                </div>

                {friendsLoading ? (
                  <div className="flex items-center justify-center py-8 text-muted-foreground text-xs gap-2">
                    <Loader2 className="h-4 w-4 animate-spin text-primary" /> Yükleniyor...
                  </div>
                ) : friends.length === 0 ? (
                  <div className="text-center py-8 text-muted-foreground text-xs italic">
                    Arkadaşınız bulunamadı.
                  </div>
                ) : (
                  <div className="space-y-2 max-h-[160px] overflow-y-auto pr-1">
                    {friends.map((f) => {
                      const isInvited = invitedFriends.includes(f.friendId);
                      return (
                        <div key={f.friendshipId} className="flex items-center justify-between p-2 bg-input rounded-xl border border-border">
                          <div className="flex items-center gap-2">
                            <span className={`h-2.5 w-2.5 rounded-full ${f.isOnline ? 'bg-[#3fb950] shadow-[0_0_8px_#3fb950]' : 'bg-muted-foreground/30'}`} />
                            <div className="flex flex-col">
                              <span className="text-xs font-bold text-foreground truncate max-w-[100px]">{f.friendUsername}</span>
                              <span className="text-[9px] text-muted-foreground">{f.friendElo} Elo</span>
                            </div>
                          </div>
                          <button
                            onClick={() => handleInviteFriend(f.friendId)}
                            disabled={isInvited}
                            className={`px-3 py-1.5 rounded-lg text-[10px] font-black uppercase transition-all flex items-center gap-1 ${
                              isInvited 
                                ? 'bg-[#3fb950]/15 text-[#3fb950] border border-[#3fb950]/30' 
                                : 'bg-primary text-white hover:opacity-90 active:scale-95'
                            }`}
                          >
                            {isInvited ? (
                              <><MailCheck className="h-3 w-3" /> Davet Edildi</>
                            ) : (
                              'Davet Et'
                            )}
                          </button>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
              <div className="text-[10px] text-muted-foreground text-center italic pt-4">
                Arkadaşlarınızı davet ederek anında düelloya başlayın!
              </div>
            </div>
          </div>
        </div>

        {/* Chat Area */}
        <div className="bg-card border border-border rounded-2xl flex flex-col overflow-hidden h-[500px] lg:h-auto">
          <div className="p-4 border-b border-border flex items-center gap-2">
            <MessageSquare className="h-4 w-4 text-primary" />
            <h3 className="font-bold text-sm">Room Chat</h3>
          </div>
          <div className="flex-1 overflow-y-auto p-4 space-y-4">
            {messages.length === 0 && (
              <p className="text-center text-muted-foreground text-xs italic mt-4">Welcome to the chat! Be respectful.</p>
            )}
            {messages.map((msg, i) => (
              <div key={i} className={`flex flex-col ${msg.username === me?.username ? 'items-end' : 'items-start'}`}>
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-[10px] font-bold text-muted-foreground">{msg.username}</span>
                  <span className="text-[10px] text-muted-foreground/50">{msg.timestamp}</span>
                </div>
                <div className={`px-3 py-2 rounded-xl text-sm max-w-[80%] ${msg.username === me?.username ? 'bg-primary text-white rounded-tr-none' : 'bg-input text-foreground rounded-tl-none border border-border'}`}>
                  {msg.text}
                </div>
              </div>
            ))}
            <div ref={chatEndRef} />
          </div>
          <form onSubmit={handleSendMessage} className="p-4 bg-input border-t border-border flex gap-2">
            <input 
              type="text" 
              value={newMessage}
              onChange={e => setNewMessage(e.target.value)}
              placeholder="Type a message..."
              className="flex-1 bg-transparent border-none outline-none text-sm placeholder:text-muted-foreground"
            />
            <button type="submit" className="p-2 bg-primary text-white rounded-lg hover:opacity-90 transition-all">
              <Send className="h-4 w-4" />
            </button>
          </form>
        </div>
      </main>
    </div>
  );
}
