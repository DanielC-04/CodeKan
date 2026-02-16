import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { EMPTY, catchError, concat, filter, map, of, switchMap, tap, withLatestFrom } from 'rxjs';
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
  hydrateSelectedProject,
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
import { selectSelectedProjectId, selectTasks } from './kanban.selectors';

const ACTIVE_PROJECT_KEY = 'devboard.activeProjectId';

@Injectable()
export class KanbanEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(KanbanApiService);
  private readonly ui = inject(UiService);
  private readonly localStore = inject(LocalStoreService);
  private readonly store = inject(Store);

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
      switchMap(({ name, repoOwner, repoName, gitHubToken }) =>
        this.api.createProject({ name, repoOwner, repoName, gitHubToken }).pipe(
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

  readonly initSelectedProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(loadProjectsSuccess),
      map(({ projects }) => {
        const storedProjectId = this.localStore.get<string>(ACTIVE_PROJECT_KEY);
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
        tap(({ projectId }) => this.localStore.set(ACTIVE_PROJECT_KEY, projectId))
      ),
    { dispatch: false }
  );

  readonly loadTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(loadTasks),
      switchMap(({ projectId }) => {
        const snapshot = this.localStore.get<TaskDto[]>(`devboard.kanban.${projectId}`);
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

  readonly notifyCreateSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(createTaskSuccess),
        tap(() => this.ui.showSuccess('Tarea creada correctamente.'))
      ),
    { dispatch: false }
  );

  readonly persistTasksSnapshot$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(loadTasksSuccess, createTaskSuccess, moveTaskSuccess),
        withLatestFrom(this.store.select(selectSelectedProjectId), this.store.select(selectTasks)),
        tap(([, projectId, tasks]) => {
          if (!projectId) {
            return;
          }

          this.localStore.set(`devboard.kanban.${projectId}`, tasks);
        })
      ),
    { dispatch: false }
  );
}
