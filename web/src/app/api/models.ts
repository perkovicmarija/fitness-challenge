export interface User {
  id: string;
  firstName: string;
  lastName: string;
}

export interface Sport {
  sport: string | null;
  label: string;
  metric: 'distance' | 'duration' | 'count';
  unit: string;
  pointsPerUnit: number;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  firstName: string;
  lastName: string;
  totalPoints: number;

  previousRank: number | null;
  rankDelta: number | null;

  pointsToOvertake: number | null;
}

export interface ChallengeSummary {
  name: string;
  startsOn: string;
  endsOn: string;

  day: number;
  totalDays: number;

  daysLeft: number;
  hasEnded: boolean;
}

export interface Leaderboard {

  challenge: ChallengeSummary | null;

  activeToday: number;
  entries: LeaderboardEntry[];
}

export interface DayTotal {
  date: string;
  points: number;
}

export interface SportTotal {
  sport: string | null;
  points: number;
  share: number;
}

export interface SportShare {
  sport: string | null;
  share: number;
}

export interface Dashboard {
  userId: string;
  totalPoints: number;
  currentStreakDays: number;
  perDay: DayTotal[];
  perSport: SportTotal[];
  fieldAverage: SportShare[];
}

export interface Activity {
  id: string;
  userId: string;
  occurredAt: string;
  localDate: string;
  sport: string | null;
  distance: number | null;
  duration: string | null;
  steps: number | null;
  points: number;
}

export interface CoachEffort {

  sport: string | null;
  label: string;
  quantity: number;
  unit: string;
}

export interface CoachSport {
  label: string;
  points: number;
  sessions: number;
  averagePoints: number;
}

export interface RankChange {
  previousRank: number;
  currentRank: number;
  myPointsThisWeek: number;

  mover: string | null;
  moverPointsThisWeek: number | null;
}

export interface CoachFacts {
  userId: string;

  challenge: ChallengeSummary | null;

  rank: number | null;
  competitors: number;
  totalPoints: number;

  pointsToPassNextRank: number | null;
  pointsToTakeTheLead: number | null;

  rankChange: RankChange | null;
  activeDaysLastSevenDays: number;
  bestDay: DayTotal | null;
  leadOverNextRank: number | null;

  nextRankName: string | null;
  activitiesLastSevenDays: number;
  pointsLastSevenDays: number;
  averageWeeklyPoints: number;
  daysTrainedLastFourWeeks: number;

  bestWeekday: string | null;
  waysToPassNextRank: CoachEffort[];

  recommended: CoachEffort | null;
  sports: CoachSport[];
}

export interface CoachTurn {
  role: 'user' | 'assistant';
  text: string;
}
