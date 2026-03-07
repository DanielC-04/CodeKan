import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  CreateProjectRequest,
  CreateTaskRequest,
  IssueComment,
  IssueDetails,
  ImportIssuesResult,
  ProjectDto,
  TaskDto,
  TaskStatus
} from '../models/kanban.models';

@Injectable({ providedIn: 'root' })
export class KanbanApiService {
  constructor(private readonly api: ApiService) {}

  getProjects(): Observable<ApiResponse<ProjectDto[]>> {
    return this.api.get<ProjectDto[]>('/api/projects');
  }

  createProject(request: CreateProjectRequest): Observable<ApiResponse<ProjectDto>> {
    return this.api.post<ProjectDto>('/api/projects', request);
  }

  getGitHubInstallUrl(projectId: string): Observable<ApiResponse<{ installUrl: string }>> {
    return this.api.post<{ installUrl: string }>('/api/github-app/install-url', { projectId });
  }

  deleteProject(projectId: string): Observable<void> {
    return this.api.delete<void>(`/api/projects/${projectId}`);
  }

  getTasks(projectId: string): Observable<ApiResponse<TaskDto[]>> {
    return this.api.get<TaskDto[]>(`/api/projects/${projectId}/tasks`);
  }

  createTask(projectId: string, request: CreateTaskRequest): Observable<ApiResponse<TaskDto>> {
    return this.api.post<TaskDto>(`/api/projects/${projectId}/tasks`, request);
  }

  updateTaskStatus(taskId: string, status: TaskStatus): Observable<ApiResponse<TaskDto>> {
    return this.api.patch<TaskDto>(`/api/tasks/${taskId}/status`, { status });
  }

  getIssueDetails(taskId: string): Observable<ApiResponse<IssueDetails>> {
    return this.api.get<IssueDetails>(`/api/tasks/${taskId}/issue-details`);
  }

  getIssueComments(taskId: string): Observable<ApiResponse<IssueComment[]>> {
    return this.api.get<IssueComment[]>(`/api/tasks/${taskId}/issue-comments`);
  }

  importIssues(projectId: string): Observable<ApiResponse<ImportIssuesResult>> {
    return this.api.post<ImportIssuesResult>(`/api/projects/${projectId}/import-issues`, {});
  }
}
