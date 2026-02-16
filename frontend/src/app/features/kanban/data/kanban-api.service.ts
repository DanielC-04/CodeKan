import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  CreateProjectRequest,
  CreateTaskRequest,
  IssueComment,
  IssueDetails,
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
}
