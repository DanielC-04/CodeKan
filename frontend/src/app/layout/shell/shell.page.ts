import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { AuthSessionStore } from '../../core/auth/auth-session.store';

@Component({
  selector: 'app-shell-page',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './shell.page.html',
  styleUrl: './shell.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly authSessionStore = inject(AuthSessionStore);

  readonly userEmail = computed(() => this.authSessionStore.user()?.email ?? 'Unknown user');

  logout(): void {
    this.authService.revoke().subscribe({
      next: () => {
        void this.router.navigate(['/auth/login']);
      }
    });
  }
}
