import { createAction, props } from '@ngrx/store';
import { ProjectDto, TaskDto, TaskStatus, TaskUpdatedEvent } from '../models/kanban.models';

export const loadProjects = createAction('[Kanban] Load Projects');
export const loadProjectsSuccess = createAction(
  '[Kanban] Load Projects Success',
  props<{ projects: ProjectDto[] }>()
);
export const loadProjectsFailure = createAction(
  '[Kanban] Load Projects Failure',
  props<{ error: string }>()
);

export const createProject = createAction(
  '[Kanban] Create Project',
  props<{ name: string; repoOwner: string; repoName: string; gitHubToken: string }>()
);
export const createProjectSuccess = createAction(
  '[Kanban] Create Project Success',
  props<{ project: ProjectDto }>()
);
export const createProjectFailure = createAction(
  '[Kanban] Create Project Failure',
  props<{ error: string }>()
);

export const hydrateSelectedProject = createAction(
  '[Kanban] Hydrate Selected Project',
  props<{ projectId: string | null }>()
);

export const selectProject = createAction(
  '[Kanban] Select Project',
  props<{ projectId: string }>()
);

export const loadTasks = createAction('[Kanban] Load Tasks', props<{ projectId: string }>());
export const loadTasksSuccess = createAction('[Kanban] Load Tasks Success', props<{ tasks: TaskDto[] }>());
export const loadTasksFailure = createAction('[Kanban] Load Tasks Failure', props<{ error: string }>());

export const createTask = createAction('[Kanban] Create Task', props<{ title: string; description?: string }>());
export const createTaskSuccess = createAction('[Kanban] Create Task Success', props<{ task: TaskDto }>());
export const createTaskFailure = createAction('[Kanban] Create Task Failure', props<{ error: string }>());

export const moveTaskOptimistic = createAction(
  '[Kanban] Move Task Optimistic',
  props<{ taskId: string; newStatus: TaskStatus; previousStatus: TaskStatus; previousCompletedAt: string | null }>()
);
export const moveTaskSuccess = createAction('[Kanban] Move Task Success', props<{ task: TaskDto }>());
export const moveTaskFailure = createAction(
  '[Kanban] Move Task Failure',
  props<{ taskId: string; previousStatus: TaskStatus; previousCompletedAt: string | null; error: string }>()
);

export const taskUpdatedFromRealtime = createAction(
  '[Kanban] Task Updated From Realtime',
  props<{ event: TaskUpdatedEvent }>()
);
