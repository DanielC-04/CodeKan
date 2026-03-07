import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { EMPTY, catchError, concat, filter, map, of, switchMap, tap, withLatestFrom } from 'rxjs';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { LocalStoreService } from '../../../core/storage/local-store.service';
import { UiService } from '../../../core/ui/ui.service';
import { KanbanApiService } from '../data/kanban-api.service';
import { TaskDto } from '../models/kanban.models';
import {
  createProject,
  createProjectFailure,
  createProjectSuccess,
  createTask,
  createTaskFailure,
  createTaskSuccess,
  deleteProject,
  deleteProjectFailure,
  deleteProjectSuccess,
  hydrateSelectedProject,
  importIssues,
  importIssuesFailure,
  importIssuesSuccess,
  loadProjects,
  loadProjectsFailure,
  loadProjectsSuccess,
  loadTasks,
  loadTasksFailure,
  loadTasksSuccess,
  moveTaskFailure,
  moveTaskOptimistic,
  moveTaskSuccess,
  selectProject
} from './kanban.actions';
import { selectProjects, selectSelectedProjectId, selectTasks } from './kanban.selectors';

const ACTIVE_PROJECT_KEY_PREFIX = 'devboard.activeProjectId';
const KANBAN_SNAPSHOT_KEY_PREFIX = 'devboard.kanban';

