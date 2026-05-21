import { useEffect, useState } from 'react';
import signalRService from '@/lib/signalr';
import { useGameStore } from '@/lib/store';
import { useRouter } from 'next/navigation';

export const useMatchmaking = () => {
  const [isSearching, setIsSearching] = useState(false);
  const { setGameStatus, setSessionId, setUserId, setPlayers } = useGameStore();
  const router = useRouter();

  useEffect(() => {
    const handleMatchFound = (data: any) => {
      console.log('Match Found!', data);
      setSessionId(data.sessionId);
      setUserId(data.myId);
      if (data.players) {
        setPlayers(data.players.map((p: any) => ({
          id: p.id,
          username: p.username,
          elo: p.elo,
          score: 0,
          correctCount: 0
        })));
      }
      setGameStatus('playing');
      router.push(`/duel/${data.sessionId}`);
    };

    const handleMatchmakingTimeout = (data: any) => {
      console.log('Matchmaking Timeout:', data);
      setIsSearching(false);
      setGameStatus('idle');
      // You could also trigger a toast/notification here if available
      alert(data.message || 'No opponent found. Please try again.');
    };

    const handleGameError = (data: any) => {
      console.log('Game Error:', data);
      setIsSearching(false);
      setGameStatus('idle');
      alert(data.message || 'An error occurred during game setup.');
    };

    signalRService.startConnection().then(() => {
      signalRService.onMatchFound(handleMatchFound);
      signalRService.onMatchmakingTimeout(handleMatchmakingTimeout);
      signalRService.onGameError(handleGameError);
    });

    return () => {
      signalRService.removeHandlers();
    };
  }, [setGameStatus, setSessionId, setPlayers, setUserId, router]);

  const startMatchmasking = async (mode: number, categoryId?: string, difficulty?: number, questionType?: number) => {
    setIsSearching(true);
    setGameStatus('searching');

    try {
      await signalRService.ensureConnected();
      const connection = signalRService.getConnection();
      if (connection) {
        // GameMode: Short = 5, Long = 10
        await connection.invoke('JoinMatchmaking', mode, categoryId, difficulty, questionType);
      }
    } catch (error) {
      console.error('Failed to start matchmaking:', error);
      setIsSearching(false);
      setGameStatus('idle');
    }
  };

  const cancelMatchmaking = async () => {
    try {
      await signalRService.ensureConnected();
      const connection = signalRService.getConnection();
      if (connection) {
        setIsSearching(false);
        setGameStatus('idle');
        await connection.invoke('LeaveMatchmaking');
      }
    } catch (error) {
      console.error('Failed to cancel matchmaking:', error);
    }
  };

  return {
    isSearching,
    startMatchmasking,
    cancelMatchmaking,
  };
};
