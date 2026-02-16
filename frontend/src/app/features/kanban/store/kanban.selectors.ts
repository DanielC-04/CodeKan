import { createFeatureSelector, createSelector } from '@ngrx/store';
import { KanbanState } from './kanban.state';

export const selectKanbanState = createFeatureSelector<KanbanState>('kanban');

export const selectProjects = createSelector(selectKanbanState, (state) => state.projects);
export const selectSelectedProjectId = createSelector(
  selectKanbanState,
  (state) => state.selectedProjectId
);
export const selectTasks = createSelector(selectKanbanState, (state) => state.tasks);

export const selectTodoTasks = createSelector(selectTasks, (tasks) =>
  tasks.filter((task) => task.status === 'Todo')
);
export const selectInProgressTasks = createSelector(selectTasks, (tasks) =>
  tasks.filter((task) => task.status === 'InProgress')
);
export const selectDoneTasks = createSelector(selectTasks, (tasks) =>
  tasks.filter((task) => task.status === 'Done')
);

export const selectKanbanLoading = createSelector(
  selectKanbanState,
  (state) => state.loadingProjects || state.creatingProject || state.loadingTasks || state.creatingTask
);
export const selectKanbanError = createSelector(selectKanbanState, (state) => state.error);
