import { useEffect } from 'react';
import signalRService from '@/lib/signalr';
import { useGameStore } from '@/lib/store';

export const useGame = () => {
  const { 
    setQuestion, 
    setCurrentRound, 
    setGameStatus,
    setGamePhase,
    setLastRoundResult,
    setEndResult,
    setOpponentDisconnected,
    updatePlayerStates,
    triggerPlayerEmote,
    sessionId,
    userId
  } = useGameStore();

  useEffect(() => {
    if (!sessionId) return;

    const handleRoundStarted = (data: any) => {
      setCurrentRound(data.roundNumber);
      setQuestion(data.question);
      setGameStatus('playing');
      setGamePhase('active');
      setLastRoundResult(null);
    };

    const handleRoundResult = (data: any) => {
      if (data.players && Array.isArray(data.players)) {
        updatePlayerStates(data.players.map((p: any) => ({
          id: p.playerId,
          score: p.score,
          correctCount: p.correctCount,
          isCorrect: p.isCorrect
        })));
      }
      setLastRoundResult({
        correctOptionIndex: data.correctOptionIndex,
        playerResults: data.players
      });
      setGamePhase('round_result');
    };

    const handleGameEnded = (data: any) => {
      setEndResult({
        winnerId: data.winnerId,
        winnerUsername: data.winnerUsername,
      });
      
      if (data.players && Array.isArray(data.players)) {
        updatePlayerStates(data.players.map((p: any) => ({
          id: p.playerId,
          score: p.finalScore,
          newElo: p.newRating,
          eloChange: p.ratingDelta
        })));
      }

      setGamePhase('game_over');
      setGameStatus('ended');
    };

    const handleOpponentDisconnected = (data: any) => {
      // Backend maçı bitirmesi gerekiyorsa GameEnded event'i gönderir.
      // Sadece 1 kişi düşünce direkt oyunu bitirme mantığını kaldırıyoruz.
      // İsteğe bağlı olarak burada sadece bir toast gösterilebilir.
    };

    const handleEmoteReceived = (playerId: string, emote: string) => {
      triggerPlayerEmote(playerId, emote);
    };

    let isMounted = true;

    const setupListeners = async () => {
      await signalRService.ensureConnected();
      if (!isMounted) return;

      signalRService.onRoundStarted(handleRoundStarted);
      signalRService.onRoundResult(handleRoundResult);
      signalRService.onGameEnded(handleGameEnded);
      signalRService.onOpponentDisconnected(handleOpponentDisconnected);
      signalRService.onEmoteReceived(handleEmoteReceived);
    };

    setupListeners();

    return () => {
      isMounted = false;
      signalRService.removeHandlers();
    };
  }, [
    sessionId, 
    userId, 
    setQuestion, 
    setCurrentRound, 
    updatePlayerStates,
    triggerPlayerEmote,
    setGameStatus, 
    setGamePhase, 
    setLastRoundResult, 
    setEndResult, 
    setOpponentDisconnected
  ]);

  const submitAnswer = async (answerIndex: number, timeMs: number) => {
    try {
      await signalRService.ensureConnected();
      const connection = signalRService.getConnection();
      if (connection && sessionId) {
        await connection.invoke('SubmitAnswer', sessionId, answerIndex, timeMs);
      }
    } catch (error) {
      console.error('Failed to submit answer:', error);
    }
  };

  const joinGameGroup = async (id: string) => {
    try {
      await signalRService.ensureConnected();
      const connection = signalRService.getConnection();
      if (connection) {
        await connection.invoke('JoinGameGroup', id);
      }
    } catch (error) {
      console.error('Failed to join game group:', error);
    }
  };

  const sendEmote = async (emote: string) => {
    try {
      if (sessionId) {
        await signalRService.sendEmote(sessionId, emote);
      }
    } catch (error) {
      console.error('Failed to send emote:', error);
    }
  };

  return {
    submitAnswer,
    joinGameGroup,
    sendEmote
  };
};
