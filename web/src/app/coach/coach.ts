import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../api/api';
import { CurrentUser } from '../api/current-user';
import { CoachEffort, CoachFacts, CoachTurn } from '../api/models';

@Component({
  selector: 'app-coach',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink],
  templateUrl: './coach.html',
  styleUrl: './coach.scss',
})
export class Coach {
  private readonly api = inject(Api);
  readonly current = inject(CurrentUser);

  readonly loading = signal(false);
  readonly facts = signal<CoachFacts | null>(null);

  readonly messages = signal<CoachTurn[]>([]);
  readonly draft = signal('');
  readonly thinking = signal(false);
  readonly error = signal<string | null>(null);

  readonly unavailable = signal(false);

  readonly full = computed(() => this.messages().length >= MAX_MESSAGES);

  readonly alternative = computed<CoachEffort | null>(() => {
    const facts = this.facts();

    if (!facts?.recommended) {
      return null;
    }

    const second = facts.sports.filter(sport => sport.sessions > 0)[1];

    return second
      ? facts.waysToPassNextRank.find(way => way.label === second.label) ?? null
      : null;
  });

  private readonly starters = computed(() => {
    const facts = this.facts();

    if (!facts || facts.rank === null) {
      return ['How does scoring work?', "What's the quickest way to score?", 'Build me a 3-day plan'];
    }

    const common = ['Build me a 3-day plan', 'I only have 30 minutes a day'];

    return facts.pointsToPassNextRank === null
      ? ['How do I stay ahead?', 'How big is my lead?', ...common]
      : ['How do I catch up?', "What's my best activity?", ...(facts.rank > 2 ? ['Can I reach #1?'] : []), ...common];
  });

  readonly maxLength = MAX_MESSAGE_LENGTH;

  readonly suggestions = computed(() => {
    const asked = new Set(
      this.messages().filter(turn => turn.role === 'user').map(turn => turn.text));

    return this.starters().filter(starter => !asked.has(starter));
  });

  constructor() {
    effect(() => {
      const userId = this.current.id();

      untracked(() => {
        this.messages.set([]);
        this.error.set(null);
        this.thinking.set(false);

        if (!userId) {
          this.facts.set(null);
          return;
        }

        this.loading.set(true);
        this.api.coachFacts(userId).subscribe({
          next: facts => {
            this.facts.set(facts);
            this.loading.set(false);
          },
          error: () => {
            this.facts.set(null);
            this.loading.set(false);
          },
        });
      });
    });
  }

  send(text: string): void {
    const userId = this.current.id();
    const question = text.trim();

    if (!userId || !question || this.thinking() || this.full()) {
      return;
    }

    const conversation: CoachTurn[] = [...this.messages(), { role: 'user', text: question }];

    this.messages.set(conversation);
    this.draft.set('');
    this.thinking.set(true);
    this.error.set(null);

    this.api.coachChat(userId, conversation).subscribe({
      next: ({ reply }) => {
        this.messages.update(messages => [...messages, { role: 'assistant', text: reply }]);
        this.thinking.set(false);
      },
      error: (failure: HttpErrorResponse) => {
        this.thinking.set(false);

        this.messages.update(messages => messages.slice(0, -1));
        this.draft.set(question);

        if (failure.status === 503) {
          this.unavailable.set(true);
        } else {
          this.error.set('The coach could not answer just now. Try again.');
        }
      },
    });
  }
}

const MAX_MESSAGES = 12;
const MAX_MESSAGE_LENGTH = 500;
