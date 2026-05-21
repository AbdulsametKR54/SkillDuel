'use client';

import { useEffect, useState } from 'react';
import { useGame } from '@/hooks/useGame';
import { useGameStore } from '@/lib/store';
import { usersApi } from '@/lib/api';
import { useParams, useRouter } from 'next/navigation';
import { cn } from '@/lib/utils';
import { Check, X } from 'lucide-react';

export default function DuelPage() {
  const params = useParams();
  const router = useRouter();
  const sessionId = params.sessionId as string;
  const {
    question, userId, players,
    currentRound, gamePhase, gameStatus,
    setUserId, setGamePhase, lastRoundResult, endResult,
    opponentDisconnected, resetGame,
  } = useGameStore();

  const { submitAnswer, joinGameGroup, sendEmote } = useGame();
  const [countdown, setCountdown] = useState(3);
  const [timer, setTimer] = useState(15);
  const [selectedOption, setSelectedOption] = useState<number | null>(null);
  const [roundStartTime, setRoundStartTime] = useState<number>(0);

  useEffect(() => {
    usersApi.me().then(res => setUserId(res.data.id)).catch(console.error);
  }, [setUserId]);

  useEffect(() => { if (sessionId) joinGameGroup(sessionId); }, [sessionId, joinGameGroup]);

  useEffect(() => {
    if (gamePhase === 'waiting') {
      setCountdown(3);
      const iv = setInterval(() => setCountdown(p => { if (p <= 1) { clearInterval(iv); return 0; } return p - 1; }), 1000);
      return () => clearInterval(iv);
    }
  }, [gamePhase]);

  useEffect(() => {
    if (gamePhase === 'active') {
      setTimer(15); setSelectedOption(null); setRoundStartTime(Date.now());
      const iv = setInterval(() => setTimer(p => { if (p <= 0) { clearInterval(iv); return 0; } return p - 1; }), 1000);
      return () => clearInterval(iv);
    }
  }, [gamePhase]);

  const handleAnswer = async (index: number) => {
    if (gamePhase !== 'active' || selectedOption !== null) return;
    setSelectedOption(index); setGamePhase('answered');
    await submitAnswer(index, Date.now() - roundStartTime);
  };

  const handleBackToLobby = () => { resetGame(); router.push('/lobby'); };

  const totalRounds = question?.totalRounds || 5;

  /* ── Waiting ──────────────────────────────────────────────── */
  if (gamePhase === 'waiting' || gameStatus === 'searching') {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-background text-center p-4">
        <div className="space-y-6 max-w-md w-full animate-in fade-in zoom-in duration-500">
          <h1 className="text-3xl font-bold text-foreground">Players found!</h1>
          <p className="text-muted-foreground text-lg">Game starting in...</p>
          <div className="text-8xl font-black text-gradient-accent tabular-nums">{countdown}</div>
          <div className="p-6 bg-card border border-border rounded-2xl flex flex-col gap-4">
            {players.map((p, i) => (
              <div key={p.id} className="flex justify-between items-center text-lg">
                <div className={cn("font-semibold italic", p.id === userId ? "text-primary" : "text-muted-foreground")}>{p.username}</div>
                <div className="text-sm font-bold text-muted-foreground bg-input px-2 py-1 rounded">Elo {p.elo}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  /* ── Game Over ─────────────────────────────────────────────── */
  if (gamePhase === 'game_over') {
    const isWinner = userId && endResult?.winnerId === userId;
    const isDraw = !endResult?.winnerId;

    const sortedPlayers = [...players].sort((a, b) => b.score - a.score);

    return (
      <div className="flex flex-col items-center justify-center min-h-screen bg-background text-center p-4 overflow-hidden">
        <div className="z-10 bg-card p-10 rounded-3xl border border-border shadow-2xl max-w-3xl w-full space-y-8 animate-in slide-in-from-bottom-10 duration-700">
          <div>
            <h1 className={cn('text-6xl font-black mb-2 tracking-tighter', isWinner ? 'text-primary' : isDraw ? 'text-foreground' : 'text-muted-foreground')}>
              {opponentDisconnected ? 'PLAYER LEFT' : isWinner ? 'VICTORY!' : isDraw ? 'DRAW!' : 'DEFEAT'}
            </h1>
            <p className="text-muted-foreground font-medium uppercase tracking-widest">
              {isWinner ? 'Champion' : isDraw ? 'Evenly Matched' : 'Better luck next time'}
            </p>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 items-center">
            {sortedPlayers.map((p, idx) => {
              const mine = p.id === userId;
              const pDelta = p.eloChange || 0;
              const pNewElo = p.newElo || p.elo;
              const pOldElo = pNewElo - pDelta;
              
              return (
                <div key={p.id} className={cn('flex flex-col items-center p-6 rounded-2xl border-2 transition-all', mine ? 'border-primary bg-primary/5 scale-105 z-10' : 'border-border bg-input opacity-80', idx === 0 && !isDraw ? 'border-[#3fb950] bg-[#3fb950]/5 ring-4 ring-[#3fb950]/20' : '')}>
                  <span className={cn('text-[10px] font-black uppercase tracking-widest mb-4', mine ? 'text-primary' : 'text-muted-foreground')}>{mine ? 'YOU' : p.username}</span>
                  <span className="text-4xl font-black mb-6 text-foreground">{p.score}</span>
                  <div className="flex flex-col items-center gap-1">
                    <div className="flex items-center gap-2 text-xs font-bold text-muted-foreground">
                      <span>{pOldElo}</span><span className="opacity-40">→</span><span className="text-foreground">{pNewElo}</span>
                    </div>
                    <span className={cn('text-xs font-black px-2 py-0.5 rounded-md', pDelta >= 0 ? 'bg-[#3fb950]/10 text-[#3fb950]' : 'bg-destructive/10 text-destructive')}>
                      {pDelta >= 0 ? `+${pDelta}` : pDelta}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>

          <button onClick={handleBackToLobby} className="w-full h-16 text-xl font-bold rounded-2xl bg-gradient-accent text-white shadow-lg hover:opacity-90 active:scale-[0.98] transition-all">
            BACK TO LOBBY
          </button>
        </div>
      </div>
    );
  }

  /* ── Active / Answered / Round Result ─────────────────────── */
  return (
    <div className="min-h-screen bg-background flex flex-col p-4 md:p-8 max-w-4xl mx-auto overflow-hidden">
      {/* Scoreboard */}
      <div className="bg-card p-6 rounded-3xl border border-border shadow-xl mb-8 relative pt-10">
        <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-gradient-accent text-white px-6 py-2 rounded-full text-sm font-black uppercase tracking-widest shadow-lg border-2 border-background">
          Round {currentRound} / {totalRounds}
        </div>

        <div className="flex justify-around items-center w-full gap-4">
          {players.map((p, idx) => (
             <div key={p.id} className="flex flex-col items-center flex-1 relative">
               {p.activeEmote && (
                 <div className="absolute -top-14 bg-card border border-border/80 px-3 py-1 rounded-full shadow-2xl text-3xl animate-bounce z-50 select-none">
                   {p.activeEmote}
                 </div>
               )}
               <div className="flex items-center gap-1.5 mb-1">
                 <span className={cn("text-[10px] font-black uppercase tracking-tighter truncate max-w-[80px]", p.id === userId ? "text-primary" : "text-muted-foreground")}>{p.id === userId ? 'YOU' : p.username}</span>
                 <span className="text-[10px] font-bold text-muted-foreground bg-input px-1.5 rounded">{p.correctCount}/{currentRound}</span>
               </div>
               <div className="text-3xl md:text-4xl font-black tabular-nums tracking-tighter text-foreground">{p.score}</div>
             </div>
          ))}
        </div>
      </div>

      {/* Question */}
      <div className="flex-grow flex flex-col justify-center space-y-10">
        <div className="text-center space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
          <div className="flex flex-wrap justify-center gap-2">
            <div className="px-4 py-1 rounded-full bg-primary/10 text-primary text-[10px] font-black uppercase tracking-widest border border-primary/20">
              {question?.categoryName || 'General'}
            </div>
            <div className="px-4 py-1 rounded-full bg-input text-muted-foreground text-[10px] font-black uppercase tracking-widest border border-border">
              {question?.difficulty || 'Medium'}
            </div>
            <div className="px-4 py-1 rounded-full bg-input text-muted-foreground text-[10px] font-black uppercase tracking-widest border border-border">
              {question?.questionType || 'Multiple'}
            </div>
          </div>
          <h2 className="text-2xl md:text-4xl font-black leading-tight px-4 tracking-tight text-foreground">
            {question?.text || 'Question loading...'}
          </h2>
        </div>

        {/* Answer Options */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 px-2">
          {question?.options.map((option, index) => {
            const isSelected = selectedOption === index;
            const isCorrect = lastRoundResult?.correctOptionIndex === index;
            const isWrong = isSelected && !isCorrect && gamePhase === 'round_result';

            let colorClass = 'bg-card hover:bg-input border-border hover:border-primary/50';
            if (gamePhase === 'round_result') {
              if (isCorrect) colorClass = 'bg-[#3fb950]/10 border-[#3fb950] text-[#3fb950] ring-4 ring-[#3fb950]/20 z-10 scale-[1.02]';
              else if (isWrong) colorClass = 'bg-destructive/10 border-destructive text-destructive ring-4 ring-destructive/20 z-10';
              else colorClass = 'opacity-40 grayscale-[0.5] border-border';
            } else if (gamePhase === 'answered' && isSelected) {
              colorClass = 'bg-primary/10 border-primary ring-4 ring-primary/20 scale-[0.98]';
            }

            return (
              <button
                key={index}
                disabled={gamePhase !== 'active'}
                onClick={() => handleAnswer(index)}
                className={cn('relative flex items-center p-6 text-left text-lg md:text-xl transition-all duration-300 rounded-2xl border-2 group', colorClass)}
              >
                <div className={cn(
                  'flex-shrink-0 w-10 h-10 rounded-xl border-2 flex items-center justify-center mr-4 text-sm font-black transition-colors',
                  gamePhase === 'round_result' && isCorrect ? 'bg-[#3fb950] border-[#3fb950] text-white' :
                  gamePhase === 'round_result' && isWrong ? 'bg-destructive border-destructive text-white' :
                  'bg-input border-border text-muted-foreground group-hover:border-primary/50 group-hover:text-primary',
                )}>
                  {String.fromCharCode(65 + index)}
                </div>
                <span className="font-semibold text-foreground">{option}</span>
                {gamePhase === 'round_result' && isCorrect && <div className="absolute top-1/2 -translate-y-1/2 right-4 text-[#3fb950]"><Check className="w-6 h-6" /></div>}
                {gamePhase === 'round_result' && isWrong && <div className="absolute top-1/2 -translate-y-1/2 right-4 text-destructive"><X className="w-6 h-6" /></div>}
              </button>
            );
          })}
        </div>
      </div>

      {/* Timer */}
      <div className="mt-auto pt-10 space-y-4">
        <div className="flex justify-center gap-2 py-2 px-3 bg-card/60 backdrop-blur-md border border-border/80 rounded-2xl max-w-sm mx-auto shadow-lg animate-in fade-in slide-in-from-bottom-2 duration-300">
          {['👍', '😂', '😮', '😢', '🔥', '👑', '🧠', '🤬'].map((emote) => (
            <button
              key={emote}
              onClick={() => sendEmote(emote)}
              className="w-10 h-10 flex items-center justify-center text-2xl hover:scale-125 active:scale-95 transition-all duration-200 select-none"
            >
              {emote}
            </button>
          ))}
        </div>

        <div className="flex justify-between items-end px-2">
          <div className="text-xs font-black text-muted-foreground uppercase opacity-50 tracking-widest">
            {gamePhase === 'active' ? 'Think fast!' : gamePhase === 'answered' ? 'Waiting for others...' : 'Next round coming up'}
          </div>
          <div className={cn('text-3xl font-black tabular-nums leading-none', timer <= 5 && gamePhase === 'active' ? 'text-destructive animate-pulse' : 'text-muted-foreground')}>
            {timer}
          </div>
        </div>
        <div className="h-3 bg-input rounded-full overflow-hidden border border-border">
          <div
            className={cn('h-full transition-all duration-1000 ease-linear', timer <= 5 ? 'bg-destructive shadow-[0_0_12px_rgba(239,68,68,0.5)]' : 'bg-gradient-accent')}
            style={{ width: `${(timer / 15) * 100}%`, transitionProperty: 'width, background-color' }}
          />
        </div>
      </div>
    </div>
  );
}
