import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { AuthService } from '../../../../core/auth/auth.service';
import { UiService } from '../../../../core/ui/ui.service';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, NzFormModule, NzInputModule, NzButtonModule, NzCardModule],
  templateUrl: './register.page.html',
  styleUrl: './register.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterPage {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly uiService = inject(UiService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.authService.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.uiService.showSuccess('Account created successfully.');
        void this.router.navigate(['/kanban']);
      }
    });
  }
}