@Injectable()
export class KanbanEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(KanbanApiService);
  private readonly ui = inject(UiService);
  private readonly localStore = inject(LocalStoreService);
  private readonly store = inject(Store);
  private readonly authSessionStore = inject(AuthSessionStore);

  readonly loadProjects$ = createEffect(() =>
    this.actions$.pipe(
      ofType(loadProjects),
      switchMap(() =>
        this.api.getProjects().pipe(
          map((response) => loadProjectsSuccess({ projects: response.data })),
          catchError((error) =>
            of(loadProjectsFailure({ error: error.error?.message ?? 'No se pudieron cargar proyectos.' }))
          )
        )
      )
    )
  );

  readonly createProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(createProject),
      switchMap(({ name, repoOwner, repoName }) =>
        this.api.createProject({ name, repoOwner, repoName }).pipe(
          map((response) => createProjectSuccess({ project: response.data })),
          catchError((error) =>
            of(createProjectFailure({ error: error.error?.message ?? 'No se pudo crear el proyecto.' }))
          )
        )
      )
    )
  );

  readonly selectCreatedProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(createProjectSuccess),
      map(({ project }) => selectProject({ projectId: project.id }))
    )
  );

  readonly deleteProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(deleteProject),
      switchMap(({ projectId }) =>
        this.api.deleteProject(projectId).pipe(
          map(() => deleteProjectSuccess({ projectId })),
          catchError((error) =>
            of(deleteProjectFailure({ error: error.error?.message ?? 'No se pudo eliminar el proyecto.' }))
          )
        )
      )
    )
  );

  readonly selectProjectAfterDelete$ = createEffect(() =>
    this.actions$.pipe(
      ofType(deleteProjectSuccess),
      withLatestFrom(this.store.select(selectSelectedProjectId), this.store.select(selectProjects)),
      map(([, selectedProjectId, projects]) => {
        if (selectedProjectId) {
          return null;
        }

        const nextProject = projects[0];
        return nextProject ? selectProject({ projectId: nextProject.id }) : hydrateSelectedProject({ projectId: null });
      }),
      filter((action): action is ReturnType<typeof selectProject> | ReturnType<typeof hydrateSelectedProject> => action !== null)
    )
  );

  readonly initSelectedProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(loadProjectsSuccess),
      map(({ projects }) => {
        const activeProjectKey = this.getActiveProjectKey();
        const storedProjectId = activeProjectKey ? this.localStore.get<string>(activeProjectKey) : null;
        if (storedProjectId && projects.some((project) => project.id === storedProjectId)) {
          return selectProject({ projectId: storedProjectId });
        }

        if (projects.length > 0) {
          return selectProject({ projectId: projects[0].id });
        }

        return hydrateSelectedProject({ projectId: null });
      })
    )
  );

  readonly selectProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(selectProject),
      map(({ projectId }) => loadTasks({ projectId }))
    )
  );

  readonly persistSelectedProject$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(selectProject),
        tap(({ projectId }) => {
          const activeProjectKey = this.getActiveProjectKey();
          if (!activeProjectKey) {
            return;
          }

          this.localStore.set(activeProjectKey, projectId);
        })
      ),
    { dispatch: false }
  );

  readonly loadTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(loadTasks),
      switchMap(({ projectId }) => {
        const snapshotKey = this.getTaskSnapshotKey(projectId);
        const snapshot = snapshotKey ? this.localStore.get<TaskDto[]>(snapshotKey) : null;
        const hydrateFromCache$ = snapshot && snapshot.length > 0 ? of(loadTasksSuccess({ tasks: snapshot })) : EMPTY;

        const loadFromApi$ = this.api.getTasks(projectId).pipe(
          map((response) => loadTasksSuccess({ tasks: response.data })),
          catchError((error) =>
            of(loadTasksFailure({ error: error.error?.message ?? 'No se pudieron cargar tareas.' }))
          )
        );

        return concat(hydrateFromCache$, loadFromApi$);
      })
    )
  );

  readonly createTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(createTask),
      withLatestFrom(this.store.select(selectSelectedProjectId)),
      filter(([, projectId]) => !!projectId),
      switchMap(([{ title, description }, projectId]) =>
        this.api.createTask(projectId!, { title, description }).pipe(
          map((response) => createTaskSuccess({ task: response.data })),
          catchError((error) =>
            of(createTaskFailure({ error: error.error?.message ?? 'No se pudo crear la tarea.' }))
          )
        )
      )
    )
  );

  readonly importIssues$ = createEffect(() =>
    this.actions$.pipe(
      ofType(importIssues),
      withLatestFrom(this.store.select(selectSelectedProjectId)),
      filter(([, projectId]) => !!projectId),
      switchMap(([, projectId]) =>
        this.api.importIssues(projectId!).pipe(
          map((response) => importIssuesSuccess({ result: response.data })),
          catchError((error) =>
            of(importIssuesFailure({ error: error.error?.message ?? 'No se pudieron importar issues.' }))
          )
        )
      )
    )
  );

  readonly moveTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(moveTaskOptimistic),
      switchMap(({ taskId, newStatus, previousStatus, previousCompletedAt }) =>
        this.api.updateTaskStatus(taskId, newStatus).pipe(
          map((response) => moveTaskSuccess({ task: response.data })),
          catchError((error) =>
            of(
              moveTaskFailure({
                taskId,
                previousStatus,
                previousCompletedAt,
                error: error.error?.message ?? 'No se pudo mover la tarea.'
              })
            )
          )
        )
      )
    )
  );

  readonly notifyProjectSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(createProjectSuccess),
        tap(() => this.ui.showSuccess('Proyecto creado correctamente.'))
      ),
    { dispatch: false }
  );

  readonly notifyProjectDeleteSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(deleteProjectSuccess),
        tap(() => this.ui.showSuccess('Proyecto eliminado correctamente.'))
      ),
    { dispatch: false }
  );

  readonly notifyCreateSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(createTaskSuccess),
        tap(() => this.ui.showSuccess('Tarea creada correctamente.'))
      ),
    { dispatch: false }
  );

  readonly notifyImportSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(importIssuesSuccess),
        tap(({ result }) =>
          this.ui.showSuccess(`Importados ${result.imported} de ${result.total} issues. (${result.skipped} omitidos)`)
        )
      ),
    { dispatch: false }
  );

  readonly notifyImportFailure$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(importIssuesFailure),
        tap(({ error }) => this.ui.showError(error))
      ),
    { dispatch: false }
  );

  readonly reloadTasksAfterImport$ = createEffect(() =>
    this.actions$.pipe(
      ofType(importIssuesSuccess),
      withLatestFrom(this.store.select(selectSelectedProjectId)),
      filter(([, projectId]) => !!projectId),
      map(([, projectId]) => loadTasks({ projectId: projectId! }))
    )
  );

  readonly persistTasksSnapshot$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(loadTasksSuccess, createTaskSuccess, moveTaskSuccess, importIssuesSuccess),
        withLatestFrom(this.store.select(selectSelectedProjectId), this.store.select(selectTasks)),
        tap(([, projectId, tasks]) => {
          if (!projectId) {
            return;
          }

          const snapshotKey = this.getTaskSnapshotKey(projectId);
          if (!snapshotKey) {
            return;
          }

          this.localStore.set(snapshotKey, tasks);
        })
      ),
    { dispatch: false }
  );

  readonly clearDeletedProjectCache$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(deleteProjectSuccess),
        tap(({ projectId }) => {
          const snapshotKey = this.getTaskSnapshotKey(projectId);
          if (snapshotKey) {
            this.localStore.remove(snapshotKey);
          }

          const activeProjectKey = this.getActiveProjectKey();
          if (!activeProjectKey) {
            return;
          }

          const persistedProject = this.localStore.get<string>(activeProjectKey);
          if (persistedProject === projectId) {
            this.localStore.remove(activeProjectKey);
          }
        })
      ),
    { dispatch: false }
  );

  private getActiveProjectKey(): string | null {
    const userId = this.authSessionStore.user()?.id;
    return userId ? `${ACTIVE_PROJECT_KEY_PREFIX}.${userId}` : null;
  }

  private getTaskSnapshotKey(projectId: string): string | null {
    const userId = this.authSessionStore.user()?.id;
    return userId ? `${KANBAN_SNAPSHOT_KEY_PREFIX}.${userId}.${projectId}` : null;
  }
}
