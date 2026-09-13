import { Injectable, signal } from '@angular/core';
import { LeaderboardEntry } from './models';

const STORAGE_KEY = 'fitness-challenge.user';

@Injectable({ providedIn: 'root' })
export class CurrentUser {
  readonly id = signal<string | null>(localStorage.getItem(STORAGE_KEY));

  rowIn(entries: LeaderboardEntry[]): LeaderboardEntry | undefined {
    return entries.find(entry => entry.userId === this.id());
  }

  select(id: string | null): void {
    this.id.set(id);

    if (id) {
      localStorage.setItem(STORAGE_KEY, id);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}
