import { ArrowLeft, History, ChevronLeft, ChevronRight, TrendingUp, TrendingDown, Minus } from 'lucide-react';
import Link from 'next/link';
import { format } from 'date-fns';
import { cookies } from 'next/headers';

interface MatchHistoryEntry {
  sessionId: string;
  opponentUsername: string;
  result: 'Win' | 'Loss' | 'Draw';
  myScore: number;
  opponentScore: number;
  eloChange: number;
  playedAt: string;
}

const resultStyle = {
  Win:  'text-[#3fb950] bg-[#3fb950]/10 border border-[#3fb950]/30',
  Loss: 'text-destructive bg-destructive/10 border border-destructive/30',
  Draw: 'text-muted-foreground bg-input border border-border',
};

async function getHistory(page: number, pageSize: number) {
  const token = cookies().get('token')?.value;
  const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
  
  const headers = { 'Content-Type': 'application/json' };
  if (token) {
    (headers as any)['Authorization'] = `Bearer ${token}`;
  }

  const res = await fetch(`${baseUrl}/api/Games/history?page=${page}&pageSize=${pageSize}`, { headers, cache: 'no-store' });
  const data = res.ok ? await res.json() : null;
  return data?.data || [];
}

export default async function HistoryPage({ searchParams }: { searchParams: { page?: string } }) {
  const page = parseInt(searchParams.page || '1', 10) || 1;
  const pageSize = 10;
  
  const history: MatchHistoryEntry[] = await getHistory(page, pageSize);

  const getEloIcon = (change: number) => {
    if (change > 0) return <TrendingUp className="h-4 w-4 text-[#3fb950]" />;
    if (change < 0) return <TrendingDown className="h-4 w-4 text-destructive" />;
    return <Minus className="h-4 w-4 text-muted-foreground" />;
  };

  const btnCls = 'flex items-center gap-1 px-4 py-2 rounded-xl bg-card border border-border text-sm font-bold text-muted-foreground hover:text-foreground hover:border-primary/40 transition-all disabled:opacity-40 disabled:cursor-not-allowed';

  return (
    <div className="min-h-screen bg-background p-6 md:p-12">
      <div className="max-w-4xl mx-auto space-y-8">
        {/* Header */}
        <div className="flex items-center gap-4">
          <Link href="/lobby">
            <button className="p-2 rounded-xl bg-card border border-border text-muted-foreground hover:text-foreground hover:border-primary/40 transition-all">
              <ArrowLeft className="h-5 w-5" />
            </button>
          </Link>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-xl"><History className="h-6 w-6 text-primary" /></div>
            <h1 className="text-4xl font-black tracking-tighter text-foreground uppercase">Match History</h1>
          </div>
        </div>

        {/* Rows */}
        <div className="space-y-3">
          {history.length > 0 ? (
            <>
              {history.map(entry => (
                <div key={entry.sessionId} className="bg-card border border-border rounded-2xl hover:border-primary/30 transition-all overflow-hidden">
                  <div className="flex flex-col md:flex-row items-center justify-between p-6 gap-6">
                    {/* Result badge + opponent */}
                    <div className="flex items-center gap-6 w-full md:w-auto">
                      <div className={`px-4 py-2 rounded-xl font-black text-sm uppercase tracking-tight ${resultStyle[entry.result]}`}>
                        {entry.result}
                      </div>
                      <div className="flex flex-col">
                        <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Opponent</span>
                        <span className="font-bold text-lg text-foreground">{entry.opponentUsername}</span>
                      </div>
                    </div>

                    {/* Score */}
                    <div className="flex items-center justify-center gap-8 flex-1">
                      <div className="flex flex-col items-center">
                        <span className="text-4xl font-black text-foreground">{entry.myScore}</span>
                        <span className="text-[10px] font-bold uppercase text-muted-foreground">Me</span>
                      </div>
                      <div className="text-2xl font-black text-border">VS</div>
                      <div className="flex flex-col items-center">
                        <span className="text-4xl font-black text-foreground">{entry.opponentScore}</span>
                        <span className="text-[10px] font-bold uppercase text-muted-foreground">Them</span>
                      </div>
                    </div>

                    {/* Elo + Date */}
                    <div className="flex items-center gap-8 w-full md:w-auto justify-between md:justify-end">
                      <div className="flex flex-col items-end">
                        <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Elo Change</span>
                        <div className="flex items-center gap-2">
                          {getEloIcon(entry.eloChange)}
                          <span className={`font-black text-lg ${entry.eloChange > 0 ? 'text-[#3fb950]' : entry.eloChange < 0 ? 'text-destructive' : 'text-muted-foreground'}`}>
                            {entry.eloChange > 0 ? `+${entry.eloChange}` : entry.eloChange}
                          </span>
                        </div>
                      </div>
                      <div className="flex flex-col items-end">
                        <span className="text-[10px] font-bold text-muted-foreground uppercase tracking-widest">Date</span>
                        <span className="text-sm font-semibold text-foreground">{format(new Date(entry.playedAt), 'MMM d, HH:mm')}</span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              {/* Pagination */}
              <div className="flex items-center justify-center gap-4 mt-8">
                <Link href={page > 1 ? `?page=${page - 1}` : '#'}>
                  <button disabled={page === 1} className={btnCls}>
                    <ChevronLeft className="h-4 w-4" />Previous
                  </button>
                </Link>
                <div className="bg-card border border-border rounded-xl px-4 py-2 font-bold text-sm text-foreground">Page {page}</div>
                <Link href={history.length >= pageSize ? `?page=${page + 1}` : '#'}>
                  <button disabled={history.length < pageSize} className={btnCls}>
                    Next<ChevronRight className="h-4 w-4" />
                  </button>
                </Link>
              </div>
            </>
          ) : (
            <div className="bg-card border border-dashed border-border rounded-2xl p-20 flex flex-col items-center justify-center text-center space-y-4">
              <div className="p-4 bg-input rounded-full"><History className="h-12 w-12 text-muted-foreground/50" /></div>
              <h3 className="text-2xl font-bold text-foreground">No Games Played Yet</h3>
              <p className="text-muted-foreground max-w-xs">Your battle history will appear here once you&apos;ve completed your first duel!</p>
              <Link href="/lobby">
                <button className="mt-4 px-6 h-11 rounded-xl bg-gradient-accent text-white font-bold hover:opacity-90 transition-all">Start Your First Duel</button>
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
