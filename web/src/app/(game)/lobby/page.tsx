'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { useMatchmaking } from '@/hooks/useMatchmaking';
import { usersApi, categoriesApi, roomsApi, friendsApi } from '@/lib/api';
import signalRService from '@/lib/signalr';
import Link from 'next/link';
import { LogOut, Trophy, Swords, User as UserIcon, BarChart3, Loader2, Clock, Target, History, Plus, Search, Lock, Users, ChevronRight, UserPlus, UserCheck, UserX, UserMinus, Mail } from 'lucide-react';
import { SuggestQuestionModal } from '@/components/SuggestQuestionModal';
import { toast } from 'sonner';

interface UserProfile { id: string; username: string; email: string; eloRating: number; totalWins: number; totalLosses: number; totalGames: number; role?: string; }
interface Category { id: string; name: string; }
interface Room { id: string; code: string; name: string; hostUsername: string; categoryName?: string; difficulty?: string; maxPlayers: number; isPrivate: boolean; status: string; }

const selectCls = 'w-full h-12 px-4 rounded-xl bg-input border border-border text-foreground text-sm outline-none appearance-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/20 font-medium';
const chevron = <svg className="absolute right-4 top-1/2 -translate-y-1/2 pointer-events-none text-muted-foreground" width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M2.5 4.5L6 8L9.5 4.5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/></svg>;

