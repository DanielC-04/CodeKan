import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { UiService } from '../../../../core/ui/ui.service';

@Component({
  selector: 'app-github-callback-page',
  templateUrl: './github-callback.page.html',
  styleUrl: './github-callback.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GitHubCallbackPage {
  private readonly authService = inject(AuthService);
  private readonly uiService = inject(UiService);
  private readonly router = inject(Router);

  constructor() {
    this.finishLogin();
  }

  private finishLogin(): void {
    this.authService.refresh().subscribe({
      next: () => {
        this.uiService.showSuccess('Sesion iniciada con GitHub.');
        void this.router.navigate(['/kanban']);
      },
      error: () => {
        this.uiService.showError('No fue posible completar el inicio de sesion con GitHub.');
        void this.router.navigate(['/auth/login'], { queryParams: { oauth: 'error' } });
      }
    });
  }
}
