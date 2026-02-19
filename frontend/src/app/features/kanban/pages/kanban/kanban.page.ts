import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, effect, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { finalize } from 'rxjs';
import { SignalrService } from '../../../../core/signalr/signalr.service';
import { UiService } from '../../../../core/ui/ui.service';
import { KanbanApiService } from '../../data/kanban-api.service';
import { TaskCardComponent } from '../../components/task-card/task-card.component';
import { IssueComment, IssueDetails, IssueLabel, TaskDto, TaskStatus, TaskUpdatedEvent } from '../../models/kanban.models';
import {
  createProject,
  createTask,
  loadProjects,
  moveTaskOptimistic,
  selectProject,
  taskUpdatedFromRealtime
} from '../../store/kanban.actions';
import {
  selectDoneTasks,
  selectInProgressTasks,
  selectKanbanLoading,
  selectProjects,
  selectSelectedProjectId,
  selectTodoTasks
} from '../../store/kanban.selectors';

@Component({
  selector: 'app-kanban-page',
  imports: [
    ReactiveFormsModule,
    CommonModule,
    FormsModule,
    DragDropModule,
    NzButtonModule,
    NzDrawerModule,
    NzTagModule,
    NzAvatarModule,
    NzFormModule,
    NzSelectModule,
    NzInputModule,
    NzSpinModule,
    NzEmptyModule,
    TaskCardComponent
  ],
  templateUrl: './kanban.page.html',
  styleUrl: './kanban.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KanbanPage implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);
  private readonly signalr = inject(SignalrService);
  private readonly ui = inject(UiService);
  private readonly kanbanApi = inject(KanbanApiService);

  readonly projects = this.store.selectSignal(selectProjects);
  readonly selectedProjectId = this.store.selectSignal(selectSelectedProjectId);
  readonly todoTasks = this.store.selectSignal(selectTodoTasks);
  readonly inProgressTasks = this.store.selectSignal(selectInProgressTasks);
  readonly doneTasks = this.store.selectSignal(selectDoneTasks);
  readonly isLoading = this.store.selectSignal(selectKanbanLoading);

  readonly todoListId = 'todo-list';
  readonly inProgressListId = 'in-progress-list';
  readonly doneListId = 'done-list';
  readonly connectedDropLists = [this.todoListId, this.inProgressListId, this.doneListId];

  readonly issueDrawerOpen = signal(false);
  readonly selectedTask = signal<TaskDto | null>(null);
  readonly issueDetails = signal<IssueDetails | null>(null);
  readonly issueComments = signal<IssueComment[]>([]);
  readonly issueDetailsLoading = signal(false);
  readonly issueCommentsLoading = signal(false);
  readonly issueDetailsError = signal<string | null>(null);
  readonly issueCommentsError = signal<string | null>(null);
  readonly activeIssueTab = signal<'summary' | 'meta' | 'comments'>('summary');
  readonly taskLabelsById = signal<Record<string, IssueLabel[]>>({});
  readonly issueDetailsCacheByTaskId = signal<Record<string, IssueDetails>>({});
  readonly issueCommentsCacheByTaskId = signal<Record<string, IssueComment[]>>({});

  private readonly taskLabelsLoading = new Set<string>();

  showProjectForm = false;

  readonly projectForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    repoOwner: ['', [Validators.required, Validators.maxLength(100)]],
    repoName: ['', [Validators.required, Validators.maxLength(100)]],
    gitHubToken: ['', [Validators.required, Validators.maxLength(4000)]]
  });

  readonly taskForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', [Validators.maxLength(20000)]]
  });

  private readonly realtimeHandler = (eventData: unknown): void => {
    const event = this.parseTaskUpdatedEvent(eventData);
    if (!event) {
      return;
    }

    if (event.projectId === this.selectedProjectId()) {
      this.invalidateTaskCache(event.taskId);
      this.store.dispatch(taskUpdatedFromRealtime({ event }));
    }
  };

  private readonly syncTaskLabelsEffect = effect(() => {
    const allTasks = [...this.todoTasks(), ...this.inProgressTasks(), ...this.doneTasks()];
    for (const task of allTasks) {
      if (!task.gitHubIssueNumber) {
        continue;
      }

      const cached = this.taskLabelsById()[task.id];
      if (cached || this.taskLabelsLoading.has(task.id)) {
        continue;
      }

      this.loadTaskLabels(task.id);
    }
  });

  ngOnInit(): void {
    this.store.dispatch(loadProjects());

    void this.signalr
      .startConnection()
      .then(() => this.signalr.onTaskUpdated(this.realtimeHandler))
      .catch(() => this.ui.showError('No se pudo conectar a tiempo real.'));
  }

  ngOnDestroy(): void {
    this.signalr.offTaskUpdated(this.realtimeHandler);
  }

  changeProject(projectId: string): void {
    if (!projectId) {
      return;
    }

    this.taskLabelsById.set({});
    this.issueDetailsCacheByTaskId.set({});
    this.issueCommentsCacheByTaskId.set({});
    this.taskLabelsLoading.clear();
    this.store.dispatch(selectProject({ projectId }));
  }

  toggleProjectForm(): void {
    this.showProjectForm = !this.showProjectForm;
  }

  submitProject(): void {
    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    const { name, repoOwner, repoName, gitHubToken } = this.projectForm.getRawValue();
    this.store.dispatch(
      createProject({
        name: name.trim(),
        repoOwner: repoOwner.trim(),
        repoName: repoName.trim(),
        gitHubToken: gitHubToken.trim()
      })
    );

    this.projectForm.reset();
    this.showProjectForm = false;
  }

  submitTask(): void {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }

    const title = this.taskForm.controls.title.value.trim();
    const description = this.taskForm.controls.description.value.trim();

    this.store.dispatch(
      createTask({
        title,
        description: description.length > 0 ? description : undefined
      })
    );

    this.taskForm.reset();
  }

  drop(event: CdkDragDrop<TaskDto[]>, newStatus: TaskStatus): void {
    const task = event.item.data as TaskDto | undefined;
    if (!task || task.status === newStatus) {
      return;
    }

    this.invalidateTaskCache(task.id);

    this.store.dispatch(
      moveTaskOptimistic({
        taskId: task.id,
        newStatus,
        previousStatus: task.status,
        previousCompletedAt: task.completedAt
      })
    );
  }

  openIssueDetails(task: TaskDto): void {
    this.selectedTask.set(task);
    this.issueDrawerOpen.set(true);
    this.activeIssueTab.set('summary');
    this.issueDetailsError.set(null);
    this.issueCommentsError.set(null);

    const cachedDetails = this.issueDetailsCacheByTaskId()[task.id];
    const cachedComments = this.issueCommentsCacheByTaskId()[task.id];

    if (cachedDetails) {
      this.issueDetails.set(cachedDetails);
    }
    else {
      this.issueDetails.set(null);
      this.loadIssueDetails(task.id);
    }

    if (cachedComments) {
      this.issueComments.set(cachedComments);
    }
    else {
      this.issueComments.set([]);
      this.loadIssueComments(task.id);
    }
  }

  closeIssueDetails(): void {
    this.issueDrawerOpen.set(false);
  }

  setIssueTab(tab: 'summary' | 'meta' | 'comments'): void {
    this.activeIssueTab.set(tab);
  }

  hasIssueDescription(): boolean {
    const description = this.issueDetails()?.description;
    return typeof description === 'string' && description.trim().length > 0;
  }

  issueLabelStyle(color: string | null): Record<string, string> {
    if (!color) {
      return {};
    }

    return {
      borderColor: `#${color}`,
      color: `#${color}`
    };
  }

  taskLabels(taskId: string): IssueLabel[] {
    return this.taskLabelsById()[taskId] ?? [];
  }

  hasIssueDetailsError(): boolean {
    return !!this.issueDetailsError();
  }

  hasIssueCommentsError(): boolean {
    return !!this.issueCommentsError();
  }

  private loadIssueDetails(taskId: string): void {
    this.issueDetailsLoading.set(true);
    this.kanbanApi
      .getIssueDetails(taskId)
      .pipe(finalize(() => this.issueDetailsLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.issueDetailsError.set(null);
          this.issueDetails.set(response.data);
          this.issueDetailsCacheByTaskId.update((current) => ({
            ...current,
            [taskId]: response.data
          }));
          this.taskLabelsById.update((current) => ({
            ...current,
            [taskId]: response.data.labels ?? []
          }));
        },
        error: () => {
          this.issueDetailsError.set('No se pudieron cargar los detalles del issue.');
          this.issueDetails.set(null);
        }
      });
  }

  private loadTaskLabels(taskId: string): void {
    this.taskLabelsLoading.add(taskId);
    this.kanbanApi
      .getIssueDetails(taskId)
      .pipe(finalize(() => this.taskLabelsLoading.delete(taskId)))
      .subscribe({
        next: (response) => {
          this.issueDetailsCacheByTaskId.update((current) => ({
            ...current,
            [taskId]: response.data
          }));
          this.taskLabelsById.update((current) => ({
            ...current,
            [taskId]: response.data.labels ?? []
          }));
        },
        error: () => {
          this.taskLabelsById.update((current) => ({
            ...current,
            [taskId]: []
          }));
        }
      });
  }

  private loadIssueComments(taskId: string): void {
    this.issueCommentsLoading.set(true);
    this.kanbanApi
      .getIssueComments(taskId)
      .pipe(finalize(() => this.issueCommentsLoading.set(false)))
      .subscribe({
        next: (response) => {
          this.issueCommentsError.set(null);
          this.issueComments.set(response.data);
          this.issueCommentsCacheByTaskId.update((current) => ({
            ...current,
            [taskId]: response.data
          }));
        },
        error: () => {
          this.issueCommentsError.set('No se pudieron cargar los comentarios.');
          this.issueComments.set([]);
        }
      });
  }

  private invalidateTaskCache(taskId: string): void {
    this.issueDetailsCacheByTaskId.update((current) => {
      const { [taskId]: _removed, ...rest } = current;
      return rest;
    });

    this.issueCommentsCacheByTaskId.update((current) => {
      const { [taskId]: _removed, ...rest } = current;
      return rest;
    });

    this.taskLabelsById.update((current) => {
      const { [taskId]: _removed, ...rest } = current;
      return rest;
    });
  }

  private parseTaskUpdatedEvent(eventData: unknown): TaskUpdatedEvent | null {
    if (!eventData || typeof eventData !== 'object') {
      return null;
    }

    const value = eventData as Record<string, unknown>;
    const taskId = typeof value['taskId'] === 'string' ? value['taskId'] : null;
    const projectId = typeof value['projectId'] === 'string' ? value['projectId'] : null;
    const status =
      value['status'] === 'Todo' || value['status'] === 'InProgress' || value['status'] === 'Done'
        ? value['status']
        : null;
    const completedAt = typeof value['completedAt'] === 'string' ? value['completedAt'] : null;
    const updatedFrom = typeof value['updatedFrom'] === 'string' ? value['updatedFrom'] : 'unknown';

    if (!taskId || !projectId || !status) {
      return null;
    }

    return {
      taskId,
      projectId,
      status,
      completedAt,
      updatedFrom
    };
  }
}