export default function LobbyPage() {
  const router = useRouter();
  const { isSearching, startMatchmasking, cancelMatchmaking } = useMatchmaking();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  
  // Tabs
  const [activeTab, setActiveTab] = useState<'matchmaking' | 'create' | 'browse'>('matchmaking');
  
  // Matchmaking State
  const [selectedMode, setSelectedMode] = useState<number>(5);
  const [selectedCategory, setSelectedCategory] = useState<string>('any');
  const [selectedDifficulty, setSelectedDifficulty] = useState<number | 'any'>('any');
  const [selectedType, setSelectedType] = useState<number | 'any'>('any');
  
  // Create Room State
  const [roomName, setRoomName] = useState('');
  const [roomPassword, setRoomPassword] = useState('');
  const [isPrivate, setIsPrivate] = useState(false);
  const [roomMaxPlayers, setRoomMaxPlayers] = useState(2);
  const [createLoading, setCreateLoading] = useState(false);

  // Browse Rooms State
  const [rooms, setRooms] = useState<Room[]>([]);
  const [browseLoading, setBrowseLoading] = useState(false);
  const [joiningRoom, setJoiningRoom] = useState<Room | null>(null);
  const [joinPassword, setJoinPassword] = useState('');
  
  // Join by Code State
  const [joinCode, setJoinCode] = useState('');
  const [joinCodeLoading, setJoinCodeLoading] = useState(false);

  // Friends & Requests State
  const [friends, setFriends] = useState<any[]>([]);
  const [friendRequests, setFriendRequests] = useState<any[]>([]);
  const [friendUsername, setFriendUsername] = useState('');
  const [friendsLoading, setFriendsLoading] = useState(false);
  const [inviteNotification, setInviteNotification] = useState<{ senderUsername: string; roomCode: string } | null>(null);

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

  const fetchFriendRequests = async () => {
    try {
      const res = await friendsApi.requests();
      setFriendRequests(res.data || []);
    } catch (e) {
      console.error("Failed to fetch friend requests", e);
    }
  };

  const handleSendFriendRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!friendUsername.trim()) return;
    try {
      const res = await friendsApi.sendRequest(friendUsername);
      toast.success(res.data?.message || "Arkadaşlık isteği başarıyla işlendi!");
      setFriendUsername('');
      fetchFriends();
      fetchFriendRequests();
    } catch (err: any) {
      const msg = err.response?.data?.error || err.response?.data?.message || "İstek gönderilemedi";
      toast.error(msg);
    }
  };

  const handleAcceptFriendRequest = async (id: string) => {
    try {
      const res = await friendsApi.acceptRequest(id);
      toast.success(res.data?.message || "Arkadaşlık isteği kabul edildi!");
      fetchFriends();
      fetchFriendRequests();
    } catch (err: any) {
      toast.error("İstek kabul edilemedi");
    }
  };

  const handleDeclineFriendRequest = async (id: string) => {
    try {
      const res = await friendsApi.declineRequest(id);
      toast.success(res.data?.message || "Arkadaşlık isteği reddedildi");
      fetchFriends();
      fetchFriendRequests();
    } catch (err: any) {
      toast.error("İstek reddedilemedi");
    }
  };

  const handleRemoveFriend = async (id: string) => {
    if (!confirm("Bu arkadaşı silmek istediğinize emin misiniz?")) return;
    try {
      const res = await friendsApi.removeFriend(id);
      toast.success(res.data?.message || "Arkadaş silindi");
      fetchFriends();
    } catch (err: any) {
      toast.error("Arkadaş silinemedi");
    }
  };

  useEffect(() => {
    fetchFriends();
    fetchFriendRequests();

    signalRService.startConnection().then(() => {
      signalRService.onFriendInviteReceived((data) => {
        console.log("[Lobby] Friend invite received:", data);
        setInviteNotification(data);
      });

      signalRService.onFriendRequestReceived((senderUsername) => {
        console.log("[Lobby] Friend request received from:", senderUsername);
        toast.info(`${senderUsername} size arkadaşlık isteği gönderdi!`);
        fetchFriendRequests();
        fetchFriends();
      });

      const conn = signalRService.getConnection();
      if (conn) {
        conn.on('PlayerLeft', () => {
          if (activeTab === 'browse') {
            fetchRooms();
          }
        });
        conn.on('RoomClosed', () => {
          if (activeTab === 'browse') {
            fetchRooms();
          }
        });
      }
    });

    return () => {
      const conn = signalRService.getConnection();
      if (conn) {
        conn.off('PlayerLeft');
        conn.off('RoomClosed');
      }
      signalRService.removeHandlers();
    };
  }, [activeTab]);

  useEffect(() => {
    Promise.all([usersApi.me(), categoriesApi.list()])
      .then(([u, c]) => {
        let role = '';
        const token = Cookies.get('token');
        if (token) {
          try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role;
          } catch (e) {}
        }
        setUser({ ...u.data, role });
        setCategories(c.data || []);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (activeTab === 'browse') {
      fetchRooms();
    }
  }, [activeTab]);

  const fetchRooms = async () => {
    setBrowseLoading(true);
    try {
      const res = await roomsApi.list();
      setRooms(res.data || []);
    } catch (err) {
      toast.error("Failed to fetch rooms");
    } finally {
      setBrowseLoading(false);
    }
  };

  const handleLogout = () => { Cookies.remove('token'); Cookies.remove('refreshToken'); router.push('/login'); };
  
  const handleFindMatch = async () => {
    await startMatchmasking(
      selectedMode,
      selectedCategory === 'any' ? undefined : selectedCategory,
      selectedDifficulty === 'any' ? undefined : selectedDifficulty,
      selectedType === 'any' ? undefined : selectedType,
    );
  };

  const handleCreateRoom = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!roomName.trim()) return toast.error("Room name is required");
    
    setCreateLoading(true);
    try {
      const res = await roomsApi.create({
        name: roomName,
        isPrivate,
        password: isPrivate ? roomPassword : null,
        categoryId: selectedCategory === 'any' ? null : selectedCategory,
        difficulty: selectedDifficulty === 'any' ? null : String(selectedDifficulty),
        questionType: selectedType === 'any' ? null : String(selectedType),
        roundCount: selectedMode,
        maxPlayers: roomMaxPlayers
      });
      router.push(`/room/${res.data.code}`);
    } catch (err: any) {
      const msg = err.response?.data?.error || err.response?.data?.message || (typeof err.response?.data === 'string' ? err.response.data : err.message) || "Failed to create room";
      toast.error(msg);
    } finally {
      setCreateLoading(false);
    }
  };

  const handleJoinRoom = async (room: Room) => {
    console.log(`[Lobby] Attempting to join room ${room.code}. IsPrivate: ${room.isPrivate}`);
    if (room.isPrivate) {
      setJoiningRoom(room);
      return;
    }
    
    try {
      console.log(`[Lobby] Calling roomsApi.join for public room ${room.code}`);
      const res = await roomsApi.join(room.code, {});
      console.log(`[Lobby] Join successful:`, res);
      router.push(`/room/${room.code}`);
    } catch (err: any) {
      console.error(`[Lobby] Failed to join public room ${room.code}:`, err.response?.data || err.message);
      const msg = err.response?.data?.error || err.response?.data?.message || (typeof err.response?.data === 'string' ? err.response.data : err.message) || "Failed to join room";
      toast.error(msg);
    }
  };

  const confirmJoin = async () => {
    if (!joiningRoom) return;
    try {
      console.log(`[Lobby] Calling roomsApi.join for private room ${joiningRoom.code}`);
      const res = await roomsApi.join(joiningRoom.code, { password: joinPassword });
      console.log(`[Lobby] Join successful:`, res);
      router.push(`/room/${joiningRoom.code}`);
    } catch (err: any) {
      console.error(`[Lobby] Failed to join private room ${joiningRoom.code}:`, err.response?.data || err.message);
      const msg = err.response?.data?.error || err.response?.data?.message || (typeof err.response?.data === 'string' ? err.response.data : err.message) || "Failed to join room";
      toast.error(msg);
    }
  };

  const handleJoinByCode = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!joinCode.trim()) return;
    
    setJoinCodeLoading(true);
    try {
      // Check if room exists
      console.log(`[Lobby] Fetching room by code: ${joinCode}`);
      const res = await roomsApi.get(joinCode);
      const roomData = res.data;
      
      console.log(`[Lobby] Found room:`, roomData);
      
      if (roomData.isPrivate) {
        // Show password modal
        setJoiningRoom(roomData);
      } else {
        // Direct join
        console.log(`[Lobby] Calling roomsApi.join for public room ${joinCode}`);
        await roomsApi.join(roomData.code, {});
        router.push(`/room/${roomData.code}`);
      }
    } catch (err: any) {
      console.error(`[Lobby] Failed to join by code ${joinCode}:`, err);
      toast.error("Room not found or unauthorized");
    } finally {
      setJoinCodeLoading(false);
    }
  };

  const winRate = user
    ? user.totalWins + user.totalLosses === 0 ? 0 : ((user.totalWins / (user.totalWins + user.totalLosses)) * 100).toFixed(1)
    : 0;

  const modeBtnCls = (active: boolean) =>
    `flex flex-col items-center justify-center p-4 rounded-xl border-2 transition-all font-medium ${active ? 'border-primary bg-primary/10 text-primary' : 'border-border bg-input hover:border-primary/50 text-muted-foreground'}`;

  const tabCls = (active: boolean) =>
    `flex-1 py-3 px-4 rounded-xl font-bold text-sm transition-all flex items-center justify-center gap-2 ${active ? 'bg-primary text-white shadow-lg' : 'bg-input text-muted-foreground hover:bg-card hover:text-foreground'}`;

  if (loading) return <div className="flex items-center justify-center min-h-screen bg-background"><Loader2 className="h-12 w-12 animate-spin text-primary" /></div>;

  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Top Bar */}
      <header className="sticky top-0 z-10 bg-background border-b border-border px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="bg-gradient-accent p-1.5 rounded-lg"><Swords className="h-5 w-5 text-white" /></div>
          <span className="text-xl font-black tracking-tighter text-gradient-accent">SKILLDUEL</span>
        </div>
        <nav className="hidden lg:flex items-center gap-1 bg-card p-1 rounded-xl border border-border">
          <Link href="/leaderboard"><button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold text-muted-foreground hover:bg-input hover:text-foreground transition-all"><Trophy className="h-4 w-4 text-primary" />Leaderboard</button></Link>
          <Link href="/history"><button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold text-muted-foreground hover:bg-input hover:text-foreground transition-all"><History className="h-4 w-4 text-primary" />My History</button></Link>
        </nav>
        <div className="flex items-center gap-4">
          {user?.role === 'Admin' && (
            <Link href="/admin/questions">
              <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-bold bg-primary/10 text-primary border border-primary/20 hover:bg-primary/20 transition-all">
                Admin Panel
              </button>
            </Link>
          )}
          <Link href="/profile" className="hidden md:flex flex-col items-end group cursor-pointer">
            <span className="text-sm font-semibold text-foreground group-hover:text-primary transition-colors">{user?.username}</span>
            <div className="flex items-center gap-1 text-xs text-muted-foreground bg-card border border-border px-2 py-0.5 rounded-full"><Trophy className="h-3 w-3 text-primary" /><span>{user?.eloRating} Elo</span></div>
          </Link>
          <button onClick={handleLogout} title="Logout" className="p-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-card border border-transparent hover:border-border transition-all"><LogOut className="h-5 w-5" /></button>
        </div>
      </header>

      {/* Main */}
      <main className="flex-1 max-w-7xl mx-auto w-full p-6 md:p-12">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">

          {/* Start a Duel Area */}
          <section className="lg:col-span-2 space-y-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-primary/10 rounded-xl"><Target className="h-6 w-6 text-primary" /></div>
                <h2 className="text-3xl font-bold tracking-tight text-foreground">Play Game</h2>
              </div>
              <div className="w-48"><SuggestQuestionModal categories={categories} /></div>
            </div>

            <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-xl">
              {/* Tabs */}
              <div className="p-2 flex gap-2 bg-input/50 border-b border-border">
                <button onClick={() => setActiveTab('matchmaking')} className={tabCls(activeTab === 'matchmaking')}><Swords className="h-4 w-4" />Hızlı Maç</button>
                <button onClick={() => setActiveTab('create')} className={tabCls(activeTab === 'create')}><Plus className="h-4 w-4" />Oda Kur</button>
                <button onClick={() => setActiveTab('browse')} className={tabCls(activeTab === 'browse')}><Search className="h-4 w-4" />Oda Bul</button>
              </div>

              <div className="p-8">
                {activeTab === 'matchmaking' && (
                  <div className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-300">
                    {isSearching ? (
                      <div className="flex flex-col items-center justify-center py-12 space-y-6 text-center">
                        <div className="relative">
                          <div className="absolute inset-0 bg-primary/20 rounded-full animate-ping" />
                          <div className="relative bg-gradient-accent p-6 rounded-full"><Loader2 className="h-12 w-12 text-white animate-spin" /></div>
                        </div>
                        <div><h3 className="text-2xl font-bold">Searching for Opponent...</h3><p className="text-muted-foreground">Finding a challenger who matches your skill level.</p></div>
                        <button onClick={cancelMatchmaking} className="px-10 h-12 text-lg font-bold rounded-2xl bg-destructive/10 border-2 border-destructive/30 text-destructive hover:bg-destructive hover:text-white transition-all">Cancel Search</button>
                      </div>
                    ) : (
                      <div className="space-y-8">
                        <div className="space-y-4">
                          <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Game Mode</label>
                          <div className="grid grid-cols-2 gap-4">
                            <button onClick={() => setSelectedMode(5)} type="button" className={modeBtnCls(selectedMode === 5)}><Clock className="h-6 w-6 mb-2" /><span className="font-bold">Short Duel</span><span className="text-xs opacity-70">5 Questions</span></button>
                            <button onClick={() => setSelectedMode(10)} type="button" className={modeBtnCls(selectedMode === 10)}><Trophy className="h-6 w-6 mb-2" /><span className="font-bold">Long Duel</span><span className="text-xs opacity-70">10 Questions</span></button>
                          </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                          <div className="space-y-2">
                            <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Category</label>
                            <div className="relative">
                              <select value={selectedCategory} onChange={e => setSelectedCategory(e.target.value)} className={selectCls}>
                                <option value="any">🎲 Any Category</option>
                                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                              </select>
                              {chevron}
                            </div>
                          </div>
                          <div className="space-y-2">
                            <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Difficulty</label>
                            <div className="relative">
                              <select value={selectedDifficulty} onChange={e => setSelectedDifficulty(e.target.value === 'any' ? 'any' : Number(e.target.value))} className={selectCls}>
                                <option value="any">✨ Any Difficulty</option>
                                <option value={0}>🟢 Easy</option>
                                <option value={1}>🟠 Medium</option>
                                <option value={2}>🔴 Hard</option>
                              </select>
                              {chevron}
                            </div>
                          </div>
                        </div>

                        <div className="space-y-2">
                          <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Question Type</label>
                          <div className="grid grid-cols-3 gap-3">
                            {(['any', 1, 2] as const).map(t => (
                              <button key={String(t)} onClick={() => setSelectedType(t)} type="button"
                                className={`py-2 px-4 rounded-xl border-2 text-sm font-bold transition-all ${selectedType === t ? 'border-primary bg-primary/10 text-primary' : 'border-border bg-input text-muted-foreground hover:border-primary/50'}`}>
                                {t === 'any' ? 'Any' : t === 1 ? 'Multiple Choice' : 'True/False'}
                              </button>
                            ))}
                          </div>
                        </div>

                        <button onClick={handleFindMatch} className="w-full h-16 text-xl font-black rounded-2xl bg-gradient-accent text-white shadow-lg hover:opacity-90 active:scale-[0.98] transition-all uppercase tracking-wider flex items-center justify-center gap-3">
                          <Swords className="h-6 w-6" />Find Opponent
                        </button>
                      </div>
                    )}
                  </div>
                )}

                {activeTab === 'create' && (
                  <form onSubmit={handleCreateRoom} className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-300">
                    <div className="space-y-4">
                      <div className="space-y-2">
                        <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Room Name</label>
                        <input type="text" value={roomName} onChange={e => setRoomName(e.target.value)} placeholder="My Awesome Room" className={selectCls} />
                      </div>
                      
                      <div className="flex items-center gap-4">
                        <label className="flex items-center gap-2 cursor-pointer">
                          <input type="checkbox" checked={isPrivate} onChange={e => setIsPrivate(e.target.checked)} className="h-5 w-5 accent-primary" />
                          <span className="text-sm font-bold text-foreground">Private Room</span>
                        </label>
                        {isPrivate && (
                          <input type="password" value={roomPassword} onChange={e => setRoomPassword(e.target.value)} placeholder="Password" className={`${selectCls} flex-1`} />
                        )}
                      </div>

                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Round Count</label>
                          <select value={selectedMode} onChange={e => setSelectedMode(Number(e.target.value))} className={selectCls}>
                            <option value={5}>5 Rounds</option>
                            <option value={10}>10 Rounds</option>
                          </select>
                        </div>
                        <div className="space-y-2">
                          <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Max Players</label>
                          <select value={roomMaxPlayers} onChange={e => setRoomMaxPlayers(Number(e.target.value))} className={selectCls}>
                            <option value={2}>2 Players</option>
                            <option value={3}>3 Players</option>
                            <option value={4}>4 Players</option>
                          </select>
                        </div>
                      </div>

                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Category</label>
                          <select value={selectedCategory} onChange={e => setSelectedCategory(e.target.value)} className={selectCls}>
                            <option value="any">🎲 Any Category</option>
                            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                          </select>
                        </div>
                        <div className="space-y-2">
                          <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Difficulty</label>
                          <select value={selectedDifficulty} onChange={e => setSelectedDifficulty(e.target.value === 'any' ? 'any' : Number(e.target.value))} className={selectCls}>
                            <option value="any">✨ Any Difficulty</option>
                            <option value={0}>🟢 Easy</option>
                            <option value={1}>🟠 Medium</option>
                            <option value={2}>🔴 Hard</option>
                          </select>
                        </div>
                      </div>

                      <div className="space-y-2">
                        <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Question Type</label>
                        <div className="grid grid-cols-3 gap-3">
                          {(['any', 1, 2] as const).map(t => (
                            <button key={String(t)} onClick={() => setSelectedType(t)} type="button"
                              className={`py-2 px-4 rounded-xl border-2 text-sm font-bold transition-all ${selectedType === t ? 'border-primary bg-primary/10 text-primary' : 'border-border bg-input text-muted-foreground hover:border-primary/50'}`}>
                              {t === 'any' ? 'Any' : t === 1 ? 'Multiple Choice' : 'True/False'}
                            </button>
                          ))}
                        </div>
                      </div>
                    </div>

                    <button type="submit" disabled={createLoading} className="w-full h-16 text-xl font-black rounded-2xl bg-gradient-accent text-white shadow-lg hover:opacity-90 transition-all uppercase tracking-wider flex items-center justify-center gap-3">
                      {createLoading ? <Loader2 className="h-6 w-6 animate-spin" /> : <><Plus className="h-6 w-6" />Create Room</>}
                    </button>
                  </form>
                )}

                {activeTab === 'browse' && (
                  <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-300">
                    {/* Join by Code */}
                    <form onSubmit={handleJoinByCode} className="space-y-2">
                      <label className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Join by Code</label>
                      <div className="flex gap-2">
                        <input 
                          type="text" 
                          value={joinCode} 
                          onChange={e => setJoinCode(e.target.value.toUpperCase())} 
                          placeholder="6-Letter Code" 
                          maxLength={6}
                          className={`${selectCls} uppercase tracking-widest font-mono flex-1`} 
                        />
                        <button 
                          type="submit" 
                          disabled={joinCodeLoading || joinCode.length !== 6} 
                          className="px-6 h-12 bg-primary text-white font-bold rounded-xl hover:opacity-90 transition-all disabled:opacity-50 flex items-center justify-center min-w-[100px]"
                        >
                          {joinCodeLoading ? <Loader2 className="h-5 w-5 animate-spin" /> : 'Join'}
                        </button>
                      </div>
                    </form>

                    <div className="flex items-center justify-between pt-4 border-t border-border">
                      <h3 className="text-sm font-bold uppercase tracking-widest text-muted-foreground">Available Rooms</h3>
                      <button onClick={fetchRooms} className="text-xs font-bold text-primary hover:underline">Refresh</button>
                    </div>

                    {browseLoading ? (
                      <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
                        <Loader2 className="h-8 w-8 animate-spin mb-2" />
                        <p className="text-sm">Fetching rooms...</p>
                      </div>
                    ) : rooms.length === 0 ? (
                      <div className="text-center py-12 text-muted-foreground">
                        <Users className="h-12 w-12 mx-auto mb-4 opacity-20" />
                        <p className="font-medium">No public rooms found.</p>
                        <p className="text-xs">Create your own and invite friends!</p>
                      </div>
                    ) : (
                      <div className="grid grid-cols-1 gap-3 max-h-[400px] overflow-y-auto pr-2">
                        {rooms.map(room => (
                          <div key={room.id} onClick={() => handleJoinRoom(room)} className="group bg-input hover:bg-card border border-border hover:border-primary/50 rounded-xl p-4 transition-all cursor-pointer flex items-center justify-between">
                            <div className="flex items-center gap-4">
                              <div className="h-12 w-12 rounded-lg bg-primary/10 flex items-center justify-center text-primary group-hover:bg-primary group-hover:text-white transition-all">
                                {room.isPrivate ? <Lock className="h-5 w-5" /> : <Users className="h-5 w-5" />}
                              </div>
                              <div>
                                <h4 className="font-bold text-foreground group-hover:text-primary transition-colors">{room.name}</h4>
                                <p className="text-xs text-muted-foreground flex items-center gap-2">
                                  <span>by {room.hostUsername}</span>
                                  <span>•</span>
                                  <span>{room.categoryName || 'All Categories'}</span>
                                </p>
                              </div>
                            </div>
                            <div className="flex items-center gap-3">
                              <div className="text-right hidden sm:block">
                                <p className="text-xs font-bold text-foreground capitalize">{room.difficulty || 'Any'}</p>
                                <p className="text-[10px] text-muted-foreground">{room.maxPlayers} Players Max</p>
                              </div>
                              <ChevronRight className="h-5 w-5 text-muted-foreground group-hover:text-primary group-hover:translate-x-1 transition-all" />
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          </section>

          {/* Stats and Friends Columns */}
          <div className="lg:col-span-1 space-y-6 flex flex-col">
            {/* My Stats */}
            <section className="space-y-6">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-primary/10 rounded-xl"><BarChart3 className="h-6 w-6 text-primary" /></div>
                <h2 className="text-3xl font-bold tracking-tight text-foreground">My Stats</h2>
              </div>
              <div className="bg-card border border-border rounded-2xl shadow-lg">
                <div className="p-8">
                  <div className="grid grid-cols-2 gap-4">
                    <div className="p-6 bg-input rounded-2xl flex flex-col items-center justify-center space-y-1"><span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground">Elo Rating</span><span className="text-4xl font-black text-primary">{user?.eloRating}</span></div>
                    <div className="p-6 bg-input rounded-2xl flex flex-col items-center justify-center space-y-1"><span className="text-[10px] font-black uppercase tracking-widest text-muted-foreground">Win Rate</span><span className="text-4xl font-black text-primary">{winRate}%</span></div>
                    <div className="p-6 bg-[#3fb950]/10 border border-[#3fb950]/20 rounded-2xl flex flex-col items-center justify-center space-y-1"><span className="text-[10px] font-black uppercase tracking-widest text-[#3fb950]/70">Total Wins</span><span className="text-3xl font-black text-[#3fb950]">{user?.totalWins}</span></div>
                    <div className="p-6 bg-destructive/10 border border-destructive/20 rounded-2xl flex flex-col items-center justify-center space-y-1"><span className="text-[10px] font-black uppercase tracking-widest text-destructive/70">Total Losses</span><span className="text-3xl font-black text-destructive">{user?.totalLosses}</span></div>
                  </div>
                  <div className="mt-6 p-6 bg-input border border-border rounded-2xl space-y-4">
                    <h4 className="font-bold flex items-center gap-2 text-foreground"><UserIcon className="h-4 w-4 text-primary" />Account Details</h4>
                    <div className="space-y-2 text-sm">
                      {[['Username', user?.username], ['Email', user?.email], ['Total Games', user?.totalGames]].map(([l, v]) => (
                        <div key={String(l)} className="flex justify-between"><span className="text-muted-foreground">{l}</span><span className="font-semibold text-foreground">{v}</span></div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            </section>

            {/* Arkadaşlık ve Davetler Paneli */}
            <section className="space-y-6">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-primary/10 rounded-xl"><Users className="h-6 w-6 text-primary" /></div>
                <h2 className="text-3xl font-bold tracking-tight text-foreground">Arkadaşlar</h2>
              </div>
              
              <div className="bg-card border border-border rounded-2xl shadow-lg p-6 space-y-6">
                {/* Arkadaş Ekleme Formu */}
                <form onSubmit={handleSendFriendRequest} className="space-y-2">
                  <label className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Arkadaş Ekle</label>
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={friendUsername}
                      onChange={e => setFriendUsername(e.target.value)}
                      placeholder="Kullanıcı adı girin..."
                      className="w-full h-10 px-3 rounded-xl bg-input border border-border text-foreground text-sm outline-none transition-all focus:border-primary"
                    />
                    <button type="submit" className="px-4 h-10 bg-primary hover:opacity-90 text-white rounded-xl font-bold text-xs transition-all flex items-center justify-center gap-1 min-w-[70px]">
                      <UserPlus className="h-4 w-4" /> Ekle
                    </button>
                  </div>
                </form>

                {/* Gelen İstekler */}
                {friendRequests.length > 0 && (
                  <div className="space-y-3 pt-4 border-t border-border">
                    <h3 className="text-xs font-black uppercase tracking-wider text-muted-foreground flex items-center gap-2">
                      <span className="relative flex h-2 w-2">
                        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-primary opacity-75"></span>
                        <span className="relative inline-flex rounded-full h-2 w-2 bg-primary"></span>
                      </span>
                      Bekleyen İstekler ({friendRequests.length})
                    </h3>
                    <div className="space-y-2 max-h-[150px] overflow-y-auto pr-1">
                      {friendRequests.map((req) => (
                        <div key={req.friendshipId} className="flex items-center justify-between p-2 bg-input rounded-xl border border-border">
                          <div className="flex flex-col">
                            <span className="text-sm font-bold text-foreground">{req.senderUsername}</span>
                            <span className="text-[10px] text-muted-foreground">{req.senderElo} Elo</span>
                          </div>
                          <div className="flex gap-1">
                            <button
                              onClick={() => handleAcceptFriendRequest(req.friendshipId)}
                              className="p-1.5 bg-[#3fb950]/15 hover:bg-[#3fb950]/30 text-[#3fb950] rounded-lg transition-all"
                              title="Kabul Et"
                            >
                              <UserCheck className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => handleDeclineFriendRequest(req.friendshipId)}
                              className="p-1.5 bg-destructive/15 hover:bg-destructive/30 text-destructive rounded-lg transition-all"
                              title="Reddet"
                            >
                              <UserX className="h-4 w-4" />
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Arkadaşlar Listesi */}
                <div className="space-y-3 pt-4 border-t border-border">
                  <div className="flex items-center justify-between">
                    <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                      Arkadaşlarım ({friends.length})
                    </h3>
                    <button onClick={fetchFriends} className="text-[10px] font-bold text-primary hover:underline">Yenile</button>
                  </div>

                  {friendsLoading ? (
                    <div className="flex items-center justify-center py-6 text-muted-foreground text-xs gap-2">
                      <Loader2 className="h-4 w-4 animate-spin text-primary" /> Yükleniyor...
                    </div>
                  ) : friends.length === 0 ? (
                    <div className="text-center py-6 text-muted-foreground text-xs">
                      Henüz arkadaşınız bulunmuyor.
                    </div>
                  ) : (
                    <div className="space-y-2 max-h-[300px] overflow-y-auto pr-1">
                      {friends.map((f) => (
                        <div key={f.friendshipId} className="flex items-center justify-between p-3 bg-input hover:bg-card border border-border hover:border-primary/20 rounded-xl transition-all group">
                          <div className="flex items-center gap-3">
                            <div className="relative">
                              <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold text-sm">
                                {f.friendUsername.substring(0, 2).toUpperCase()}
                              </div>
                              <span className={`absolute bottom-0 right-0 h-2.5 w-2.5 rounded-full border-2 border-card ${f.isOnline ? 'bg-[#3fb950]' : 'bg-muted-foreground'}`} />
                            </div>
                            <div className="flex flex-col">
                              <span className="text-sm font-bold text-foreground">{f.friendUsername}</span>
                              <span className="text-[10px] text-muted-foreground">{f.friendElo} Elo</span>
                            </div>
                          </div>
                          <button
                            onClick={() => handleRemoveFriend(f.friendId)}
                            className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg opacity-0 group-hover:opacity-100 transition-all"
                            title="Arkadaşı Sil"
                          >
                            <UserMinus className="h-4 w-4" />
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </section>
          </div>
        </div>
      </main>

      {/* Join Password Modal */}
      {joiningRoom && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-300">
          <div className="bg-card border border-border rounded-3xl w-full max-w-md shadow-2xl overflow-hidden scale-in duration-300">
            <div className="p-6 border-b border-border flex items-center justify-between">
              <h3 className="text-xl font-bold">Private Room</h3>
              <button onClick={() => setJoiningRoom(null)} className="text-muted-foreground hover:text-foreground">✕</button>
            </div>
            <div className="p-8 space-y-6">
              <div className="text-center space-y-2">
                <div className="h-16 w-16 bg-primary/10 text-primary rounded-full flex items-center justify-center mx-auto mb-4">
                  <Lock className="h-8 w-8" />
                </div>
                <h4 className="text-lg font-bold">Password Required</h4>
                <p className="text-sm text-muted-foreground">Please enter the password to join <strong>{joiningRoom.name}</strong>.</p>
              </div>
              <input type="password" value={joinPassword} onChange={e => setJoinPassword(e.target.value)} placeholder="Password" className={selectCls} autoFocus />
              <div className="flex gap-3">
                <button onClick={() => setJoiningRoom(null)} className="flex-1 h-12 rounded-xl bg-input font-bold hover:bg-card transition-all">Cancel</button>
                <button onClick={confirmJoin} className="flex-1 h-12 rounded-xl bg-primary text-white font-bold hover:opacity-90 shadow-lg shadow-primary/20 transition-all">Join Room</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Floating Invite Notification */}
      {inviteNotification && (
        <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 duration-300 max-w-sm w-full">
          <div className="bg-card/95 backdrop-blur-md border border-primary/30 rounded-2xl p-6 shadow-2xl space-y-4 relative overflow-hidden">
            {/* Animated neon border */}
            <div className="absolute top-0 left-0 w-full h-1 bg-gradient-accent animate-pulse" />
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 bg-primary/10 text-primary rounded-full flex items-center justify-center">
                <Mail className="h-5 w-5 animate-bounce text-primary" />
              </div>
              <div>
                <h4 className="font-bold text-foreground">Oyun Daveti!</h4>
                <p className="text-xs text-muted-foreground"><strong>{inviteNotification.senderUsername}</strong> seni odasına davet ediyor.</p>
              </div>
            </div>
            <div className="flex gap-2">
              <button 
                onClick={() => setInviteNotification(null)} 
                className="flex-1 py-2 rounded-xl bg-input text-xs font-bold hover:bg-card border border-border text-muted-foreground transition-all"
              >
                Kapat
              </button>
              <button 
                onClick={async () => {
                  const code = inviteNotification.roomCode;
                  setInviteNotification(null);
                  try {
                    await roomsApi.join(code, {});
                    router.push(`/room/${code}`);
                  } catch (e: any) {
                    toast.error("Odaya katılamadı. Oda dolmuş veya kapanmış olabilir.");
                  }
                }} 
                className="flex-1 py-2 rounded-xl bg-gradient-accent text-white text-xs font-bold hover:opacity-90 shadow-lg shadow-accent/20 active:scale-[0.98] transition-all"
              >
                Katıl
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

