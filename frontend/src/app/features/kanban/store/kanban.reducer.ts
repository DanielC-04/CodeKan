import { createReducer, on } from '@ngrx/store';
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
  selectProject,
  taskUpdatedFromRealtime
} from './kanban.actions';
import { initialKanbanState } from './kanban.state';

export const kanbanReducer = createReducer(
  initialKanbanState,
  on(createProject, (state) => ({ ...state, creatingProject: true, error: null })),
  on(createProjectSuccess, (state, { project }) => ({
    ...state,
    creatingProject: false,
    projects: [...state.projects, project],
    selectedProjectId: project.id,
    tasks: []
  })),
  on(createProjectFailure, (state, { error }) => ({ ...state, creatingProject: false, error })),
  on(loadProjects, (state) => ({ ...state, loadingProjects: true, error: null })),
  on(loadProjectsSuccess, (state, { projects }) => ({ ...state, loadingProjects: false, projects })),
  on(loadProjectsFailure, (state, { error }) => ({ ...state, loadingProjects: false, error })),
  on(hydrateSelectedProject, (state, { projectId }) => ({ ...state, selectedProjectId: projectId })),
  on(selectProject, (state, { projectId }) => ({ ...state, selectedProjectId: projectId, tasks: [] })),
  on(loadTasks, (state) => ({ ...state, loadingTasks: true, error: null })),
  on(loadTasksSuccess, (state, { tasks }) => ({ ...state, loadingTasks: false, tasks })),
  on(loadTasksFailure, (state, { error }) => ({ ...state, loadingTasks: false, error })),
  on(createTask, (state) => ({ ...state, creatingTask: true, error: null })),
  on(createTaskSuccess, (state, { task }) => ({ ...state, creatingTask: false, tasks: [...state.tasks, task] })),
  on(createTaskFailure, (state, { error }) => ({ ...state, creatingTask: false, error })),
  on(moveTaskOptimistic, (state, { taskId, newStatus }) => ({
    ...state,
    tasks: state.tasks.map((task) =>
      task.id === taskId
        ? {
            ...task,
            status: newStatus,
            completedAt: newStatus === 'Done' ? new Date().toISOString() : null
          }
        : task
    )
  })),
  on(moveTaskSuccess, (state, { task }) => ({
    ...state,
    tasks: state.tasks.map((item) => (item.id === task.id ? task : item))
  })),
  on(moveTaskFailure, (state, { taskId, previousStatus, previousCompletedAt, error }) => ({
    ...state,
    error,
    tasks: state.tasks.map((task) =>
      task.id === taskId
        ? {
            ...task,
            status: previousStatus,
            completedAt: previousCompletedAt
          }
        : task
    )
  })),
  on(taskUpdatedFromRealtime, (state, { event }) => ({
    ...state,
    tasks: state.tasks.map((task) =>
      task.id === event.taskId ? { ...task, status: event.status, completedAt: event.completedAt } : task
    )
  }))
);
