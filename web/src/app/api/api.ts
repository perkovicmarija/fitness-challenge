import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { Activity, CoachFacts, CoachTurn, Dashboard, Leaderboard, Sport, User } from './models';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  users(): Observable<User[]> {
    return this.http.get<User[]>('/api/users');
  }

  register(firstName: string, lastName: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>('/api/users', { firstName, lastName });
  }

  sports(): Observable<Sport[]> {
    return this.http.get<Sport[]>('/api/sports');
  }

  leaderboard(): Observable<Leaderboard> {
    return this.http.get<Leaderboard>('/api/leaderboard');
  }

  dashboard(userId: string): Observable<Dashboard> {
    return this.http.get<Dashboard>(`/api/users/${userId}/dashboard`);
  }

  activities(userId: string): Observable<Activity[]> {
    return this.http.get<Activity[]>(`/api/users/${userId}/activities`);
  }

  logActivity(body: Record<string, unknown>): Observable<Activity> {
    return this.http.post<Activity>('/api/activities', body);
  }

  coachFacts(userId: string): Observable<CoachFacts> {
    return this.http.get<CoachFacts>(`/api/users/${userId}/coach`);
  }

  insight(userId: string, kind: 'rankChange' | 'weeklyRecap'): Observable<{ reply: string } | null> {
    return this.http
      .get<{ reply: string }>(`/api/users/${userId}/coach/insight/${kind}`, { observe: 'response' })
      .pipe(map(response => response.body));
  }

  coachChat(userId: string, messages: CoachTurn[]): Observable<{ reply: string }> {
    return this.http.post<{ reply: string }>(`/api/users/${userId}/coach/chat`, { messages });
  }
}
