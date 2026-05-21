import { Trophy, Medal, Crown, ArrowLeft, Swords } from 'lucide-react';
import Link from 'next/link';
import { cookies } from 'next/headers';

interface LeaderboardEntry {
  rank: number;
  userId: string;
  username: string;
  eloRating: number;
  totalWins: number;
  totalLosses: number;
  winRate: number;
}

async function getLeaderboardAndUser() {
  const token = cookies().get('token')?.value;
  const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
  
  const headers = { 'Content-Type': 'application/json' };
  if (token) {
    (headers as any)['Authorization'] = `Bearer ${token}`;
  }

  const [lbRes, userRes] = await Promise.all([
    fetch(`${baseUrl}/api/Leaderboard?limit=50`, { headers, cache: 'no-store' }),
    fetch(`${baseUrl}/api/Users/me`, { headers, cache: 'no-store' })
  ]);

  const lbData = lbRes.ok ? await lbRes.json() : [];
  const userData = userRes.ok ? await userRes.json() : null;

  return {
    entries: lbData.data || [],
    currentUserId: userData?.data?.id || null
  };
}

export default async function LeaderboardPage() {
  const { entries, currentUserId } = await getLeaderboardAndUser();

  const getRankIcon = (rank: number) => {
    if (rank === 1) return <Crown className="h-5 w-5 text-primary fill-primary" />;
    if (rank === 2) return <Medal className="h-5 w-5 text-[#8b949e] fill-[#8b949e]" />;
    if (rank === 3) return <Medal className="h-5 w-5 text-[#f59e0b] fill-[#f59e0b]" />;
    return <span className="font-bold text-muted-foreground text-sm">{rank}</span>;
  };

  const getRowBg = (rank: number, isMe: boolean) => {
    if (isMe) return 'bg-primary/8 border-l-2 border-l-primary';
    if (rank === 1) return 'bg-primary/5';
    if (rank === 2) return 'bg-[#8b949e]/5';
    if (rank === 3) return 'bg-[#f59e0b]/5';
    return 'hover:bg-input/60 transition-colors';
  };

  return (
    <div className="min-h-screen bg-background p-6 md:p-12">
      <div className="max-w-5xl mx-auto space-y-8">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/lobby">
              <button className="p-2 rounded-xl bg-card border border-border text-muted-foreground hover:text-foreground hover:border-primary/40 transition-all">
                <ArrowLeft className="h-5 w-5" />
              </button>
            </Link>
            <div className="flex items-center gap-3">
              <div className="p-2 bg-primary/10 rounded-xl"><Trophy className="h-6 w-6 text-primary" /></div>
              <h1 className="text-4xl font-black tracking-tighter text-foreground uppercase">Global Leaderboard</h1>
            </div>
          </div>
          <div className="hidden md:flex items-center gap-2 bg-card border border-border rounded-2xl px-4 py-2">
            <Swords className="h-4 w-4 text-primary" />
            <span className="font-bold text-foreground text-sm">Season 1</span>
          </div>
        </div>

        {/* Table */}
        <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-xl">
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-input border-b border-border">
                  {['Rank', 'Player', 'Elo', 'W / L', 'Win Rate'].map((h, i) => (
                    <th key={h} className={`p-4 md:p-5 font-black uppercase tracking-widest text-xs text-muted-foreground ${i === 2 || i === 3 ? 'text-center' : i === 4 ? 'text-right' : ''}`}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {entries.length > 0 ? entries.map((entry: LeaderboardEntry) => {
                  const isMe = entry.userId === currentUserId;
                  return (
                    <tr key={entry.userId} className={getRowBg(entry.rank, isMe)}>
                      <td className="p-4 md:p-5">
                        <div className="flex items-center justify-center w-8">{getRankIcon(entry.rank)}</div>
                      </td>
                      <td className="p-4 md:p-5">
                        <div className="flex items-center gap-3">
                          <div className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold ${isMe ? 'bg-gradient-accent text-white' : 'bg-input text-muted-foreground border border-border'}`}>
                            {entry.username.substring(0, 2).toUpperCase()}
                          </div>
                          <span className="truncate max-w-[120px] md:max-w-none font-medium text-foreground">
                            {entry.username}
                            {isMe && <span className="ml-2 text-[10px] bg-primary/20 text-primary px-1.5 py-0.5 rounded font-black">Me</span>}
                          </span>
                        </div>
                      </td>
                      <td className="p-4 md:p-5 text-center">
                        <span className="font-black text-primary">{entry.eloRating}</span>
                      </td>
                      <td className="p-4 md:p-5 text-center text-sm font-medium">
                        <span className="text-[#3fb950]">{entry.totalWins}</span>
                        <span className="mx-1 text-muted-foreground">/</span>
                        <span className="text-destructive">{entry.totalLosses}</span>
                      </td>
                      <td className="p-4 md:p-5 text-right">
                        <span className={`px-2 py-1 rounded-lg text-xs font-bold ${entry.winRate >= 50 ? 'bg-[#3fb950]/10 text-[#3fb950]' : 'bg-destructive/10 text-destructive'}`}>
                          {entry.winRate}%
                        </span>
                      </td>
                    </tr>
                  );
                }) : (
                  <tr>
                    <td colSpan={5} className="p-12 text-center text-muted-foreground font-medium">
                      No legends found yet. Be the first to reach the top!
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
