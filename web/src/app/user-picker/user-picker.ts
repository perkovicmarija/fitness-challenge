import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../api/api';
import { CurrentUser } from '../api/current-user';
import { User } from '../api/models';

const OPENS_AS = 'Anna';

@Component({
  selector: 'app-user-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="picker">
      <select [ngModel]="current.id()" (ngModelChange)="current.select($event)" aria-label="Viewing as">
        <option [ngValue]="null">Choose a person…</option>
        @for (user of users(); track user.id) {
          <option [ngValue]="user.id">{{ user.firstName }} {{ user.lastName }}</option>
        }
      </select>

      @if (adding()) {
        <input [(ngModel)]="firstName" placeholder="First name" aria-label="First name" />
        <input [(ngModel)]="lastName" placeholder="Last name" aria-label="Last name" />
        <button type="button" (click)="register()" [disabled]="saving()">Save</button>
        <button type="button" class="ghost" (click)="adding.set(false)">Cancel</button>
      } @else {
        <button type="button" class="ghost" (click)="adding.set(true)">＋ New</button>
      }
    </div>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
  `,
  styles: `
    .picker { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }

    select, input, button {
      background: var(--surface-2);
      border: 1px solid var(--border);
      border-radius: var(--radius-control);
      padding: 7px 10px;
      font-size: 14px;
    }

    input { width: 118px; }

    button { cursor: pointer; }
    button:hover { border-color: var(--text-muted); }
    button.ghost { background: none; color: var(--text-secondary); }

    .error { color: var(--down); font-size: 13px; margin: 8px 0 0; }
  `,
})
export class UserPicker {
  private readonly api = inject(Api);
  readonly current = inject(CurrentUser);

  readonly users = signal<User[]>([]);
  readonly adding = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  firstName = '';
  lastName = '';

  constructor() {
    this.load();
  }

  register(): void {
    this.saving.set(true);
    this.error.set(null);

    this.api.register(this.firstName, this.lastName).subscribe({
      next: created => {
        this.firstName = '';
        this.lastName = '';
        this.adding.set(false);
        this.saving.set(false);
        this.current.select(created.id);
        this.load();
      },
      error: response => {

        this.error.set(response.status === 409
          ? 'Someone with that name is already registered.'
          : response.error?.title ?? 'Could not register that name.');
        this.saving.set(false);
      },
    });
  }

  private load(): void {
    this.api.users().subscribe(users => {
      this.users.set(users);

      if (!this.current.id() && users.length > 0) {
        this.current.select((users.find(user => user.firstName === OPENS_AS) ?? users[0]).id);
      }
    });
  }
}
