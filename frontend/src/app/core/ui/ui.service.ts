import { inject, Injectable } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';

@Injectable({ providedIn: 'root' })
export class UiService {
  private readonly message = inject(NzMessageService);

  showSuccess(message: string): void {
    this.message.success(message);
  }

  showError(message: string): void {
    this.message.error(message);
  }
}
