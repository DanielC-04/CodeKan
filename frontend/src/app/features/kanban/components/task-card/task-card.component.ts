import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { IssueLabel, TaskDto } from '../../models/kanban.models';

@Component({
  selector: 'app-task-card',
  imports: [CommonModule, NzCardModule, NzTagModule],
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskCardComponent {
  readonly task = input.required<TaskDto>();
  readonly labels = input<IssueLabel[]>([]);
  readonly open = output<void>();

  issueTagColor(): string {
    const status = this.task().status;
    if (status === 'Done') {
      return 'green';
    }

    if (status === 'InProgress') {
      return 'blue';
    }

    return 'gold';
  }

  openDetails(): void {
    this.open.emit();
  }

  visibleLabels(): IssueLabel[] {
    return this.labels().slice(0, 2);
  }

  hiddenLabelsCount(): number {
    return Math.max(this.labels().length - 2, 0);
  }
}
