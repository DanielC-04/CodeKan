export type TaskStatus = 'Todo' | 'InProgress' | 'Done';

export interface ProjectDto {
  id: string;
  name: string;
  repoOwner: string;
  repoName: string;
  createdAt: string;
}

export interface CreateProjectRequest {
  name: string;
  repoOwner: string;
  repoName: string;
  gitHubToken: string;
}

export interface TaskDto {
  id: string;
  projectId: string;
  title: string;
  status: TaskStatus;
  gitHubIssueNumber: number | null;
  createdAt: string;
  completedAt: string | null;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
}

export interface IssueUser {
  login: string;
  avatarUrl: string | null;
  profileUrl: string | null;
}

export interface IssueLabel {
  name: string;
  color: string | null;
}

export interface IssueDetails {
  taskId: string;
  issueNumber: number;
  title: string;
  description: string | null;
  state: string;
  stateReason: string | null;
  author: IssueUser | null;
  assignees: IssueUser[];
  labels: IssueLabel[];
  commentsCount: number;
  createdAt: string;
  updatedAt: string;
  url: string | null;
}

export interface IssueComment {
  id: number;
  body: string;
  author: IssueUser | null;
  createdAt: string;
  updatedAt: string;
  url: string | null;
}

export interface TaskUpdatedEvent {
  taskId: string;
  projectId: string;
  status: TaskStatus;
  completedAt: string | null;
  updatedFrom: string;
}
