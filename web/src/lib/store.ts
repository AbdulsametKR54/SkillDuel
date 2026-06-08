import { create } from 'zustand';

export interface PlayerState {
  id: string;
  username: string;
  elo: number;
  score: number;
  correctCount: number;
  oldElo?: number;
  newElo?: number;
  eloChange?: number;
  activeEmote?: string;
}

interface GameState {
  sessionId: string | null;
  userId: string | null;
  players: PlayerState[];
  currentRound: number;
  question: {
    id: string;
    text: string;
    options: string[];
    difficulty: string;
    totalRounds: number;
    durationSeconds: number;
    categoryName: string;
    questionType: string;
  } | null;
  gameStatus: 'idle' | 'searching' | 'playing' | 'ended';
  gamePhase: 'preparing' | 'waiting' | 'active' | 'answered' | 'round_result' | 'calculating' | 'game_over';
  lastRoundResult: {
    correctOptionIndex: number;
    playerResults?: any[];
  } | null;
  endResult: {
    winnerId?: string;
    winnerUsername?: string;
  } | null;
  opponentDisconnected: boolean;
  
  // Actions
  setUserId: (id: string | null) => void;
  setSessionId: (id: string | null) => void;
  setPlayers: (players: PlayerState[]) => void;
  updatePlayerStates: (updates: Partial<PlayerState>[]) => void;
  triggerPlayerEmote: (playerId: string, emote: string) => void;
  setCurrentRound: (round: number) => void;
  setQuestion: (question: GameState['question']) => void;
  setGameStatus: (status: GameState['gameStatus']) => void;
  setGamePhase: (phase: GameState['gamePhase']) => void;
  setLastRoundResult: (result: GameState['lastRoundResult']) => void;
  setEndResult: (result: GameState['endResult']) => void;
  setOpponentDisconnected: (disconnected: boolean) => void;
  resetGame: () => void;
}

export const useGameStore = create<GameState>((set) => ({
  sessionId: null,
  userId: null,
  players: [],
  currentRound: 0,
  question: null,
  gameStatus: 'idle',
  gamePhase: 'preparing',
  lastRoundResult: null,
  endResult: null,
  opponentDisconnected: false,

  setUserId: (id) => set({ userId: id }),
  setSessionId: (id) => set({ sessionId: id }),
  setPlayers: (players) => set({ players }),
  updatePlayerStates: (updates) => set((state) => {
    const newPlayers = state.players.map(p => {
      const update = updates.find(u => u.id?.toLowerCase() === p.id.toLowerCase());
      return update ? { ...p, ...update } : p;
    });
    return { players: newPlayers };
  }),
  triggerPlayerEmote: (playerId, emote) => {
    set((state) => {
      const newPlayers = state.players.map(p => 
        p.id.toLowerCase() === playerId.toLowerCase() ? { ...p, activeEmote: emote } : p
      );
      return { players: newPlayers };
    });
    setTimeout(() => {
      set((state) => {
        const newPlayers = state.players.map(p => 
          p.id.toLowerCase() === playerId.toLowerCase() ? { ...p, activeEmote: undefined } : p
        );
        return { players: newPlayers };
      });
    }, 2500);
  },
  setCurrentRound: (round) => set({ currentRound: round }),
  setQuestion: (question) => set({ question }),
  setGameStatus: (status) => set({ gameStatus: status }),
  setGamePhase: (phase) => set({ gamePhase: phase }),
  setLastRoundResult: (result) => set({ lastRoundResult: result }),
  setEndResult: (result) => set({ endResult: result }),
  setOpponentDisconnected: (disconnected) => set({ opponentDisconnected: disconnected as any }),
  resetGame: () => set({
    userId: null,
    sessionId: null,
    players: [],
    currentRound: 0,
    question: null,
    gameStatus: 'idle',
    gamePhase: 'preparing',
    lastRoundResult: null,
    endResult: null,
    opponentDisconnected: false
  }),
}));
