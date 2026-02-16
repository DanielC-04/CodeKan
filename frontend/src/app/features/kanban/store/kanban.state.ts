import { ProjectDto, TaskDto } from '../models/kanban.models';

export interface KanbanState {
  projects: ProjectDto[];
  selectedProjectId: string | null;
  tasks: TaskDto[];
  loadingProjects: boolean;
  creatingProject: boolean;
  loadingTasks: boolean;
  creatingTask: boolean;
  error: string | null;
}

export const initialKanbanState: KanbanState = {
  projects: [],
  selectedProjectId: null,
  tasks: [],
  loadingProjects: false,
  creatingProject: false,
  loadingTasks: false,
  creatingTask: false,
  error: null
};
